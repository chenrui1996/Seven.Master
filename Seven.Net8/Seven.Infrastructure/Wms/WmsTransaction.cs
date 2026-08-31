using Microsoft.EntityFrameworkCore;

namespace Seven.Infrastructure.Wms;

internal static class WmsTransaction
{
    public static async Task ExecuteAsync(DbContext db, Func<Task> action, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            await action();
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await action();
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
