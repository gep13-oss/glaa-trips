using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Coverage for <see cref="PasswordHasher"/>, the admin credential KDF. The
    /// known-answer vector pins the exact algorithm (PBKDF2-HMAC-SHA256, 600,000
    /// iterations, 256-bit, UTF-8 salt, upper-case hex) so an accidental change to
    /// the algorithm or iteration count is caught here rather than silently
    /// weakening every stored password.
    /// </summary>
    [TestFixture]
    public class PasswordHasherTests
    {
        // Independently computed (see item 7 notes): PBKDF2-HMAC-SHA256, 600k
        // iterations, 32-byte output for password "correct-horse-battery-staple"
        // and salt "glaa-trips-known-answer".
        private const string KnownPassword = "correct-horse-battery-staple";
        private const string KnownSalt = "glaa-trips-known-answer";
        private const string KnownHash = "2540E29489708B6C68FBDCF8362AA8886E7C0BB4A4A101C54EF6C10CC6821B0E";

        [Test]
        public void HashToHex_matches_the_known_answer_vector()
        {
            Assert.That(PasswordHasher.HashToHex(KnownPassword, KnownSalt), Is.EqualTo(KnownHash));
        }

        [Test]
        public void HashToHex_produces_a_256_bit_uppercase_hex_digest()
        {
            var hash = PasswordHasher.HashToHex("hunter2", "salt");

            Assert.That(hash, Has.Length.EqualTo(64));
            Assert.That(hash, Does.Match("^[0-9A-F]+$"));
        }

        [Test]
        public void HashToHex_changes_with_the_salt()
        {
            var a = PasswordHasher.HashToHex("hunter2", "salt-a");
            var b = PasswordHasher.HashToHex("hunter2", "salt-b");

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void HashToHex_changes_with_the_password()
        {
            var a = PasswordHasher.HashToHex("hunter2", "salt");
            var b = PasswordHasher.HashToHex("hunter3", "salt");

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Verify_accepts_the_matching_password()
        {
            Assert.That(PasswordHasher.Verify(KnownPassword, KnownSalt, KnownHash), Is.True);
        }

        [TestCase("wrong-password")]
        [TestCase("")]
        [TestCase("Correct-Horse-Battery-Staple")]
        public void Verify_rejects_a_non_matching_password(string attempt)
        {
            Assert.That(PasswordHasher.Verify(attempt, KnownSalt, KnownHash), Is.False);
        }

        [Test]
        public void Verify_rejects_when_no_hash_is_configured()
        {
            Assert.That(PasswordHasher.Verify(KnownPassword, KnownSalt, string.Empty), Is.False);
            Assert.That(PasswordHasher.Verify(KnownPassword, KnownSalt, null!), Is.False);
        }

        [Test]
        public void Iteration_count_meets_owasp_guidance()
        {
            Assert.That(PasswordHasher.IterationCount, Is.EqualTo(600_000));
        }
    }
}