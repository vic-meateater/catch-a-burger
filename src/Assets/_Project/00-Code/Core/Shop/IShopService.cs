using BurgerCatch.Data;

namespace BurgerCatch.Core.Shop
{
  /// <summary>Покупки за мягкую валюту и выбор скина. Инапы — отдельно (IIapService).</summary>
  public interface IShopService
  {
    /// <summary>Купить скин за валюту. false, если не хватает или скин только за инап.</summary>
    bool TryBuySkin(string skinId);

    /// <summary>Купить буст-расходник за валюту. false, если не хватает.</summary>
    bool TryBuyBoost(BoostType type);

    /// <summary>Выбрать уже купленный скин.</summary>
    void SelectSkin(string skinId);

    bool IsSkinOwned(string skinId);
  }
}
