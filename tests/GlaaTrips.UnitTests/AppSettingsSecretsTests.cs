using System;
using System.IO;
using System.Text.Json;

namespace GlaaTrips.UnitTests
{
    /// <summary>
    /// Fails if a real secret is ever committed to <c>appsettings.json</c>. The
    /// admin credential (hash/salt/username) and the Google Maps API key must be
    /// supplied per environment via user-secrets or environment variables, leaving
    /// these entries blank in the committed file.
    /// </summary>
    [TestFixture]
    public class AppSettingsSecretsTests
    {
        [TestCase("user:username")]
        [TestCase("user:password")]
        [TestCase("user:salt")]
        [TestCase("GoogleMaps:ApiKey")]
        public void Committed_appsettings_has_no_populated_secret(string configPath)
        {
            using var doc = JsonDocument.Parse(
                File.ReadAllText(AppSettingsPath()),
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

            var value = ResolvePath(doc.RootElement, configPath);

            Assert.That(value, Is.Empty, $"'{configPath}' must be blank in the committed appsettings.json — set it via user-secrets / environment instead");
        }

        private static string? ResolvePath(JsonElement root, string colonPath)
        {
            var element = root;

            foreach (var key in colonPath.Split(':'))
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(key, out element))
                {
                    return null;
                }
            }

            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
        }

        private static string AppSettingsPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                var projectDir = Path.Combine(dir.FullName, "src", "GlaaTrips");

                if (File.Exists(Path.Combine(projectDir, "GlaaTrips.csproj")))
                {
                    return Path.Combine(projectDir, "appsettings.json");
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException("Could not locate src/GlaaTrips/GlaaTrips.csproj above the test output directory.");
        }
    }
}