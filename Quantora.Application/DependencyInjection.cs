using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Quantora.Application.Behaviors;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Quantora.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

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
