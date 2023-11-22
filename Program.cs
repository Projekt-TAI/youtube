using Microsoft.OpenApi.Models;
using TAIBackend.Entities;
using TAIBackend.DataBase;
using TAIBackend.Filler;
using System;
using System.Net.NetworkInformation;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen(c =>
{
     c.SwaggerDoc("v1", new OpenApiInfo { Title = "TAI API", Description = "TAI Project video player api", Version = "v1.0.0" });
});

builder.Services.AddControllers();

var app = builder.Build();


await app.Services.CreateScope().ServiceProvider.GetRequiredService<UsersFiller>().Seed();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
   c.SwaggerEndpoint("/swagger/v1/swagger.json", "TAI API V1");
});


app.MapControllers();

app.Run();

