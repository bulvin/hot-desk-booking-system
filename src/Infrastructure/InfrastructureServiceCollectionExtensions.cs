using System.Security.Claims;
using System.Text;
using Application.Accounts;
using Application.Interfaces;
using Domain;
using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using Domain.Users;
using Infrastructure.Authentication;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddLogging();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IDeskRepository, DeskRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, UserContext>();
            services.AddDatabase(configuration);
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<JwtIssuer>();
            services.AddScoped<LocalTokenValidationEvents>();
            services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>();
            services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"))
                .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "Jwt:SigningKey must contain at least 32 bytes of secret key material.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience), "JWT issuer and audience are required.")
                .Validate(options => options.AccessTokenMinutes > 0 && options.SessionDays > 0, "Token lifetimes must be positive.")
                .ValidateOnStart();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((options, settings) =>
                {
                    options.MapInboundClaims = false;
                    options.IncludeErrorDetails = false;
                    options.EventsType = typeof(LocalTokenValidationEvents);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = settings.Value.Issuer,
                        ValidAudience = settings.Value.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.SigningKey)),
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = "email",
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyNames.Admin, policy => policy.RequireRole(PolicyNames.Admin))
                .AddPolicy(PolicyNames.Employee, policy => policy.RequireRole(PolicyNames.Employee));
            return services;
        }

        private void AddDatabase(IConfiguration configuration)
        {
            var connection = configuration.GetConnectionString("Database");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("ConnectionStrings:Database is required.");
            var seedSampleData = configuration.GetValue<bool>("Database:SeedSampleData");
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                options.UseNpgsql(connection);
                if (seedSampleData && provider.GetRequiredService<IHostEnvironment>().IsDevelopment())
                {
                    var clock = provider.GetRequiredService<IDateTimeProvider>();
                    options.UseSeeding((context, _) => SampleDataSeeder.Seed(context, clock))
                        .UseAsyncSeeding((context, _, token) => SampleDataSeeder.SeedAsync(context, clock, token));
                }
            });
        }
    }
}
