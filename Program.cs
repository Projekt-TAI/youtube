using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TAIBackend.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TAI API", Description = "TAI Project video player api", Version = "v1.0.0" });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("https://localhost:3000", "https://localhost:5000", "https://youtube-tai.netlify.app");
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.AllowCredentials();
        });


    options.AddPolicy("AuthenticatedPolicy",
        policy =>
        {
            policy.WithOrigins("https://localhost:3000", "https://localhost:5000", "https://youtube-tai.netlify.app");
            policy.AllowCredentials();
        });
});

builder.Services.AddDbContext<YoutubeContext>(options =>
            options.UseInMemoryDatabase("youtube"));

builder.Services.Configure<KestrelServerOptions>(options =>
{
     options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddFacebook(options =>
{
    options.AppId = Environment.GetEnvironmentVariable("APP_ID") ?? throw new InvalidOperationException();
    options.AppSecret = Environment.GetEnvironmentVariable("APP_SECRET") ?? throw new InvalidOperationException();
    options.CorrelationCookie.Path = "/";
    options.AccessDeniedPath = "/account/access-denied";
    options.SaveTokens = true;

    // TODO Paging
    options.Scope.Add("user_friends");
    options.Fields.Add("friends");

    options.Fields.Add("picture");
    options.ClaimActions.MapCustomJson("urn:facebook:picture", claim => claim.GetProperty("picture").GetProperty("data").GetString("url"));
    options.ClaimActions.MapCustomJson("urn:facebook:friends", claim =>
    {
        try
        {
            return claim.GetProperty("friends").GetProperty("data").ToString();
        }
        catch
        {
            return null;
        }
    });
}).AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.LoginPath = "/account/facebook-login";
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };

    /* TODO: enable when finished with dev
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Cookie.MaxAge = options.ExpireTimeSpan;
    options.SlidingExpiration = true;
    */
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TAI API V1");
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.Use((context, next) =>
{
    if (context.Request.Headers["x-forwarded-proto"] == "https")
    {
        context.Request.Scheme = "https";
    }
    return next();
});

app.UseCors();
app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();
app.UseUserAccountMiddleware();

app.Run();

