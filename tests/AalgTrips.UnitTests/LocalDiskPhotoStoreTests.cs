using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Runs the <see cref="PhotoStoreContractTests"/> against
    /// <see cref="LocalDiskPhotoStore"/>, each test over its own temp albums root.
    /// </summary>
    [TestFixture]
    public sealed class LocalDiskPhotoStoreTests : PhotoStoreContractTests
    {
        private readonly List<string> _roots = new List<string>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var root in _roots)
            {
                try
                {
                    if (Directory.Exists(root))
                    {
                        Directory.Delete(root, recursive: true);
                    }
                }
                catch
                { /* best effort */
                }
            }

            _roots.Clear();
        }

        protected override IPhotoStore CreateStore()
        {
            var root = Path.Combine(Path.GetTempPath(), "glaa-store-" + Guid.NewGuid().ToString("N"));
            _roots.Add(root);
            return new LocalDiskPhotoStore(Path.Combine(root, "albums"));
        }
    }
}