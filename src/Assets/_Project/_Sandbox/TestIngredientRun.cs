using BurgerCatch.Events;
using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Order;
using BurgerCatch.Gameplay.Scoring;
using BurgerCatch.Gameplay.Time;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace _Project._Sandbox
{
  /// <summary>
  /// ВРЕМЕННЫЕ ЛЕСА (рабочий минимум). Отрисовка ингредиентов переехала в
  /// IngredientView/BurgerStackView — здесь осталось только:
  /// - квадрат-повар на текущей стороне (его скин — отдельная будущая задача);
  /// - ПРОБЕЛ — пауза/резюм игрового времени;
  /// - запуск часов (_clock.Resume) на старте;
  /// - отладочные логи сигналов.
  /// </summary>
  public sealed class TestIngredientRun : MonoBehaviour
  {
    [SerializeField] private Vector3 _chefScale = new Vector3(0.9f, 0.9f, 0.9f);

    private IGameClock _clock;
    private ConveyorGeometry _geometry;
    private ChefController _chef;
    private SignalBus _signalBus;
    private OrderSystem _order;
    private ScoringSystem _scoring;

    private Transform _chefCube;

    [Inject]
    public void Construct(
      IGameClock clock,
      ConveyorGeometry geometry,
      ChefController chef,
      SignalBus signalBus,
      OrderSystem order,
      ScoringSystem scoring)
    {
      _clock = clock;
      _geometry = geometry;
      _chef = chef;
      _signalBus = signalBus;
      _order = order;
      _scoring = scoring;
    }

    private void Start()
    {
      // Игровое время по умолчанию заморожено — запускаем, иначе ничего не поедет.
      _clock.Resume();

      // Кубик-повар (красный, чтобы отличать от ингредиентов).
      var chefGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
      chefGo.name = "__TEST_Chef";
      chefGo.transform.localScale = _chefScale;
      var rend = chefGo.GetComponent<Renderer>();
      if (rend != null) rend.material.color = Color.red;
      _chefCube = chefGo.transform;

      _signalBus.Subscribe<OrderChangedSignal>(s =>
        Debug.Log($"[Order] NEW ORDER. Need first: {s.Recipe[0]}"));
      _signalBus.Subscribe<OrderItemMatchedSignal>(s =>
        Debug.Log($"[Order] MATCHED {s.Type}. Next needed: {_order.Current}"));
      _signalBus.Subscribe<OrderItemWrongSignal>(s =>
        Debug.Log($"[Order] WRONG {s.Type} (dirty layer)"));
      _signalBus.Subscribe<OrderCompletedSignal>(_ =>
        Debug.Log("[Order] BURGER COMPLETED!"));
      _signalBus.Subscribe<BurgerLayerAddedSignal>(s =>
        Debug.Log($"[Stack] +layer {s.Type} dirty={s.IsDirty}, total={s.TotalLayers}"));
      _signalBus.Subscribe<BurgerSpoiledSignal>(_ =>
        Debug.Log("[Scoring] !!! BURGER SPOILED (price hit 0) !!!"));
      _signalBus.Subscribe<OrderCompletedSignal>(_ =>
        Debug.Log($"[Scoring] sold. RunScore now: {_scoring.RunScore}"));
      _signalBus.Subscribe<BurgerStackClearedSignal>(_ =>
        Debug.Log("[Stack] CLEARED"));
      _signalBus.Subscribe<IngredientHitSignal>(s =>
        Debug.Log($"[Hit] IngredientHit {s.Type} @ {s.Side}"));
    }

    private void Update()
    {
      // Пауза/резюм по пробелу.
      if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
      {
        if (_clock.IsRunning) _clock.Pause();
        else _clock.Resume();
        Debug.Log($"[Test] Clock running = {_clock.IsRunning}");
      }

      SyncChef();
    }

    private void SyncChef()
    {
      if (_chefCube == null) return;
      // Повар стоит у устья своей текущей стороны.
      _chefCube.position = _geometry.CatchPoint(_chef.CurrentSide);
    }
  }
}
