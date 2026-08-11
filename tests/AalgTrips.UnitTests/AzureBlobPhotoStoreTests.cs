using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Runs the <see cref="PhotoStoreContractTests"/> against
    /// <see cref="AzureBlobPhotoStore"/> so the Azure Blob store is proven to
    /// satisfy the same contract as the local disk store. It is an integration
    /// test that needs a real (or emulated) Blob endpoint, supplied through the
    /// <c>AALG_TRIPS_BLOB_CONNECTION</c> environment variable — a full storage
    /// connection string. The simplest source is the Azurite emulator (the
    /// <c>--skipApiVersionCheck</c> flag is needed because the Azure SDK's API
    /// version is newer than Azurite understands; real Azure supports it):
    /// <code>
    ///   npx --package azurite azurite-blob --silent --skipApiVersionCheck --location .azurite
    ///   set AALG_TRIPS_BLOB_CONNECTION=UseDevelopmentStorage=true
    /// </code>
    /// When the variable is not set the tests are ignored rather than failed, so
    /// the default gate stays fast and hermetic while the local disk store covers
    /// the same contract unconditionally. Each test uses its own container for
    /// isolation.
    /// </summary>
    [TestFixture]
    public sealed class AzureBlobPhotoStoreTests : PhotoStoreContractTests
    {
        private string? _connectionString;

        [OneTimeSetUp]
        public void ReadConnection()
        {
            _connectionString = Environment.GetEnvironmentVariable("AALG_TRIPS_BLOB_CONNECTION");
        }

        protected override IPhotoStore CreateStore()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Assert.Ignore("Set AALG_TRIPS_BLOB_CONNECTION (e.g. to a running Azurite) to run the Azure Blob store contract tests.");
            }

            // A fresh container per test keeps the contract's fixed album id isolated.
            return new AzureBlobPhotoStore(_connectionString!, "t" + Guid.NewGuid().ToString("N"));
        }
    }
}