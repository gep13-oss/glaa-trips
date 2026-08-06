using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GlaaTrips.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace GlaaTrips.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;

        public LoginModel(IConfiguration config)
        {
            _config = config;
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
            if (username == _config["user:username"] && VerifyHashedPassword(password))
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, _config["user:username"]));

                var principle = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties { IsPersistent = remember == "on" };
                await HttpContext.SignInAsync(principle, properties);

                RedirectFromLogin();
            }
        }

        private void RedirectFromLogin()
        {
            if (Request.HasFormContentType &&
                Request.Form.TryGetValue("referrer", out var referrer) &&
                Uri.TryCreate(referrer.ToString(), UriKind.Absolute, out Uri url) &&
                url.Authority == Request.Host.Value)
            {
                HttpContext.Response.Redirect(url.ToString());
            }
            else if (HttpContext.Request.Query.TryGetValue("returnUrl", out var returnUrl))
            {
                HttpContext.Response.Redirect(returnUrl.ToString());
            }
            else
            {
                HttpContext.Response.Redirect("/");
            }
        }

        private bool VerifyHashedPassword(string password)
        {
            return PasswordHasher.Verify(password, _config["user:salt"], _config["user:password"]);
        }
    }
}