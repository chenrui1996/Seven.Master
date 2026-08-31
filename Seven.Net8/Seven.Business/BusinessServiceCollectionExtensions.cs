using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Business;
using Seven.Application.Interfaces;
using Seven.Application.Pda;

namespace Seven.Business;

/// <summary>业务模块 DI（扩展单据、钩子、PDA 菜单贡献）</summary>
public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddSevenBusiness(this IServiceCollection services)
    {
        services.AddScoped<IWorkFlowService, WorkFlowService>();
        services.AddScoped<ITransferOrderService, TransferOrderService>();
        // 项目可 Replace 为自定义 / Composite 钩子
        services.AddScoped<IWmsExtensionHooks, NoOpWmsExtensionHooks>();
        services.AddScoped<IPdaMenuContributor, TransferOrderPdaMenuContributor>();
        return services;
    }
}
