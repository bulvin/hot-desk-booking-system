using System.Text.Json.Serialization;
using Application;
using Infrastructure;
using Infrastructure.Data.Converters;
using Serilog;
using Web.Api.Endpoints;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddHostedService<DatabaseInitializationHostedService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGenWithAuth(builder.Configuration);
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ProblemDetailsConfiguration.Customize;
    });


    var app = builder.Build();

    app.UseRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.OAuthClientId(builder.Configuration["Keycloak:SwaggerClientId"] ?? "hotdesk-swagger");
            options.OAuthUsePkce();
            options.EnablePersistAuthorization();
        });
    }

    app.MapAuthEndpoints();
    app.MapLocationEndpoints();
    app.MapDeskEndpoints();
    app.MapReservationEndpoints();

    await app.RunAsync();
}
#pragma warning disable S2139
catch (Exception exception)
#pragma warning restore S2139
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
