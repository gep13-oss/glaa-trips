# glaa-trips
Family trips that we have been on

## Access & users

The whole site requires signing in — nothing (the map, albums or photos) is
visible to an anonymous visitor. Each account has a **role**:

- **viewer** — can browse the map, albums and photos.
- **admin** — can additionally create, edit and delete albums and photos.

Accounts are **configuration-driven** — there is no database and no sign-up
page. Credentials are never committed to the repository; set them per
environment via
[user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
in development, or App Service application settings (or environment variables)
in production.

Passwords are stored as a PBKDF2-HMACSHA256 hash (600,000 iterations) with a
per-user salt. **Use a fresh, random salt for each user** — salts do not need
to be secret, but they must be unique per account (a shared salt lets equal
passwords be spotted and lets an attacker attack every account at once).

### Adding a user

1. **Generate a random salt** — a unique value per user:

   ```powershell
   [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
   ```

2. **Generate the password hash** using that salt (substitute the real
   password and the salt from step 1):

   ```powershell
   [Convert]::ToHexString([Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2(
       'the-password',
       [Text.Encoding]::UTF8.GetBytes('the-salt-from-step-1'),
       600000,
       [Security.Cryptography.HashAlgorithmName]::SHA256,
       32))
   ```

3. **Store the account.** The username is the configuration key; set its salt,
   hash and role (`viewer` or `admin`).

   Development (run from `src/GlaaTrips`):

   ```bash
   dotnet user-secrets set "Users:alice:salt"     "<salt from step 1>"
   dotnet user-secrets set "Users:alice:password" "<hash from step 2>"
   dotnet user-secrets set "Users:alice:role"     "viewer"
   ```

   Production — Azure App Service application settings (`__` replaces `:`):

   ```
   Users__alice__salt     = <salt from step 1>
   Users__alice__password = <hash from step 2>
   Users__alice__role     = viewer
   ```

4. **Restart the app** so it picks up the new configuration.

The original single administrator account (`user:username` / `user:password` /
`user:salt`) still works and is treated as an admin, so existing setups are
unaffected.
