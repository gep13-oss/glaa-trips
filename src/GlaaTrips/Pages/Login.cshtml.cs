using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GlaaTrips.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GlaaTrips.Pages
{
    // The rest of the site requires authentication (a global fallback policy);
    // the login page is the one place that must stay reachable anonymously.
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserAuthenticator _authenticator;

        public LoginModel(UserAuthenticator authenticator)
        {
            _authenticator = authenticator;
        }

        public async Task OnGet()
        {
            if (HttpContext.Request.Query.Any(q => q.Key == "logout") && User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                RedirectFromLogin();
            }
        }

        public async Task OnPost(string username, string password, string remember)
        {
            if (_authenticator.TryAuthenticate(username, password, out var role))
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, username));
                identity.AddClaim(new Claim(ClaimTypes.Role, role));

                var principle = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties { IsPersistent = remember == "on" };
                await HttpContext.SignInAsync(principle, properties);

                RedirectFromLogin();
            }
        }

        private void RedirectFromLogin()
        {
            HttpContext.Response.Redirect(ResolveReturnTarget());
        }

        // Works out where to send the user after signing in (or out): the page they
        // came from, then an explicit returnUrl, falling back to home. Every
        // candidate must be a safe, same-site location AND must not be the login
        // page itself — without the login-page guard a failed attempt sets the
        // Referer to /login, so the next successful sign-in would loop straight back
        // to the login page instead of reaching the site.
        private string ResolveReturnTarget()
        {
            if (Request.HasFormContentType &&
                Request.Form.TryGetValue("referrer", out var referrer) &&
                TryGetSafeLocalPath(referrer.ToString(), out var referrerPath))
            {
                return referrerPath;
            }

            if (Request.Query.TryGetValue("returnUrl", out var returnUrl) &&
                TryGetSafeLocalPath(returnUrl.ToString(), out var returnPath))
            {
                return returnPath;
            }

            return "/";
        }

        // A redirect target is safe when it stays on this site and is not the login
        // page. Same-site absolute URLs (such as the Referer header) are reduced to
        // their path; protocol-relative and off-site URLs are rejected, which also
        // closes an open-redirect on returnUrl.
        private bool TryGetSafeLocalPath(string candidate, out string localPath)
        {
            localPath = null;

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            var path = candidate;

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var absolute))
            {
                if (absolute.Authority != Request.Host.Value)
                {
                    return false;
                }

                path = absolute.PathAndQuery;
            }

            if (!Url.IsLocalUrl(path) || path.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            localPath = path;
            return true;
        }
    }
}