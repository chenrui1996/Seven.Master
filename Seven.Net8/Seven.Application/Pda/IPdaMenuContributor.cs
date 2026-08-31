using Seven.Application.Pda;

namespace Seven.Application.Pda;

/// <summary>向 PDA 宫格追加扩展菜单项。多实现均会合并进 GetMenu。</summary>
public interface IPdaMenuContributor
{
    IEnumerable<PdaMenuItemDto> GetExtraMenus();
}
