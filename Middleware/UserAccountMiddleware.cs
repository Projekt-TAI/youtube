using System.Security.Claims;
using TAIBackend.Model;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresUserAccountAttribute : Attribute
{
}

public class UserAccountMiddleware
{
    private readonly RequestDelegate _next;

    public UserAccountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    private bool RequiresUserAccount(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<RequiresUserAccountAttribute>();
        return attribute != null;
    }

    public async Task Invoke(HttpContext context, YoutubeContext _dbContext)
    {
        if (RequiresUserAccount(context))
        {
            if (await CheckDatabaseForEntry(context, _dbContext))
            {
                await _next(context);
            }
            else
            {
                // Entry not found, return a response or redirect as needed
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: HTTPContext does not incluse user details");
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task<bool> CheckDatabaseForEntry(HttpContext context, YoutubeContext _dbContext)
    {
        var User = context.User;
        if (User.FindFirst(ClaimTypes.NameIdentifier) == null ||
            User.FindFirst(ClaimTypes.GivenName) == null ||
            User.FindFirst(ClaimTypes.Name) == null ||
            User.FindFirst("urn:facebook:picture") == null ||
            User.FindFirst(ClaimTypes.Email) == null)
        {
            return false;
        }

        var account = new Account
        {
            Id = Int64.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
            Firstname = User.FindFirst(ClaimTypes.GivenName)!.Value,
            Fullname = User.FindFirst(ClaimTypes.Name)!.Value,
            Email = User.FindFirst(ClaimTypes.Email)!.Value,
            ProfilePicUrl = User.FindFirst("urn:facebook:picture")!.Value
        };

        if ((await _dbContext.Accounts.FindAsync(account.Id)) == null)
        {
            await _dbContext.Accounts.AddAsync(account);
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }
}

public static class UserMiddlewareExtensions
{
    public static IApplicationBuilder UseUserAccountMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserAccountMiddleware>();
    }
}