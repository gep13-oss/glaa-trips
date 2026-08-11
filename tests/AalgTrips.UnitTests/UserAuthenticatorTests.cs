using AalgTrips.Models;
using Microsoft.Extensions.Configuration;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Covers <see cref="UserAuthenticator"/>: the legacy single <c>user</c>
    /// account signs in as an admin, additional <c>Users</c> entries sign in with
    /// their configured role (defaulting to viewer), and bad or unknown credentials
    /// are rejected. This is the config-driven, no-database account model.
    /// </summary>
    [TestFixture]
    public class UserAuthenticatorTests
    {
        private const string Salt = "unit-salt";

        [Test]
        public void Legacy_user_authenticates_as_admin()
        {
            var auth = Build(new Dictionary<string, string?>
            {
                ["user:username"] = "owner",
                ["user:salt"] = Salt,
                ["user:password"] = PasswordHasher.HashToHex("owner-pass", Salt),
            });

            Assert.That(auth.TryAuthenticate("owner", "owner-pass", out var role), Is.True);
            Assert.That(role, Is.EqualTo(Roles.Admin));
        }

        [Test]
        public void Configured_viewer_authenticates_with_the_viewer_role()
        {
            var auth = Build(Account("alice", "alice-pass", "viewer"));

            Assert.That(auth.TryAuthenticate("alice", "alice-pass", out var role), Is.True);
            Assert.That(role, Is.EqualTo(Roles.Viewer));
        }

        [Test]
        public void Configured_admin_user_gets_the_admin_role()
        {
            var auth = Build(Account("bob", "bob-pass", "admin"));

            Assert.That(auth.TryAuthenticate("bob", "bob-pass", out var role), Is.True);
            Assert.That(role, Is.EqualTo(Roles.Admin));
        }

        [Test]
        public void Missing_or_unrecognised_role_defaults_to_viewer()
        {
            var auth = Build(Account("carol", "carol-pass", role: null));

            Assert.That(auth.TryAuthenticate("carol", "carol-pass", out var role), Is.True);
            Assert.That(role, Is.EqualTo(Roles.Viewer));
        }

        [Test]
        public void The_username_match_is_case_insensitive()
        {
            var auth = Build(Account("Alice", "alice-pass", "viewer"));

            Assert.That(auth.TryAuthenticate("alice", "alice-pass", out _), Is.True);
        }

        [Test]
        public void A_wrong_password_is_rejected()
        {
            var auth = Build(Account("alice", "alice-pass", "viewer"));

            Assert.That(auth.TryAuthenticate("alice", "not-the-password", out _), Is.False);
        }

        [Test]
        public void An_unknown_user_is_rejected()
        {
            var auth = Build(new Dictionary<string, string?>());

            Assert.That(auth.TryAuthenticate("nobody", "whatever", out _), Is.False);
        }

        private static Dictionary<string, string?> Account(string username, string password, string? role)
        {
            var values = new Dictionary<string, string?>
            {
                [$"Users:{username}:salt"] = Salt,
                [$"Users:{username}:password"] = PasswordHasher.HashToHex(password, Salt),
            };

            if (role is not null)
            {
                values[$"Users:{username}:role"] = role;
            }

            return values;
        }

        private static UserAuthenticator Build(Dictionary<string, string?> values)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            return new UserAuthenticator(config);
        }
    }
}