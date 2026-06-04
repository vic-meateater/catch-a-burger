using System.Collections.Generic;
using BurgerCatch.Core.Economy;
using BurgerCatch.Core.Saves;
using BurgerCatch.Data;
using BurgerCatch.Events;
using Zenject;

namespace BurgerCatch.Core.Shop
{
  /// <summary>
  /// Логика магазина за мягкую валюту поверх PlayerData. Списания идут через
  /// CurrencyService, изменения сохраняются и сообщаются сигналами (для UI).
  /// </summary>
  public sealed class ShopService : IShopService
  {
    private readonly ISaveService _saveService;
    private readonly ICurrencyService _currency;
    private readonly SkinCatalog _skinCatalog;
    private readonly BoostCatalog _boostCatalog;
    private readonly SignalBus _signalBus;

    public ShopService(
      ISaveService saveService,
      ICurrencyService currency,
      SkinCatalog skinCatalog,
      BoostCatalog boostCatalog,
      SignalBus signalBus)
    {
      _saveService = saveService;
      _currency = currency;
      _skinCatalog = skinCatalog;
      _boostCatalog = boostCatalog;
      _signalBus = signalBus;
    }

    public bool TryBuySkin(string skinId)
    {
      var def = _skinCatalog.GetById(skinId);
      if (def == null) return false;          // нет в каталоге
      if (def.IapOnly) return false;          // только за реальные деньги — не здесь
      if (IsSkinOwned(skinId)) return false;  // уже куплен

      if (!_currency.TrySpend(def.Price)) return false;

      OwnedSkins.Add(skinId);
      _saveService.Save();
      _signalBus.Fire(new SkinPurchasedSignal(skinId));
      return true;
    }

    public bool TryBuyBoost(BoostType type)
    {
      var def = _boostCatalog.GetByType(type);
      if (def == null) return false;

      if (!_currency.TrySpend(def.Price)) return false;

      var inv = _saveService.Data.BoostInventory ??= new Dictionary<BoostType, int>();
      inv.TryGetValue(type, out int count);
      inv[type] = count + 1;

      _saveService.Save();
      _signalBus.Fire(new BoostPurchasedSignal(type));
      return true;
    }

    public void SelectSkin(string skinId)
    {
      if (!IsSkinOwned(skinId)) return;       // выбрать можно только купленный

      _saveService.Data.SelectedSkin = skinId;
      _saveService.Save();
      _signalBus.Fire(new SkinSelectedSignal(skinId));
    }

    public bool IsSkinOwned(string skinId) => OwnedSkins.Contains(skinId);

    private List<string> OwnedSkins =>
      _saveService.Data.OwnedSkins ??= new List<string>();
  }
}
