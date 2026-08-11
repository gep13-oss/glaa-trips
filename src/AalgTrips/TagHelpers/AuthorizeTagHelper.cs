using AalgTrips.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AalgTrips.TagHelpers
{
    // Renders the element only for an administrator. It marks the admin-only
    // controls (create / edit / upload / delete / set-cover forms and admin.js);
    // a viewer or an anonymous visitor never sees them. The sign-out link is not
    // gated by this — it checks authentication directly so viewers can sign out.
    [HtmlTargetElement("*", Attributes = "if-authorized")]
    public class AuthorizeTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        // This makes sure it runs before any other tag helpers
        public override int Order => int.MinValue;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!ViewContext.HttpContext.User.IsInRole(Roles.Admin))
            {
                output.SuppressOutput();
            }
            else if (context.AllAttributes.TryGetAttribute("if-authorized", out var attribute))
            {
                output.Attributes.Remove(attribute);
            }
        }
    }
}