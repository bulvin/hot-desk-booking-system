using System.Text.Json.Serialization;
using Application;
using Infrastructure;
using Infrastructure.Data.Converters;
using Microsoft.AspNetCore.Diagnostics;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    });

builder.Services.AddSingleton<IExceptionHandler>(_ => new GlobalExceptionHandler());
builder.Services.AddProblemDetails();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseExceptionHandler();

await app.EnsureDatabaseCreated();
await app.RunAsync();
