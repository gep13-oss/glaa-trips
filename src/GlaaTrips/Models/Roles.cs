namespace GlaaTrips.Models
{
    /// <summary>
    /// The roles a signed-in user can hold. A <see cref="Viewer"/> may browse the
    /// whole site (map, albums, photos); an <see cref="Admin"/> may additionally
    /// create, edit and delete album content.
    /// </summary>
    public static class Roles
    {
        /// <summary>Full access, including content management.</summary>
        public const string Admin = "admin";

        /// <summary>Read-only access to the site.</summary>
        public const string Viewer = "viewer";
    }
}