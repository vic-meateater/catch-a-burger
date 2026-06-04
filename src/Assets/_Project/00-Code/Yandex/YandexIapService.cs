using System;
using System.Collections.Generic;
using BurgerCatch.Core.Iap;
using BurgerCatch.Core.Saves;
using BurgerCatch.Data;
using BurgerCatch.Events;
using Zenject;
using YG;

namespace BurgerCatch.Yandex
{
  /// <summary>
  /// Инапы через Plugin YG. ЕДИНСТВЕННОЕ место с YG2.* по покупкам.
  /// Подписан на onPurchaseSuccess: по id определяет товар и применяет эффект.
  /// </summary>
  public sealed class YandexIapService : IIapService, IInitializable, IDisposable
  {
    // TODO: сверить реальный id «убрать рекламу» из дашборда Яндекса.
    private const string NoAdsId = "noads";

    private readonly ISaveService _saveService;
    private readonly SkinCatalog _skinCatalog;
    private readonly SignalBus _signalBus;

    public YandexIapService(ISaveService saveService, SkinCatalog skinCatalog, SignalBus signalBus)
    {
      _saveService = saveService;
      _skinCatalog = skinCatalog;
      _signalBus = signalBus;
    }

    public void Initialize()
    {
      YG2.onPurchaseSuccess += OnPurchaseSuccess;
    }

    public void Dispose()
    {
      YG2.onPurchaseSuccess -= OnPurchaseSuccess;
    }

    public void Buy(string iapId)
    {
      YG2.BuyPayments(iapId);
    }

    // Покупка подтверждена платформой — применяем эффект по id.
    private void OnPurchaseSuccess(string id)
    {
      if (id == NoAdsId)
      {
        _saveService.Data.NoAds = true;
        _saveService.Save();
        _signalBus.Fire(new NoAdsActivatedSignal());
        return;
      }

      // Иначе ищем скин, чей IapId совпал с купленным.
      var skin = FindSkinByIap(id);
      if (skin == null) return;

      var owned = _saveService.Data.OwnedSkins ??= new List<string>();
      if (!owned.Contains(skin.Id))
        owned.Add(skin.Id);

      _saveService.Save();
      _signalBus.Fire(new SkinPurchasedSignal(skin.Id));
    }

    private SkinDefinition FindSkinByIap(string iapId)
    {
      var skins = _skinCatalog.Skins;
      for (int i = 0; i < skins.Count; i++)
        if (skins[i] != null && skins[i].IapOnly && skins[i].IapId == iapId)
          return skins[i];
      return null;
    }
  }
}
