using Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;


namespace Application;

public static class ApplicationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(assembly);
            return services;
        }
    }
}