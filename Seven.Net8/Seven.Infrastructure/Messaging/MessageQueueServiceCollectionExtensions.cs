using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Messaging.Consumers;

namespace Seven.Infrastructure.Messaging;

/// <summary>
/// 消息队列 DI 扩展（RabbitMQ + MassTransit / NoOp）
/// </summary>
public static class MessageQueueServiceCollectionExtensions
{
    /// <summary>注册 Seven 消息队列</summary>
    public static IServiceCollection AddSevenMessageQueue(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessageQueueOptions>(configuration.GetSection(MessageQueueOptions.SectionName));
        services.AddScoped<IMessageQueueService, MessageQueueService>();

        var options = configuration.GetSection(MessageQueueOptions.SectionName).Get<MessageQueueOptions>()
            ?? new MessageQueueOptions();
        var provider = Enum.TryParse<MessageQueueProvider>(options.Provider, true, out var p)
            ? p
            : MessageQueueProvider.None;

        if (provider == MessageQueueProvider.RabbitMQ)
        {
            services.AddMassTransit(x =>
            {
                if (options.EnableConsumers)
                    x.AddConsumer<AlarmRaiseConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    var mq = options.RabbitMq;
                    cfg.Host(mq.Host, mq.Port, mq.VirtualHost, h =>
                    {
                        h.Username(mq.Username);
                        h.Password(mq.Password);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            });

            services.AddScoped<IMessagePublisher, MassTransitMessagePublisher>();
        }
        else
        {
            services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();
        }

        return services;
    }
}
