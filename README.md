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

   Development (run from `src/AalgTrips`):

   ```powershell
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

## Deploying to Azure

The app runs on a **Linux App Service**; album content (photos, thumbnails,
metadata, markers) lives in a **private Azure Blob container**, not on the
app's disk, so it survives redeploys and restarts. A GitHub Actions workflow
(`.github/workflows/main_glaa-trips.yml`) builds, tests and deploys on every
push to `main`, authenticating to Azure with **OIDC** (no publish profile or
secret stored in GitHub).

All `az` snippets below are PowerShell and assume `az login` and a chosen
subscription (`az account set --subscription <id>`).

### 1. Provision the resources (one-time)

```powershell
# Adjust the names/region. Storage account names must be globally unique and
# 3–24 lowercase alphanumeric characters.
$RG = "glaa-trips-rg"
$LOCATION = "ukwest"
$PLAN = "glaa-trips-plan"
$APP = "glaa-trips"                 # must match AZURE_WEBAPP_NAME in the workflow
$STORAGE = "glaatripsstore123"
$CONTAINER = "albums"

az group create -n $RG -l $LOCATION

# Storage account + a private container for album content
az storage account create -n $STORAGE -g $RG -l $LOCATION `
  --sku Standard_LRS --allow-blob-public-access false
az storage container create --account-name $STORAGE -n $CONTAINER   # private

# Linux App Service (Basic B1) running .NET 10
az appservice plan create -n $PLAN -g $RG --is-linux --sku B1
az webapp create -n $APP -g $RG -p $PLAN --runtime "DOTNETCORE:10.0"
```

### 2. Configure application settings

App settings use `__` (double underscore) in place of the `:` config
separator. Do **not** set `ASPNETCORE_ENVIRONMENT=Development` in production.

```powershell
$CONN = az storage account show-connection-string -n $STORAGE -g $RG `
  --query connectionString -o tsv

az webapp config appsettings set -n $APP -g $RG --settings `
  "Storage__Provider=AzureBlob" `
  "Storage__AzureBlob__ConnectionString=$CONN" `
  "Storage__AzureBlob__ContainerName=$CONTAINER" `
  "forcessl=true"

# The admin account (generate the salt + hash as in "Adding a user" above)
az webapp config appsettings set -n $APP -g $RG --settings `
  "user__username=<admin-username>" `
  "user__salt=<unique random salt>" `
  "user__password=<PBKDF2 hash>"

# Any additional viewer/admin accounts, same shape as user-secrets:
az webapp config appsettings set -n $APP -g $RG --settings `
  "Users__alice__salt=<unique random salt>" `
  "Users__alice__password=<PBKDF2 hash>" `
  "Users__alice__role=viewer"
```

(For extra safety the connection string and hashes can live in Key Vault and be
referenced from app settings.)

### 3. Set up the secretless deploy identity (OIDC)

Create an identity GitHub Actions can use, let it deploy the web app, and trust
this repository. The workflow's deploy job runs in the `production`
**environment**, so the federated credential's subject must be scoped to that
environment (`…:environment:production`) — not the branch — or `azure/login`
fails with *"No matching federated identity record"*.

```powershell
$SUBSCRIPTION = az account show --query id -o tsv
$TENANT = az account show --query tenantId -o tsv

$APP_ID = az ad app create --display-name "glaa-trips-deploy" --query appId -o tsv
az ad sp create --id $APP_ID

az role assignment create --assignee $APP_ID --role "Contributor" `
  --scope "/subscriptions/$SUBSCRIPTION/resourceGroups/$RG/providers/Microsoft.Web/sites/$APP"

# Trust GitHub Actions from this repo's "production" environment. Write the JSON
# to a file to avoid shell-quoting issues. Replace <owner>/<repo>.
@'
{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}
'@ | Set-Content -Path federated-credential.json

az ad app federated-credential create --id $APP_ID --parameters federated-credential.json
```

Then add three **repository secrets** (Settings → Secrets and variables →
Actions):

- `AZURE_CLIENT_ID` = the `APP_ID` above
- `AZURE_TENANT_ID` = the `TENANT` above
- `AZURE_SUBSCRIPTION_ID` = the `SUBSCRIPTION` above

### 4. Deploy

Merge `overhaul` into `main` when you're happy and push. The workflow builds,
runs the full test suite (format + unit + Playwright UI), publishes and deploys
to the App Service. Ensure `AZURE_WEBAPP_NAME` in the workflow matches `$APP`.

### CDN (optional, later)

Because content is served through the authenticated app rather than a public
bucket, a CDN would front the **App Service** (e.g. Azure Front Door). At
family scale this mainly helps static-asset latency, so add it only if
world-wide speed becomes a real concern.
