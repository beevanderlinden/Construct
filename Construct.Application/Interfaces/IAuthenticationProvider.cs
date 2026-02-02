using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

public class HttpContextAuthenticationProvider : IAuthenticationProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextAuthenticationProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await _httpContextAccessor.HttpContext
            ?.GetTokenAsync("access_token")!;

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers["Authorization"] = [$"Bearer {accessToken}"];
        }
    }
}
