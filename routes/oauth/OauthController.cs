using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TAIBackend.routes.oauth;

[Route("oauth/v2")]
public class OauthController(IConfiguration configuration) : Controller
{
    public class OidcTokenResponse
    {
        [JsonPropertyName("token_type")]
        public required string TokenType { get; set; }
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("id_token")]
        public required string IdToken { get; set; }
    }
    
    [HttpGet("oidc-callback")]
    public async Task<IActionResult> Callback()
    {
        var code = Request.Query["code"];
        var httpClient = new HttpClient();
        var appId = configuration["Authentication:Facebook:AppId"] ?? string.Empty;
        var appSecret = configuration["Authentication:Facebook:AppSecret"] ?? string.Empty;
        var codeChallenge = configuration["Authentication:Facebook:CodeChallenge"] ?? string.Empty;
        var requestUri =
            $"https://graph.facebook.com/v18.0/oauth/access_token?client_id={appId}&redirect_uri=https://{string.Join("/", ControllerContext.HttpContext.Request.Host, "oauth/v2/oidc-callback")}&client_secret={appSecret}&code_verifier={codeChallenge}&code={code}";
        
        var response = await httpClient.GetAsync(requestUri);
        var openIdConnectMessage = await response.Content.ReadAsStringAsync();
        var authenticationResult =  JsonSerializer.Deserialize<OidcTokenResponse>(openIdConnectMessage);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Debug.Assert(authenticationResult != null, nameof(authenticationResult) + " != null");
            HttpContext.Session.SetString("access_token",authenticationResult.AccessToken);
            HttpContext.Session.SetString("id_token",authenticationResult.IdToken);
            return Redirect($"https://{ControllerContext.HttpContext.Request.Host}");
        }
        await Console.Out.WriteLineAsync(response.ToJson());
        return BadRequest();

    }
    
    [HttpGet("signin-facebook")]
    public Task<IActionResult> SigninFacebook()
    {
        var authProperties = new AuthenticationProperties{
            RedirectUri = Url.Action("LoginCallback", "Facebook")
        };            
        return Challenge(authProperties, "Facebook");
    }
}
