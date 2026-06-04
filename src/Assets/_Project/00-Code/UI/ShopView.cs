using System.Collections.Generic;
using BurgerCatch.Core.Economy;
using BurgerCatch.Core.Iap;
using BurgerCatch.Core.Saves;
using BurgerCatch.Core.Shop;
using BurgerCatch.Data;
using BurgerCatch.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Магазин: скины (за валюту или инап), бусты (за валюту), no-ads (инап).
  /// ТОНКИЙ слой: все покупки — через IShopService/IIapService. Вью только
  /// показывает и дёргает сервисы; валюту/владение не меняет само.
  /// </summary>
  public sealed class ShopView : MonoBehaviour
  {
    // Идентификатор инапа «убрать рекламу» (должен совпадать с YandexIapService).
    private const string NoAdsIapId = "noads";

    [Tooltip("Текст баланса валюты.")]
    [SerializeField] private TMP_Text _balanceText;

    [Header("Скины")]
    [Tooltip("Контейнер карточек скинов.")]
    [SerializeField] private Transform _skinsParent;

    [Tooltip("Префаб карточки скина (компонент SkinCardView).")]
    [SerializeField] private SkinCardView _skinCardPrefab;

    [Header("Бусты")]
    [Tooltip("Контейнер карточек бустов.")]
    [SerializeField] private Transform _boostsParent;

    [Tooltip("Префаб карточки буста (компонент ShopItemCardView).")]
    [SerializeField] private ShopItemCardView _boostCardPrefab;

    [Header("Прочее")]
    [Tooltip("Кнопка «убрать рекламу» (инап).")]
    [SerializeField] private Button _noAdsButton;

    [Tooltip("Кнопка закрыть магазин.")]
    [SerializeField] private Button _closeButton;

    private SignalBus _signalBus;
    private ICurrencyService _currency;
    private IShopService _shop;
    private IIapService _iap;
    private SkinCatalog _skinCatalog;
    private BoostCatalog _boostCatalog;
    private ISaveService _saveService;

    // Созданные карточки — для обновления состояний по сигналам.
    private readonly List<SkinCardView> _skinCards = new List<SkinCardView>();
    private readonly List<ShopItemCardView> _boostCards = new List<ShopItemCardView>();

    [Inject]
    public void Construct(
      SignalBus signalBus,
      ICurrencyService currency,
      IShopService shop,
      IIapService iap,
      SkinCatalog skinCatalog,
      BoostCatalog boostCatalog,
      ISaveService saveService)
    {
      _signalBus = signalBus;
      _currency = currency;
      _shop = shop;
      _iap = iap;
      _skinCatalog = skinCatalog;
      _boostCatalog = boostCatalog;
      _saveService = saveService;
    }

    private void OnEnable()
    {
      _signalBus.Subscribe<CurrencyChangedSignal>(OnCurrencyChanged);
      _signalBus.Subscribe<SkinPurchasedSignal>(OnSkinPurchased);
      _signalBus.Subscribe<BoostPurchasedSignal>(OnBoostPurchased);
      _signalBus.Subscribe<NoAdsActivatedSignal>(OnNoAdsActivated);

      if (_noAdsButton != null) _noAdsButton.onClick.AddListener(OnNoAdsClicked);
      if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);

      // Текущее состояние сразу.
      RefreshBalance();
      BuildSkins();
      BuildBoosts();
      RefreshNoAdsButton();
    }

    private void OnDisable()
    {
      _signalBus.Unsubscribe<CurrencyChangedSignal>(OnCurrencyChanged);
      _signalBus.Unsubscribe<SkinPurchasedSignal>(OnSkinPurchased);
      _signalBus.Unsubscribe<BoostPurchasedSignal>(OnBoostPurchased);
      _signalBus.Unsubscribe<NoAdsActivatedSignal>(OnNoAdsActivated);

      if (_noAdsButton != null) _noAdsButton.onClick.RemoveListener(OnNoAdsClicked);
      if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    // --- Баланс ---

    private void OnCurrencyChanged(CurrencyChangedSignal s)
    {
      if (_balanceText != null) _balanceText.text = s.Balance.ToString();
    }

    private void RefreshBalance()
    {
      if (_balanceText != null) _balanceText.text = _currency.Balance.ToString();
    }

    // --- Скины ---

    private void BuildSkins()
    {
      for (int i = 0; i < _skinCards.Count; i++)
        if (_skinCards[i] != null) Destroy(_skinCards[i].gameObject);
      _skinCards.Clear();

      if (_skinCatalog == null || _skinCardPrefab == null || _skinsParent == null)
        return;

      var skins = _skinCatalog.Skins;
      for (int i = 0; i < skins.Count; i++)
      {
        var def = skins[i];
        if (def == null) continue;

        SkinCardView card = Instantiate(_skinCardPrefab, _skinsParent);
        card.Bind(def, OnSkinClicked);
        _skinCards.Add(card);
      }

      RefreshSkinStates();
    }

    // Нажали карточку скина в магазине.
    private void OnSkinClicked(string skinId)
    {
      var def = _skinCatalog.GetById(skinId);
      if (def == null) return;

      if (_shop.IsSkinOwned(skinId)) return; // уже куплен — ничего

      if (def.IapOnly)
      {
        // Только за реальные деньги — запускаем инап.
        _iap.Buy(def.IapId);
        return;
      }

      // Обычная покупка за валюту.
      bool ok = _shop.TryBuySkin(skinId);
      if (!ok)
      {
        // TODO: фидбек «не хватает валюты» (тряхнуть баланс/звук). Разработчик добавит.
        Debug.Log($"[ShopView] Не удалось купить скин {skinId} (не хватает валюты?).");
      }
    }

    private void OnSkinPurchased(SkinPurchasedSignal s) => RefreshSkinStates();

    private void RefreshSkinStates()
    {
      string selected = _saveService.Data.SelectedSkin;

      for (int i = 0; i < _skinCards.Count; i++)
      {
        SkinCardView card = _skinCards[i];
        if (card == null) continue;

        bool owned = _shop.IsSkinOwned(card.SkinId);
        bool isSelected = card.SkinId == selected;
        card.SetState(owned, isSelected);
      }
    }

    // --- Бусты ---

    private void BuildBoosts()
    {
      for (int i = 0; i < _boostCards.Count; i++)
        if (_boostCards[i] != null) Destroy(_boostCards[i].gameObject);
      _boostCards.Clear();

      if (_boostCatalog == null || _boostCardPrefab == null || _boostsParent == null)
        return;

      var boosts = _boostCatalog.Boosts;
      for (int i = 0; i < boosts.Count; i++)
      {
        var def = boosts[i];
        if (def == null) continue;

        ShopItemCardView card = Instantiate(_boostCardPrefab, _boostsParent);
        card.Bind(def, OnBoostClicked);
        _boostCards.Add(card);
      }
    }

    private void OnBoostClicked(BoostType type)
    {
      bool ok = _shop.TryBuyBoost(type);
      if (!ok)
      {
        // TODO: фидбек «не хватает валюты». Разработчик добавит.
        Debug.Log($"[ShopView] Не удалось купить буст {type} (не хватает валюты?).");
      }
    }

    private void OnBoostPurchased(BoostPurchasedSignal s)
    {
      // Карточка буста-расходника визуально не меняется (можно показать счётчик —
      // по желанию разработчика). Баланс обновится своим сигналом.
    }

    // --- No-ads ---

    private void OnNoAdsClicked()
    {
      _iap.Buy(NoAdsIapId);
    }

    private void OnNoAdsActivated(NoAdsActivatedSignal s) => RefreshNoAdsButton();

    // Куплено no-ads — кнопка больше не нужна.
    private void RefreshNoAdsButton()
    {
      if (_noAdsButton != null)
        _noAdsButton.gameObject.SetActive(!_saveService.Data.NoAds);
    }

    // --- Закрытие ---

    // Закрыть магазин: гасим саму панель (это вью на корне панели).
    private void OnCloseClicked()
    {
      gameObject.SetActive(false);
    }
  }
}
