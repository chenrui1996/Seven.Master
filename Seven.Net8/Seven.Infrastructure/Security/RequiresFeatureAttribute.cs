using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Seven.Domain.Common;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Security;

/// <summary>功能关闭时返回 404 + 业务错误，避免简单项目暴露复杂 API</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresFeatureAttribute : Attribute, IFilterFactory
{
    public string Feature { get; }
    public bool IsReusable => false;

    public RequiresFeatureAttribute(string feature) => Feature = feature;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new RequiresFeatureFilter(Feature, serviceProvider.GetRequiredService<IOptions<FeatureOptions>>());
}

internal sealed class RequiresFeatureFilter : IAsyncActionFilter
{
    private readonly string _feature;
    private readonly FeatureOptions _features;

    public RequiresFeatureFilter(string feature, IOptions<FeatureOptions> features)
    {
        _feature = feature;
        _features = features.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_features.IsEnabled(_feature))
        {
            context.Result = new ObjectResult(WebResponseContent.Error($"功能未启用: {_feature}"))
            {
                StatusCode = StatusCodes.Status404NotFound,
            };
            return;
        }

        await next();
    }
}
