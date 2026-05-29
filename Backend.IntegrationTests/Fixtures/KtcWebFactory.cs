using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.IntegrationTests.Fixtures;

/// <summary>
/// Démarre le vrai backend ASP.NET Core en mémoire.
/// - Authentification remplacée par un handler de test (Support).
/// - IHostedService supprimés (pas de SqlTableDependency en test).
/// - Connection string héritée de appsettings.Development.json (base réelle, lecture seule).
/// </summary>
public class KtcWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Supprimer les hosted services (SqlTableDependency) — inutiles et bruyants en test
            services.RemoveAll<IHostedService>();

            // Remplacer l'auth JWT par un handler qui accepte tout avec Support
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

/// <summary>
/// Handler d'authentification de test : authentifie automatiquement chaque requête
/// avec le rôle Support (accès complet lecture + écriture).
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, "Support"),
            new Claim(ClaimTypes.Role, "Superviseur"),
        };

        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

