using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace AalgTrips.Models
{
    /// <summary>
    /// Derives and verifies the admin password hash. Uses PBKDF2 with HMAC-SHA256
    /// at the OWASP-recommended iteration count, replacing the legacy
    /// HMAC-SHA1 / 1000-iteration scheme. The salt is supplied through
    /// configuration (single admin credential, no database).
    /// <para>
    /// NOTE: the test harness mirrors this in
    /// <c>AalgTrips.UITests.ServerFixture.HashPassword</c> — that project is
    /// black-box and cannot reference this assembly, so it re-implements the same
    /// derivation to seed a credential. Keep the algorithm, iteration count and
    /// output size in sync across both, or the login tests will fail.
    /// </para>
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>OWASP guidance for PBKDF2-HMAC-SHA256.</summary>
        public const int IterationCount = 600_000;

        private const int KeyLengthBytes = 256 / 8;

        /// <summary>
        /// Derives the PBKDF2 hash of <paramref name="password"/> using
        /// <paramref name="salt"/> and returns it as upper-case hex.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <param name="salt">The salt, encoded as UTF-8 bytes.</param>
        /// <returns>The derived hash as an upper-case hex string.</returns>
        public static string HashToHex(string password, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt ?? string.Empty);

            byte[] hash = KeyDerivation.Pbkdf2(
                password: password ?? string.Empty,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: IterationCount,
                numBytesRequested: KeyLengthBytes);

            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Verifies <paramref name="password"/> against the configured
        /// <paramref name="expectedHex"/> hash in constant time.
        /// </summary>
        /// <param name="password">The plaintext password to check.</param>
        /// <param name="salt">The salt, encoded as UTF-8 bytes.</param>
        /// <param name="expectedHex">The configured hash, as upper-case hex.</param>
        /// <returns>
        /// <c>true</c> when the password matches the configured hash; otherwise
        /// <c>false</c> (including when no hash is configured).
        /// </returns>
        public static bool Verify(string password, string salt, string expectedHex)
        {
            if (string.IsNullOrEmpty(expectedHex))
            {
                return false;
            }

            string actualHex = HashToHex(password, salt);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(actualHex),
                Encoding.UTF8.GetBytes(expectedHex));
        }
    }
}