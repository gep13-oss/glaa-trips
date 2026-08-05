using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GlaaTrips.Pages
{
    /// <summary>
    /// Base class for pages that expose admin-only POST handlers alongside public
    /// GET handlers (viewing an album is public; creating/editing/deleting is not).
    /// <para>
    /// ASP.NET Core silently ignores <c>[Authorize]</c> on Razor Page handler
    /// methods (analyzer warning MVC1001), and a page/class-level or global policy
    /// would also gate the public GET handler on the same page. So each admin
    /// handler must guard explicitly:
    /// <code>
    /// if (RequireAdmin() is { } challenge)
    /// {
    ///     return challenge;
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public abstract class AdminHandlerPageModel : PageModel
    {
        /// <summary>
        /// Guards an admin handler against unauthenticated access.
        /// </summary>
        /// <returns>
        /// A challenge result (which the cookie handler turns into a redirect to
        /// the login page) when the current request is not authenticated;
        /// otherwise <c>null</c> so the handler proceeds.
        /// </returns>
        protected IActionResult RequireAdmin()
        {
            return User.Identity?.IsAuthenticated == true ? null : Challenge();
        }
    }
}