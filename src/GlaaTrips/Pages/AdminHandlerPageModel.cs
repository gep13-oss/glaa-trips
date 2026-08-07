using GlaaTrips.Models;
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
        /// Guards an admin handler so only an administrator can run it. An
        /// unauthenticated request is challenged (redirected to the login page); an
        /// authenticated non-admin (a viewer) is forbidden.
        /// </summary>
        /// <returns>
        /// A challenge result when the request is not authenticated, a forbid result
        /// when the user is authenticated but not an admin, or <c>null</c> so the
        /// handler proceeds for an admin.
        /// </returns>
        protected IActionResult RequireAdmin()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            if (!User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            return null;
        }
    }
}