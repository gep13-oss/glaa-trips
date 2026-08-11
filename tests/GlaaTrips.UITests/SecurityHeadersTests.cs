namespace GlaaTrips.UITests
{
    /// <summary>
    /// Verifies the OWASP-aligned security response headers set by the middleware
    /// in Program.cs. Asserting on a static asset (a non-Razor response) proves the
    /// headers come from the pipeline-front middleware — which covers every
    /// response — rather than <c>_ViewStart</c>, which only ran for rendered Razor
    /// pages and left the media endpoint and static files uncovered.
    /// </summary>
    [TestFixture]
    public class SecurityHeadersTests : UITestBase
    {
        [Test]
        public async Task Responses_carry_the_owasp_security_headers()
        {
            var response = await Page.APIRequest.GetAsync(BaseUrl + "/js/map.js");
            var headers = response.Headers;

            Assert.Multiple(() =>
            {
                Assert.That(headers.GetValueOrDefault("x-content-type-options"), Is.EqualTo("nosniff"));
                Assert.That(headers.GetValueOrDefault("x-frame-options"), Is.EqualTo("DENY"));
                Assert.That(headers.GetValueOrDefault("referrer-policy"), Is.EqualTo("strict-origin-when-cross-origin"));
                Assert.That(headers.GetValueOrDefault("x-xss-protection"), Is.EqualTo("0"));
                Assert.That(headers.GetValueOrDefault("cross-origin-opener-policy"), Is.EqualTo("same-origin"));
                Assert.That(headers.GetValueOrDefault("cross-origin-resource-policy"), Is.EqualTo("same-origin"));
                Assert.That(headers, Does.ContainKey("permissions-policy"));

                // The strict resource policy is enforced (no longer Report-Only).
                var csp = headers.GetValueOrDefault("content-security-policy");
                Assert.That(csp, Does.Contain("frame-ancestors 'none'"));
                Assert.That(csp, Does.Contain("script-src 'self'"));
                Assert.That(headers, Does.Not.ContainKey("content-security-policy-report-only"));
            });
        }
    }
}