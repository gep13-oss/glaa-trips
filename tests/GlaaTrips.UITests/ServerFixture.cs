using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GlaaTrips.UITests
{
    /// <summary>
    /// Boots the migrated glaa-trips app once for the whole test run and tears it
    /// down afterwards. The app is launched as a child process against a private,
    /// seeded web root and with injected test credentials, so the suite is fully
    /// self-contained: `dotnet test` (or the Cake `Test` target) needs nothing
    /// running beforehand. Set GLAA_TRIPS_BASEURL to point the suite at an already
    /// running instance instead (e.g. a deployed environment).
    /// </summary>
    [SetUpFixture]
    public sealed class ServerFixture
    {
        private static Process? _app;
        private static string? _tempWebRoot;

        public static string BaseUrl { get; private set; } = string.Empty;

        public const string TestUsername = "testuser";
        public const string TestPassword = "test-password-123";
        public const string TestSalt = "test-salt";
        public const string SampleAlbumSlug = "sample-trip";
        public const string SampleAlbumTitle = "Sample Trip";

        [OneTimeSetUp]
        public async Task StartServer()
        {
            var external = Environment.GetEnvironmentVariable("GLAA_TRIPS_BASEURL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                BaseUrl = external.TrimEnd('/');
                return;
            }

            var webProject = FindWebProject();
            var webDir = Path.GetDirectoryName(webProject)!;

            _tempWebRoot = Path.Combine(Path.GetTempPath(), "glaa-trips-tests-" + Guid.NewGuid().ToString("N"));
            SeedWebRoot(_tempWebRoot);

            var port = GetFreePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            var passwordHash = HashPassword(TestPassword, TestSalt);

            var psi = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = webDir,
            };
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add(webProject);
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("Release");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add($"--contentRoot={webDir}");
            psi.ArgumentList.Add($"--webroot={_tempWebRoot}");
            psi.ArgumentList.Add($"--urls={BaseUrl}");
            psi.ArgumentList.Add("--forcessl=false");
            psi.ArgumentList.Add($"--user:username={TestUsername}");
            psi.ArgumentList.Add($"--user:salt={TestSalt}");
            psi.ArgumentList.Add($"--user:password={passwordHash}");
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

            _app = Process.Start(psi)!;

            // Drain stdout/stderr so a full pipe never blocks the app.
            _app.OutputDataReceived += (_, _) => { };
            _app.ErrorDataReceived += (_, _) => { };
            _app.BeginOutputReadLine();
            _app.BeginErrorReadLine();

            await WaitForServer(BaseUrl, TimeSpan.FromSeconds(90));
        }

        [OneTimeTearDown]
        public void StopServer()
        {
            try
            {
                if (_app is { HasExited: false })
                {
                    _app.Kill(entireProcessTree: true);
                }
            }
            catch
            { /* best effort */
            }

            _app?.Dispose();

            try
            {
                if (_tempWebRoot is not null && Directory.Exists(_tempWebRoot))
                {
                    Directory.Delete(_tempWebRoot, recursive: true);
                }
            }
            catch
            { /* best effort */
            }
        }

        private static void SeedWebRoot(string webRoot)
        {
            var albums = Path.Combine(webRoot, "albums");
            var album = Path.Combine(albums, SampleAlbumSlug);
            Directory.CreateDirectory(Path.Combine(album, "thumbnail"));

            var meta = new
            {
                DisplayName = SampleAlbumTitle,
                Description = "A sample album used by the baseline Playwright tests.",
                Visited = "2026-01-01T00:00:00",
                Latitude = 55.953251,
                Longitude = -3.188267,
            };
            File.WriteAllText(Path.Combine(album, "data.json"), JsonSerializer.Serialize(meta));

            var markers = new[] { new { Lat = 55.953251, Long = -3.188267, Slug = SampleAlbumSlug } };
            File.WriteAllText(Path.Combine(albums, "markers.json"), JsonSerializer.Serialize(markers));
        }

        // Mirrors the app's GlaaTrips.Models.PasswordHasher: PBKDF2/HMAC-SHA256,
        // 600,000 iterations, 256-bit key, salt as UTF-8 bytes, upper-case hex.
        // This project is black-box and cannot reference the app assembly, so the
        // derivation is duplicated here — keep it in sync with PasswordHasher.
        private static string HashPassword(string password, string salt)
        {
            var saltBytes = Encoding.UTF8.GetBytes(salt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 600_000, HashAlgorithmName.SHA256, 256 / 8);
            return Convert.ToHexString(hash);
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string FindWebProject()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "GlaaTrips.csproj");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException("Could not locate GlaaTrips.csproj above the test output directory.");
        }

        private static async Task WaitForServer(string baseUrl, TimeSpan timeout)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var deadline = DateTime.UtcNow + timeout;
            Exception? last = null;

            while (DateTime.UtcNow < deadline)
            {
                if (_app is { HasExited: true })
                {
                    throw new InvalidOperationException($"The web app exited early with code {_app.ExitCode}.");
                }

                try
                {
                    var res = await client.GetAsync(baseUrl + "/");
                    if ((int)res.StatusCode < 500)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    last = ex;
                }

                await Task.Delay(500);
            }

            throw new TimeoutException($"Web app did not become ready at {baseUrl} within {timeout.TotalSeconds:n0}s.", last);
        }
    }
}