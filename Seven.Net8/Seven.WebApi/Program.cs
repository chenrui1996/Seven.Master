using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Seven.Application.Interfaces;
using Seven.Builder;
using Seven.Business;
using Seven.Infrastructure;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Middleware;
using Seven.Infrastructure.Persistence;
using Seven.WebApi;
using Seven.WebApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/seven-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

var features = builder.Configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>()
    ?? new FeatureOptions();

builder.Services.AddSevenInfrastructure(builder.Configuration);
builder.Services.AddSevenBusiness();
builder.Services.AddScoped<IBuilderService, BuilderService>();
builder.Services.AddScoped<IMessagePushService, MessagePushService>();
builder.Services.AddScoped<IAlarmPushService, AlarmPushService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    const string apiVersion = "v1";
    c.SwaggerDoc("system", new OpenApiInfo
    {
        Title = "Seven System API",
        Version = apiVersion,
        Description = "认证、用户、角色、菜单、字典、日志、定时任务等",
    });
    c.SwaggerDoc("workflow", new OpenApiInfo
    {
        Title = "Seven Workflow API",
        Version = apiVersion,
        Description = "工作流定义与审批实例",
    });
    c.SwaggerDoc("builder", new OpenApiInfo
    {
        Title = "Seven Builder API",
        Version = apiVersion,
        Description = "代码生成与表结构",
    });
    c.SwaggerDoc("ops", new OpenApiInfo
    {
        Title = "Seven Ops API",
        Version = apiVersion,
        Description = "告警、消息队列、文件、邮件、通知、健康检查",
    });

    c.TagActionsBy(api =>
    {
        if (api.ActionDescriptor is ControllerActionDescriptor cad)
            return [cad.ControllerName];
        return ["Default"];
    });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.ActionDescriptor is not ControllerActionDescriptor cad)
            return false;
        var group = cad.ControllerTypeInfo
            .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: true)
            .Cast<ApiExplorerSettingsAttribute>()
            .FirstOrDefault()?.GroupName ?? "system";
        return string.Equals(docName, group, StringComparison.OrdinalIgnoreCase);
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

// 始终注册 SignalR 服务（推送实现依赖 IHubContext）；是否对外 MapHub 由 Features.SignalR 控制
builder.Services.AddSignalR();
builder.Services.AddResponseCompression();
builder.Services.AddSingleton<SevenInfrastructureHealthCheck>();
builder.Services.AddSingleton<RedisHealthCheck>();
builder.Services.AddSingleton<RabbitMqHealthCheck>();
builder.Services.AddSingleton<MinioHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<SevenInfrastructureHealthCheck>("infrastructure")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<RabbitMqHealthCheck>("rabbitmq")
    .AddCheck<MinioHealthCheck>("minio");

var corsOrigins =
    builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.Origins
    ?? "http://localhost:5173";
var isDev = builder.Environment.IsDevelopment();
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
    {
        if (isDev)
        {
            p.SetIsOriginAllowed(static origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                return origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)
                       || origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase);
            });
        }
        else
        {
            p.WithOrigins(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        p.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    })
);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TraceIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<IpWhitelistMiddleware>();
if (features.Idempotency)
    app.UseMiddleware<IdempotencyMiddleware>();
app.UseResponseCompression();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/system/swagger.json", "System v1");
        c.SwaggerEndpoint("/swagger/workflow/swagger.json", "Workflow v1");
        c.SwaggerEndpoint("/swagger/builder/swagger.json", "Builder v1");
        c.SwaggerEndpoint("/swagger/ops/swagger.json", "Ops v1");
    });
}

app.UseStaticFiles();
var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "Upload");
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/upload",
    FileProvider = new PhysicalFileProvider(uploadRoot),
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 租户上下文
app.Use(async (ctx, next) =>
{
    var db = ctx.RequestServices.GetService<SevenDbContext>();
    var init = ctx.RequestServices.GetService<ITenantContextInitializer>();
    if (db != null && init != null) init.Apply(db);
    await next();
});

app.MapControllers();
if (features.SignalR)
{
    app.MapHub<MessageHub>("/hub/message");
    if (features.Alarm)
        app.MapHub<AlarmHub>("/hub/alarm");
}
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    await DbSeeder.SeedAsync(app.Services);
}

app.Run();

public partial class Program;
