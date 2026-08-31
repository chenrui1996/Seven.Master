using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Seven.Domain.Common;
using Seven.Infrastructure.Crud;

namespace Seven.WebApi.Controllers.Crud;

/// <summary>CRUD Excel 端点共享实现（Import / Export / exportTemplate）。</summary>
public static class EntityCrudExcel
{
    public static async Task<IActionResult> ExportAsync<T>(
        ControllerBase controller,
        EntityCrudService<T> crud,
        PageDataOptions options,
        CancellationToken ct)
        where T : class, new()
    {
        var bytes = await crud.ExportAsync(options, ct);
        return controller.File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{typeof(T).Name}.xlsx");
    }

    public static IActionResult ExportTemplate<T>(ControllerBase controller, EntityCrudService<T> crud)
        where T : class, new()
    {
        var bytes = crud.ExportTemplate();
        return controller.File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{typeof(T).Name}_template.xlsx");
    }

    public static Task<WebResponseContent> ImportAsync<T>(
        EntityCrudService<T> crud,
        IFormFile file,
        CancellationToken ct)
        where T : class, new() =>
        crud.ImportAsync(file, ct);
}
