using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Construct.WebUI.Server.Authentication
{

    /// <summary>
    /// Dummy om authenticatie te regelen, zonder echte login.
    /// </summary>
    public class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public NoAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) // ✅ geen ISystemClock meer
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Maak een nep-gebruiker aan
            var identity = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, "Demo User"),
            new Claim(ClaimTypes.Role, "Admin")
        }, Scheme.Name);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

}
