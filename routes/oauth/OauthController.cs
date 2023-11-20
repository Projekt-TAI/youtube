using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace TAIBackend.routes.oauth;

[Route("oauth/v2")]
public class OauthController(IConfiguration configuration) : Controller
{
    [Route("oidc-callback")]
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

        
        var json = await response.Content.ReadAsStringAsync();
        
        if (response.StatusCode == HttpStatusCode.OK) return Json(json);
        await Console.Out.WriteLineAsync(response.ToJson());
        return BadRequest();

    }
    
    [Route("signin-facebook")]
    public Task<IActionResult> SigninFacebook()
    {
        const string tokenEndpoint = "https://www.facebook.com/v11.0/dialog/oauth";

        var codeChallenge = configuration["Authentication:Facebook:CodeChallenge"] ?? string.Empty;
        
        var nonce = Guid.NewGuid();
        var nonceReq = nonce.ToString();
        
        var appId = configuration["Authentication:Facebook:AppId"] ?? string.Empty;
        
        string requestWithVariables =
            $"{tokenEndpoint}?client_id={appId}&code_challenge={Base64UrlTextEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeChallenge)))}&scope=openid&response_type=code&redirect_uri=https://{string.Join("/", 
                ControllerContext.HttpContext.Request.Host, "oauth/v2/oidc-callback")}&nonce={nonceReq}&code_challenge_method=S256";

        return Task.FromResult<IActionResult>(Redirect(requestWithVariables));
    }
}
