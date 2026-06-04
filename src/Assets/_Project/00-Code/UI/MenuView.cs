using System.Collections.Generic;
using BurgerCatch.Core.Economy;
using BurgerCatch.Core.Flow;
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
  /// Главное меню: баланс, выбор героя, кнопки играть/магазин/лидерборд.
  /// ТОНКИЙ слой: выбор героя идёт через IShopService, баланс — из сервиса/сигнала.
  /// </summary>
  public sealed class MenuView : MonoBehaviour
  {
    [Tooltip("Текст баланса мягкой валюты.")]
    [SerializeField] private TMP_Text _balanceText;

    [Header("Герои")]
    [Tooltip("Контейнер для карточек героев.")]
    [SerializeField] private Transform _skinsParent;

    [Tooltip("Префаб карточки героя (компонент SkinCardView на префабе).")]
    [SerializeField] private SkinCardView _skinCardPrefab;

    [Header("Кнопки")]
    [Tooltip("Играть — грузит сцену забега.")]
    [SerializeField] private Button _playButton;

    [Tooltip("Магазин — открывает панель магазина.")]
    [SerializeField] private Button _shopButton;

    [Tooltip("Лидерборд.")]
    [SerializeField] private Button _leaderboardButton;

    [Tooltip("Панель магазина (включаем по кнопке). Если магазин — отдельная сцена, замените на загрузку.")]
    [SerializeField] private GameObject _shopPanel;

    private SignalBus _signalBus;
    private ICurrencyService _currency;
    private IShopService _shop;
    private SkinCatalog _skinCatalog;
    private ISaveService _saveService;
    private ISceneLoader _sceneLoader;
    private GameFlowController _flow;

    // Созданные карточки героев — чтобы обновлять их состояние по сигналам.
    private readonly List<SkinCardView> _skinCards = new List<SkinCardView>();

    [Inject]
    public void Construct(
      SignalBus signalBus,
      ICurrencyService currency,
      IShopService shop,
      SkinCatalog skinCatalog,
      ISaveService saveService,
      ISceneLoader sceneLoader,
      GameFlowController flow)
    {
      _signalBus = signalBus;
      _currency = currency;
      _shop = shop;
      _skinCatalog = skinCatalog;
      _saveService = saveService;
      _sceneLoader = sceneLoader;
      _flow = flow;
    }

    private void OnEnable()
    {
      _signalBus.Subscribe<CurrencyChangedSignal>(OnCurrencyChanged);
      _signalBus.Subscribe<SkinSelectedSignal>(OnSkinSelected);
      _signalBus.Subscribe<SkinPurchasedSignal>(OnSkinPurchased);

      if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
      if (_shopButton != null) _shopButton.onClick.AddListener(OnShopClicked);
      if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);

      // Текущее состояние сразу, без ожидания сигналов.
      RefreshBalance();
      BuildSkins();
    }

    private void OnDisable()
    {
      _signalBus.Unsubscribe<CurrencyChangedSignal>(OnCurrencyChanged);
      _signalBus.Unsubscribe<SkinSelectedSignal>(OnSkinSelected);
      _signalBus.Unsubscribe<SkinPurchasedSignal>(OnSkinPurchased);

      if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
      if (_shopButton != null) _shopButton.onClick.RemoveListener(OnShopClicked);
      if (_leaderboardButton != null) _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
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

    // --- Герои ---

    // Построить карточки из каталога (data-driven: список не хардкодим).
    private void BuildSkins()
    {
      // Снести прошлые.
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

    // Нажали карточку героя.
    private void OnSkinClicked(string skinId)
    {
      // Куплен → выбрать его (логика выбора в IShopService).
      if (_shop.IsSkinOwned(skinId))
      {
        _shop.SelectSkin(skinId);
        return;
      }

      // TODO: герой не куплен — разработчик решит, что делать (увести в магазин,
      // подсветить карточку и т.п.). Покупку здесь НЕ делаем — это магазин.
    }

    // Выбор скина изменился — обновить визуальные состояния карточек.
    private void OnSkinSelected(SkinSelectedSignal s) => RefreshSkinStates();

    // Скин куплен — обновить визуальные состояния карточек.
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

    // --- Кнопки ---

    // Играть: грузим сцену забега и переводим автомат (как существующий GameButton).
    private void OnPlayClicked()
    {
      _sceneLoader.Load("Game", () => _flow.SetState(GameState.Ready));
    }

    // Магазин: включаем панель магазина (если она отдельная сцена — заменить на Load).
    private void OnShopClicked()
    {
      if (_shopPanel != null) _shopPanel.SetActive(true);
    }

    // Лидерборд: у ILeaderboardService есть только отправка счёта, метода показа нет.
    // TODO: показать таблицу (UI лидерборда плагина / собственная панель) — по решению разработчика.
    private void OnLeaderboardClicked()
    {
      Debug.Log("[MenuView] Leaderboard button: показ таблицы пока не реализован (TODO).");
    }
  }
}
