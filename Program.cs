using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;

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
            policy.WithOrigins("*");
        });


    options.AddPolicy("AuthenticatedPolicy",
        policy =>
        {
            policy.WithOrigins("https://localhost:3000");
            policy.AllowCredentials();
        });
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
    options.AccessDeniedPath = "/access-denied";
    options.SaveTokens = true;

    options.Fields.Add("picture");
    options.ClaimActions.MapCustomJson("urn:facebook:picture", claim => claim.GetProperty("picture").GetProperty("data").GetString("url"));
}).AddCookie(options =>
{
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

app.UseCors();
app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();

app.Run();
