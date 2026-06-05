using BurgerCatch.Core.Flow;
using BurgerCatch.Core.Saves;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Оверлей конца забега: счёт, рекорд, «продолжить за рекламу», «в меню».
  /// ТОНКИЙ слой: кнопки лишь стреляют сигнал/зовут загрузчик сцен — логика
  /// продолжения живёт в ContinueController (Фаза 3).
  /// </summary>
  public sealed class GameOverView : MonoBehaviour
  {
    [Tooltip("Корневая панель оверлея — включаем на game over, прячем иначе.")]
    [SerializeField] private GameObject _root;

    [Tooltip("Текст счёта этого забега.")]
    [SerializeField] private TMP_Text _scoreText;

    [Tooltip("Текст рекорда.")]
    [SerializeField] private TMP_Text _bestText;

    [Tooltip("Кнопка «продолжить за рекламу».")]
    [SerializeField] private Button _continueButton;

    [Tooltip("Кнопка «в меню».")]
    [SerializeField] private Button _menuButton;

    private SignalBus _signalBus;
    private ScoringSystem _scoring;
    private ISaveService _saveService;
    private ISceneLoader _sceneLoader;
    private GameFlowController _flow;

    [Inject]
    public void Construct(
      SignalBus signalBus,
      ScoringSystem scoring,
      ISaveService saveService,
      ISceneLoader sceneLoader,
      GameFlowController flow)
    {
      _signalBus = signalBus;
      _scoring = scoring;
      _saveService = saveService;
      _sceneLoader = sceneLoader;
      _flow = flow;
    }

    private void OnEnable()
    {
      // По умолчанию оверлей скрыт.
      if (_root != null) _root.SetActive(false);

      _signalBus.Subscribe<GameOverTriggeredSignal>(OnGameOver);
      _signalBus.Subscribe<RunResumedSignal>(OnRunResumed);

      if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);
      if (_menuButton != null) _menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDisable()
    {
      // Guard: объект мог отключиться до инъекции зависимостей.
      if (_signalBus == null) return;

      _signalBus.Unsubscribe<GameOverTriggeredSignal>(OnGameOver);
      _signalBus.Unsubscribe<RunResumedSignal>(OnRunResumed);

      if (_continueButton != null) _continueButton.onClick.RemoveListener(OnContinueClicked);
      if (_menuButton != null) _menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    // Забег окончен — показать панель и заполнить цифры из сервисов.
    private void OnGameOver(GameOverTriggeredSignal s)
    {
      if (_root != null) _root.SetActive(true);

      if (_scoreText != null) _scoreText.text = _scoring.RunScore.ToString();
      if (_bestText != null) _bestText.text = _saveService.Data.BestScore.ToString();
    }

    // Кнопка continue только ПРОСИТ продолжение — выдаёт его ContinueController.
    private void OnContinueClicked()
    {
      _signalBus.Fire(new ContinueRequestedSignal());
    }

    // Продолжение состоялось (просмотрели рекламу) — прячем оверлей.
    private void OnRunResumed(RunResumedSignal s)
    {
      if (_root != null) _root.SetActive(false);
    }

    // В меню: грузим сцену меню и переводим автомат состояний (как MenuButton).
    private void OnMenuClicked()
    {
      _sceneLoader.Load("MainMenu", () => _flow.SetState(GameState.Menu));
    }
  }
}
