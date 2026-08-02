using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;

namespace Seven.Business;

/// <summary>业务模块 DI</summary>
public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddSevenBusiness(this IServiceCollection services)
    {
        services.AddScoped<IWorkFlowService, WorkFlowService>();
        return services;
    }
}
