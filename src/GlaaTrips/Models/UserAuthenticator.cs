using System;
using Microsoft.Extensions.Configuration;

namespace GlaaTrips.Models
{
    /// <summary>
    /// Authenticates a username/password against the accounts defined in
    /// configuration and reports the role the account holds. Accounts are
    /// config-driven — there is no database and no sign-up UI, in keeping with the
    /// site's filesystem/config model:
    /// <list type="bullet">
    /// <item>a <c>Users</c> section maps each username to its <c>salt</c>, PBKDF2
    /// <c>password</c> hash and <c>role</c> (<c>admin</c> or <c>viewer</c>);</item>
    /// <item>the legacy single <c>user</c> account is still honoured as an
    /// administrator, so an existing single-credential setup keeps working.</item>
    /// </list>
    /// Add a user by setting <c>Users:{name}:salt</c> / <c>:password</c> /
    /// <c>:role</c> via user-secrets (development) or environment / App Service
    /// settings (production), then restarting.
    /// </summary>
    public class UserAuthenticator
    {
        private readonly IConfiguration _config;

        public UserAuthenticator(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Verifies the supplied credentials and, on success, reports the role.
        /// </summary>
        /// <param name="username">The submitted username.</param>
        /// <param name="password">The submitted password.</param>
        /// <param name="role">The authenticated user's role when the method returns <c>true</c>; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> when the credentials match a configured account.</returns>
        public bool TryAuthenticate(string username, string password, out string role)
        {
            role = null;

            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            // The legacy single account (configured under "user") is an admin.
            string legacyUser = _config["user:username"];
            if (!string.IsNullOrEmpty(legacyUser) &&
                string.Equals(username, legacyUser, StringComparison.OrdinalIgnoreCase))
            {
                if (PasswordHasher.Verify(password, _config["user:salt"], _config["user:password"]))
                {
                    role = Roles.Admin;
                    return true;
                }

                return false;
            }

            // Additional accounts live under "Users:{username}". Match the child
            // keys literally (case-insensitively) rather than treating the supplied
            // username as a configuration path.
            foreach (var account in _config.GetSection("Users").GetChildren())
            {
                if (!string.Equals(account.Key, username, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (PasswordHasher.Verify(password, account["salt"], account["password"]))
                {
                    role = IsAdminRole(account["role"]) ? Roles.Admin : Roles.Viewer;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool IsAdminRole(string configuredRole)
        {
            return string.Equals(configuredRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        }
    }
}