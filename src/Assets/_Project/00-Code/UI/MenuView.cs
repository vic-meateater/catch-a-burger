using BurgerCatch.Core.Economy;
using BurgerCatch.Core.Flow;
using BurgerCatch.Core.Saves;
using BurgerCatch.Data;
using BurgerCatch.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Главный экран меню: крупно ТЕКУЩИЙ выбранный герой, баланс и кнопки
  /// [Играть] [Сменить героя] [Магазин] [Лидерборд].
  /// ТОНКИЙ слой: только показывает (из сервисов/сигналов) и переключает панели/сцены.
  /// Выбор героя живёт в отдельной панели SkinSelectView, покупка — в ShopView.
  /// </summary>
  public sealed class MenuView : MonoBehaviour
  {
    [Tooltip("Текст баланса мягкой валюты.")]
    [SerializeField] private TMP_Text _balanceText;

    [Header("Текущий герой")]
    [Tooltip("Крупный спрайт выбранного героя.")]
    [SerializeField] private Image _currentHeroIcon;

    [Tooltip("Имя выбранного героя.")]
    [SerializeField] private TMP_Text _currentHeroName;

    [Header("Кнопки")]
    [Tooltip("Играть — грузит сцену забега.")]
    [SerializeField] private Button _playButton;

    [Tooltip("Сменить героя — открывает панель выбора.")]
    [SerializeField] private Button _selectHeroButton;

    [Tooltip("Магазин — открывает панель магазина.")]
    [SerializeField] private Button _shopButton;

    [Tooltip("Лидерборд.")]
    [SerializeField] private Button _leaderboardButton;

    [Header("Панели")]
    [Tooltip("Панель выбора героя (включаем по кнопке «Сменить героя»).")]
    [SerializeField] private GameObject _skinSelectPanel;

    [Tooltip("Панель магазина (включаем по кнопке «Магазин»).")]
    [SerializeField] private GameObject _shopPanel;

    private SignalBus _signalBus;
    private ICurrencyService _currency;
    private SkinCatalog _skinCatalog;
    private ISaveService _saveService;
    private ISceneLoader _sceneLoader;
    private GameFlowController _flow;

    [Inject]
    public void Construct(
      SignalBus signalBus,
      ICurrencyService currency,
      SkinCatalog skinCatalog,
      ISaveService saveService,
      ISceneLoader sceneLoader,
      GameFlowController flow)
    {
      _signalBus = signalBus;
      _currency = currency;
      _skinCatalog = skinCatalog;
      _saveService = saveService;
      _sceneLoader = sceneLoader;
      _flow = flow;
    }

    private void OnEnable()
    {
      _signalBus.Subscribe<CurrencyChangedSignal>(OnCurrencyChanged);
      _signalBus.Subscribe<SkinSelectedSignal>(OnSkinSelected);

      if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
      if (_selectHeroButton != null) _selectHeroButton.onClick.AddListener(OnSelectHeroClicked);
      if (_shopButton != null) _shopButton.onClick.AddListener(OnShopClicked);
      if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);

      // Текущее состояние сразу, без ожидания сигналов.
      RefreshBalance();
      RefreshCurrentHero();
    }

    private void OnDisable()
    {
      // Guard: панель могла отключиться до инъекции зависимостей.
      if (_signalBus == null) return;

      _signalBus.Unsubscribe<CurrencyChangedSignal>(OnCurrencyChanged);
      _signalBus.Unsubscribe<SkinSelectedSignal>(OnSkinSelected);

      if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
      if (_selectHeroButton != null) _selectHeroButton.onClick.RemoveListener(OnSelectHeroClicked);
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

    // --- Текущий герой ---

    // Игрок сменил героя в панели выбора — обновить главный экран.
    private void OnSkinSelected(SkinSelectedSignal s) => RefreshCurrentHero();

    // Показать спрайт+имя выбранного героя. Id — из сейва, данные — из каталога.
    private void RefreshCurrentHero()
    {
      if (_skinCatalog == null || _saveService == null) return;

      string id = _saveService.Data.SelectedSkin;
      var def = _skinCatalog.GetById(id);
      if (def == null) return;

      if (_currentHeroIcon != null) _currentHeroIcon.sprite = def.Icon;
      if (_currentHeroName != null) _currentHeroName.text = def.DisplayName;
    }

    // --- Кнопки ---

    // Играть: грузим сцену забега и переводим автомат (как существующий GameButton).
    // TODO: имя сцены забега — "Game" (проверено по GameButton.cs); свериться при переименовании.
    private void OnPlayClicked()
    {
      _sceneLoader.Load("Game", () => _flow.SetState(GameState.Ready));
    }

    // Сменить героя: включаем панель выбора (SkinSelectView построит купленных).
    private void OnSelectHeroClicked()
    {
      if (_skinSelectPanel != null) _skinSelectPanel.SetActive(true);
    }

    // Магазин: включаем панель магазина (ShopView).
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
