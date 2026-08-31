using Seven.Application.Pda;

namespace Seven.Business;

/// <summary>仓内调拨 PDA 宫格贡献（doc/23 样板）。</summary>
public sealed class TransferOrderPdaMenuContributor : IPdaMenuContributor
{
    public IEnumerable<PdaMenuItemDto> GetExtraMenus()
    {
        yield return new PdaMenuItemDto(
            "transfer",
            "仓内调拨",
            "/pages/transfer/index",
            "Pda.Transfer");
    }
}
