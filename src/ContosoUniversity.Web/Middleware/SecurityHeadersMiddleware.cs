namespace ContosoUniversity.Web.Middleware;

/// <summary>
/// Adds the standard set of HTTP security response headers (rw-001d, SEC-MEDIUM-002).
///
/// Five headers are appended to every outgoing response, before the response body
/// is written, mirroring the assessment remediation in
/// <c>specs/assessment/security.md §4 SEC-MEDIUM-002</c>:
/// <list type="bullet">
///   <item><description><c>Strict-Transport-Security</c> — pin clients to HTTPS for one year.</description></item>
///   <item><description><c>Content-Security-Policy</c> — restrict default sources to <c>'self'</c>.</description></item>
///   <item><description><c>X-Frame-Options: DENY</c> — block all framing (clickjacking defence).</description></item>
///   <item><description><c>X-Content-Type-Options: nosniff</c> — disable MIME-sniffing.</description></item>
///   <item><description><c>Referrer-Policy: strict-origin-when-cross-origin</c> — leak only the origin on cross-origin navigation.</description></item>
/// </list>
/// Headers are added via <see cref="HttpResponse.OnStarting(System.Func{object, System.Threading.Tasks.Task}, object)"/>
/// so they are written even when downstream middleware short-circuits the
/// pipeline (e.g. the exception handler re-executing /Error/500).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var response = (HttpResponse)state;
            var headers = response.Headers;

            // Use TryAdd so a downstream component can override (e.g. a future
            // [Frameable] attribute on a specific view). Idempotent if invoked twice.
            headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            // rw-002: extended CSP to allow Bootstrap 5.3.3 + jQuery 3.7.1 from the
            // jsDelivr CDN (rewrite assessment §8). default-src remains 'self' so the
            // SEC-MEDIUM-002 substring assertion in tests/integration/features/rw-001d-hardening.feature
            // still passes; script-src and style-src additionally allow https://cdn.jsdelivr.net.
            headers.TryAdd("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' https://cdn.jsdelivr.net; " +
                "style-src 'self' https://cdn.jsdelivr.net; " +
                "img-src 'self' data:; " +
                "font-src 'self' https://cdn.jsdelivr.net");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

            return Task.CompletedTask;
        }, context.Response);

        return _next(context);
    }
}
