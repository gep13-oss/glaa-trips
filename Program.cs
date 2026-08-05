using System;
using GlaaTrips.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (was Startup.ConfigureServices) ----
builder.Services.AddRazorPages();
builder.Services.AddSingleton<AlbumCollection>();
builder.Services.AddSingleton<ImageProcessor>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    // The admin handlers challenge unauthenticated requests; without these the
    // cookie handler would redirect to its default /Account/Login, which does
    // not exist here. The login page lives at /login and reads returnUrl.
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.ReturnUrlParameter = "returnUrl";
});

// Reads ApplicationInsights:ConnectionString from configuration when present.
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// ---- HTTP pipeline (was Startup.Configure) ----
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePages("text/plain", "Status code page, status code: {0}");

app.UseStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = (context) =>
    {
        var time = TimeSpan.FromDays(365);
        context.Context.Response.Headers[HeaderNames.CacheControl] = $"max-age={time.TotalSeconds.ToString()}";
        context.Context.Response.Headers[HeaderNames.Expires] = DateTime.UtcNow.Add(time).ToString("R");
    },
});

if (app.Configuration.GetValue<bool>("forcessl"))
{
    app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();