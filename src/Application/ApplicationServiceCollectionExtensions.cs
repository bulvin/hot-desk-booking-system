using Application.Behaviors;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;
            services.AddValidatorsFromAssembly(assembly);
            services.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
                options.GenerateTypesAsInternal = true;
                options.Assemblies = [typeof(ApplicationServiceCollectionExtensions)];
                options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
            });

            return services;
        }
    }
}
