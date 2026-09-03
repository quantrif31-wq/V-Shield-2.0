using Microsoft.Extensions.Hosting;

namespace API.Services;

public interface ISecurityConfigurationHealthService
{
    SecurityConfigurationHealthReport Evaluate();
}

public sealed class SecurityConfigurationHealthService : ISecurityConfigurationHealthService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public SecurityConfigurationHealthService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public SecurityConfigurationHealthReport Evaluate() =>
        Evaluate(_configuration, _environment);

    public static SecurityConfigurationHealthReport Evaluate(
        IConfiguration configuration,
        IHostEnvironment environment,
        IReadOnlyCollection<string>? allowedOriginsOverride = null,
        string? jwtSecretOverride = null,
        string? effectiveJwtSecret = null)
    {
        var findings = new List<SecurityConfigurationFinding>();
        var isProduction = environment.IsProduction();
        var allowedOrigins = allowedOriginsOverride ?? ResolveAllowedOrigins(configuration);
        var jwtOverride = jwtSecretOverride ?? Environment.GetEnvironmentVariable("VSHIELD_JWT_SECRET");
        var jwtSecret = (effectiveJwtSecret ?? jwtOverride ?? configuration["JwtSettings:Secret"] ?? string.Empty).Trim();

        AddJwtFindings(configuration, findings, isProduction, jwtOverride, jwtSecret);
        AddSeedAdminFindings(configuration, findings, isProduction);
        AddOriginFindings(configuration, findings, isProduction, allowedOrigins);
        AddConnectionStringFindings(configuration, findings, isProduction);
        AddEvidenceSigningFindings(configuration, findings, isProduction);
        AddRateLimitFindings(configuration, findings, isProduction);
        AddGatewayHeaderFindings(configuration, findings, isProduction);

        var status = findings.Any(finding => finding.Status == SecurityConfigurationFindingStatuses.Fail)
            ? SecurityConfigurationHealthStatuses.Blocked
            : findings.Any(finding => finding.Status == SecurityConfigurationFindingStatuses.Warn)
                ? SecurityConfigurationHealthStatuses.Warning
                : SecurityConfigurationHealthStatuses.Healthy;

        return new SecurityConfigurationHealthReport(
            environment.EnvironmentName,
            isProduction,
            status,
            findings);
    }

    private static void AddJwtFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction,
        string? jwtOverride,
        string jwtSecret)
    {
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            findings.Add(FailOrWarn(
                "jwt.secret",
                isProduction,
                "JWT secret is missing or shorter than 32 characters.",
                "Set VSHIELD_JWT_SECRET to a strong secret before startup."));
            return;
        }

        if (string.IsNullOrWhiteSpace(jwtOverride))
        {
            findings.Add(FailOrWarn(
                "jwt.secret.source",
                isProduction,
                "JWT secret is loaded from repository-backed configuration.",
                "Set VSHIELD_JWT_SECRET through the deployment secret store."));
        }
        else
        {
            findings.Add(Pass(
                "jwt.secret.source",
                "JWT secret is provided by environment override."));
        }

        var issuer = configuration["JwtSettings:Issuer"];
        var audience = configuration["JwtSettings:Audience"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            findings.Add(FailOrWarn(
                "jwt.issuerAudience",
                isProduction,
                "JWT issuer or audience is missing.",
                "Set JwtSettings__Issuer and JwtSettings__Audience explicitly."));
        }
        else if (isProduction &&
                 string.Equals(issuer, "VShieldAPI", StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(audience, "VShieldClient", StringComparison.OrdinalIgnoreCase) &&
                 !HasEnvironmentOverride("JwtSettings__Issuer", "JwtSettings__Audience"))
        {
            findings.Add(FailOrWarn(
                "jwt.issuerAudience",
                true,
                "Production JWT issuer/audience still use repository defaults.",
                "Override JwtSettings__Issuer and JwtSettings__Audience in production."));
        }
        else
        {
            findings.Add(Pass(
                "jwt.issuerAudience",
                "JWT issuer and audience are configured."));
        }
    }

    private static void AddSeedAdminFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction)
    {
        var username = (Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_USERNAME") ??
                        configuration["SeedAdmin:Username"] ??
                        "admin").Trim();
        var password = Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_PASSWORD") ??
                       configuration["SeedAdmin:Password"] ??
                       "Admin@123";
        var hasOverrides =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_USERNAME")) &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VSHIELD_SEED_ADMIN_PASSWORD"));
        var unsafeDefault =
            string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(password, "Admin@123", StringComparison.Ordinal);

        if (!hasOverrides || unsafeDefault)
        {
            findings.Add(FailOrWarn(
                "seedAdmin.credentials",
                isProduction,
                "Seed admin credentials are not fully supplied by environment or still use the unsafe default.",
                "Set VSHIELD_SEED_ADMIN_USERNAME and VSHIELD_SEED_ADMIN_PASSWORD from a secret store."));
        }
        else
        {
            findings.Add(Pass(
                "seedAdmin.credentials",
                "Seed admin credentials are supplied by environment."));
        }
    }

    private static void AddOriginFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction,
        IReadOnlyCollection<string> allowedOrigins)
    {
        if (allowedOrigins.Count == 0)
        {
            findings.Add(FailOrWarn(
                "cors.origins",
                isProduction,
                "No allowed frontend origin is configured.",
                "Set AppSettings__AllowedOrigins__0 or AppSettings__FrontendUrl."));
            return;
        }

        var wildcardOrigins = allowedOrigins
            .Where(origin => origin == "*" || origin.Contains('*', StringComparison.Ordinal))
            .ToArray();
        if (wildcardOrigins.Length > 0)
        {
            findings.Add(FailOrWarn(
                "cors.origins",
                isProduction,
                "CORS contains wildcard origins.",
                "Use explicit HTTPS origins only."));
        }

        var invalidOrigins = allowedOrigins
            .Where(origin => !Uri.TryCreate(origin, UriKind.Absolute, out _))
            .ToArray();
        if (invalidOrigins.Length > 0)
        {
            findings.Add(FailOrWarn(
                "cors.originFormat",
                isProduction,
                "CORS origins contain invalid URI values.",
                "Fix invalid origins: " + string.Join(", ", invalidOrigins)));
        }

        var localOrigins = allowedOrigins
            .Where(IsLocalOrigin)
            .ToArray();
        if (isProduction && localOrigins.Length > 0)
        {
            findings.Add(FailOrWarn(
                "cors.productionLocalhost",
                true,
                "Production CORS includes localhost origins.",
                "Move localhost origins to Development only."));
        }

        if (wildcardOrigins.Length == 0 && invalidOrigins.Length == 0 && (!isProduction || localOrigins.Length == 0))
        {
            findings.Add(Pass(
                "cors.origins",
                "Allowed frontend origins are explicit."));
        }

        var frontendUrl = configuration["AppSettings:FrontendUrl"];
        if (isProduction && IsLocalOrigin(frontendUrl))
        {
            findings.Add(FailOrWarn(
                "frontend.url",
                true,
                "Production frontend URL points to localhost.",
                "Set AppSettings__FrontendUrl to the real HTTPS origin."));
        }
    }

    private static void AddConnectionStringFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var hasOverride = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            findings.Add(FailOrWarn(
                "connectionStrings.default",
                isProduction,
                "Default database connection string is missing.",
                "Set ConnectionStrings__DefaultConnection."));
            return;
        }

        var looksLocalDev = connectionString.Contains("Server=.;", StringComparison.OrdinalIgnoreCase) ||
                            connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                            connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase);

        if (isProduction && (!hasOverride || looksLocalDev))
        {
            findings.Add(FailOrWarn(
                "connectionStrings.default",
                true,
                "Production database connection still looks like local development configuration.",
                "Use a production DB connection string supplied by environment or secret store."));
        }
        else
        {
            findings.Add(Pass(
                "connectionStrings.default",
                "Database connection string source is acceptable for this environment."));
        }
    }

    private static void AddRateLimitFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction)
    {
        var backend = (configuration["RateLimiting:Backend"] ?? "Memory").Trim();
        if (isProduction && string.Equals(backend, "Memory", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(FailOrWarn(
                "rateLimiting.backend",
                true,
                "Rate limiting is configured as in-memory only.",
                "Set RateLimiting__Backend to Redis or SqlDistributed before medium/large production rollout."));
        }
        else
        {
            findings.Add(Pass(
                "rateLimiting.backend",
                $"Rate limiting backend is {backend}."));
        }
    }

    private static void AddEvidenceSigningFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction)
    {
        var signingKeyOverride = Environment.GetEnvironmentVariable("VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY");
        var configuredSigningKey = configuration["Evidence:ExportSigningKey"];
        var effectiveKey = signingKeyOverride ?? configuredSigningKey;

        if (string.IsNullOrWhiteSpace(effectiveKey) || effectiveKey.Length < 32)
        {
            findings.Add(FailOrWarn(
                "evidence.exportSigningKey",
                isProduction,
                "Evidence export signing key is missing or shorter than 32 characters.",
                "Set VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY from a secret store."));
            return;
        }

        if (isProduction && string.IsNullOrWhiteSpace(signingKeyOverride))
        {
            findings.Add(FailOrWarn(
                "evidence.exportSigningKey",
                true,
                "Production evidence export signing key is not supplied by environment.",
                "Use VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY for deploy-time signing material."));
            return;
        }

        findings.Add(Pass(
            "evidence.exportSigningKey",
            "Evidence export signing key is configured for this environment."));
    }

    private static void AddGatewayHeaderFindings(
        IConfiguration configuration,
        ICollection<SecurityConfigurationFinding> findings,
        bool isProduction)
    {
        var gatewayHeadersManagedByProxy = configuration.GetValue<bool>("Security:GatewayHeadersManagedByProxy");
        if (gatewayHeadersManagedByProxy)
        {
            findings.Add(Pass(
                "gateway.securityHeaders",
                "Reverse proxy gateway security headers are explicitly managed for this deployment."));
            return;
        }

        findings.Add(isProduction
            ? FailOrWarn(
                "gateway.securityHeaders",
                true,
                "API adds baseline security headers, but production CSP/HSTS should be enforced at the edge gateway too.",
                "Configure CSP, HSTS preload, TLS policy and upload/static-file policy at the reverse proxy, or set Security__GatewayHeadersManagedByProxy=true once enforced.")
            : new SecurityConfigurationFinding(
                "gateway.securityHeaders",
                SecurityConfigurationFindingSeverities.Medium,
                SecurityConfigurationFindingStatuses.Warn,
                "API baseline security headers are enabled; gateway CSP/HSTS still needs deployment evidence.",
                "Document edge gateway CSP/HSTS before production release."));
    }

    private static string[] ResolveAllowedOrigins(IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("AppSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var frontendUrl = configuration["AppSettings:FrontendUrl"];
        var allowedOrigins = configuredOrigins
            .Append(frontendUrl ?? string.Empty)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedOrigins.Length > 0)
            return allowedOrigins;

        return new[]
        {
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175"
        };
    }

    private static bool IsLocalOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        return origin.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
               origin.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               origin.Contains("[::1]", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasEnvironmentOverride(params string[] keys) =>
        keys.Any(key => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)));

    private static SecurityConfigurationFinding Pass(string key, string message) =>
        new(
            key,
            SecurityConfigurationFindingSeverities.Low,
            SecurityConfigurationFindingStatuses.Pass,
            message,
            string.Empty);

    private static SecurityConfigurationFinding FailOrWarn(
        string key,
        bool fail,
        string message,
        string remediation) =>
        new(
            key,
            fail ? SecurityConfigurationFindingSeverities.Critical : SecurityConfigurationFindingSeverities.Medium,
            fail ? SecurityConfigurationFindingStatuses.Fail : SecurityConfigurationFindingStatuses.Warn,
            message,
            remediation);
}

public static class SecurityConfigurationHealthStatuses
{
    public const string Healthy = "Healthy";
    public const string Warning = "Warning";
    public const string Blocked = "Blocked";
}

public static class SecurityConfigurationFindingStatuses
{
    public const string Pass = "Pass";
    public const string Warn = "Warn";
    public const string Fail = "Fail";
}

public static class SecurityConfigurationFindingSeverities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string Critical = "Critical";
}

public sealed record SecurityConfigurationHealthReport(
    string EnvironmentName,
    bool IsProduction,
    string Status,
    IReadOnlyList<SecurityConfigurationFinding> Findings);

public sealed record SecurityConfigurationFinding(
    string Key,
    string Severity,
    string Status,
    string Message,
    string Remediation);
