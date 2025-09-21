namespace BookingCare.Services.Doctor.Middlewares;

public class DoctorSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DoctorSecurityMiddleware> _logger;

    public DoctorSecurityMiddleware(RequestDelegate next, ILogger<DoctorSecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context);

        // Log security-related information
        LogSecurityInfo(context);

        // Perform basic security checks
        if (await PerformSecurityChecks(context))
        {
            await _next(context);
        }
        else
        {
            // Security check failed
            context.Response.StatusCode = 403; // Forbidden
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Security check failed",
                message = "Access denied due to security policy violation."
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        // Security headers
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["X-XSS-Protection"] = "1; mode=block";
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Restrictive Content Security Policy
        response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "object-src 'none'; " +
            "media-src 'self'; " +
            "manifest-src 'self'; " +
            "worker-src 'self'; " +
            "child-src 'self'; " +
            "frame-src 'none'; " +
            "upgrade-insecure-requests; " +
            "block-all-mixed-content";

        // Remove server information
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
    }

    private void LogSecurityInfo(HttpContext context)
    {
        var request = context.Request;
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = request.Headers.UserAgent.ToString();
        var referer = request.Headers.Referer.ToString();
        var method = request.Method;
        var path = request.Path.Value;

        // Log suspicious activities
        if (IsSuspiciousRequest(request))
        {
            _logger.LogWarning("Suspicious request detected: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent} - Referer: {Referer}",
                method, path, clientIp, userAgent, referer);
        }

        // Log security-relevant information
        _logger.LogDebug("Security Info: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent}",
            method, path, clientIp, userAgent);
    }

    private static bool IsSuspiciousRequest(HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString().ToLower();
        var path = request.Path.Value?.ToLower();

        // Check for common attack patterns
        var suspiciousPatterns = new[]
        {
            "sqlmap",
            "nikto",
            "nmap",
            "dirb",
            "gobuster",
            "wfuzz",
            "sql injection",
            "xss",
            "script",
            "<script",
            "javascript:",
            "data:text/html",
            "vbscript:",
            "onload=",
            "onerror=",
            "onclick="
        };

        // Check User-Agent
        if (suspiciousPatterns.Any(pattern => userAgent.Contains(pattern)))
        {
            return true;
        }

        // Check URL path
        if (!string.IsNullOrEmpty(path) && suspiciousPatterns.Any(pattern => path.Contains(pattern)))
        {
            return true;
        }

        // Check query string
        var queryString = request.QueryString.ToString().ToLower();
        if (!string.IsNullOrEmpty(queryString) && suspiciousPatterns.Any(pattern => queryString.Contains(pattern)))
        {
            return true;
        }

        return false;
    }

    private Task<bool> PerformSecurityChecks(HttpContext context)
    {
        var request = context.Request;

        // Check for suspicious requests
        if (IsSuspiciousRequest(request))
        {
            _logger.LogWarning("Blocking suspicious request from {ClientIp}",
                context.Connection.RemoteIpAddress?.ToString());
            return Task.FromResult(false);
        }

        // Check for required headers (if needed)
        if (!ValidateRequiredHeaders())
        {
            _logger.LogWarning("Missing required headers from {ClientIp}",
                context.Connection.RemoteIpAddress?.ToString());
            return Task.FromResult(false);
        }

        // Check request size limits
        if (request.ContentLength > GetMaxRequestSize())
        {
            _logger.LogWarning("Request too large from {ClientIp}: {Size} bytes",
                context.Connection.RemoteIpAddress?.ToString(), request.ContentLength);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private static bool ValidateRequiredHeaders()
    {
        // Add any required header validation here
        // For now, we'll just return true
        return true;
    }

    private static long GetMaxRequestSize()
    {
        // 10MB limit for request body
        return 10 * 1024 * 1024;
    }
}
