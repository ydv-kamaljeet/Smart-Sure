using System;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSure.Shared.Infrastructure.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
                var rabbitMqPort = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672;
                var rabbitMqUser = configuration["RabbitMQ:Username"] ?? "smartsure";
                var rabbitMqPass = configuration["RabbitMQ:Password"] ?? "smartsure_dev";

                cfg.Host(rabbitMqHost, (ushort)rabbitMqPort, "/", h =>
                {
                    h.Username(rabbitMqUser);
                    h.Password(rabbitMqPass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
