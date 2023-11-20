using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
     c.SwaggerDoc("v1", new OpenApiInfo { Title = "TAI API", Description = "TAI Project video player api", Version = "v1.0.0" });
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
   c.SwaggerEndpoint("/swagger/v1/swagger.json", "TAI API V1");
});

int[] data = {1,2,3,4,5};

app.MapGet("/", () => "Hello World!");
app.MapGet("/products/{id}", (int id) => id);

app.Run();
