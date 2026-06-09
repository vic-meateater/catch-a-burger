using System.Collections.Generic;
using System.Linq;
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
  /// ВРЕМЕННЫЕ ЛЕСА. Удалить после визуальной проверки V-геометрии.
  /// - спавнит ингредиенты по таймеру на случайную сторону;
  /// - рисует их кубиками вдоль наклонных лент (по Progress);
  /// - рисует повара кубиком на текущей стороне (у устья);
  /// - ПРОБЕЛ — пауза/резюм игрового времени;
  /// - стрелки/тап — перемещение повара (через настоящий ввод).
  /// </summary>
  public sealed class TestIngredientRun : MonoBehaviour
  {
    [SerializeField] private Vector3 _ingredientScale = new Vector3(0.6f, 0.6f, 0.6f);
    [SerializeField] private Vector3 _chefScale = new Vector3(0.9f, 0.9f, 0.9f);

    private IGameClock _clock;
    private ConveyorSystem _conveyor;
    private ConveyorGeometry _geometry;
    private ChefController _chef;

    [Inject]
    public void Construct(
      IGameClock clock,
      ConveyorSystem conveyor,
      ConveyorGeometry geometry,
      ChefController chef,
      SignalBus signalBus,
      OrderSystem order,
      ScoringSystem scoring)
    {
      _clock = clock;
      _conveyor = conveyor;
      _geometry = geometry;
      _chef = chef;
      _signalBus = signalBus;
      _order = order;
      _scoring = scoring;
    }

    private readonly Dictionary<Ingredient, Transform> _cubes
      = new Dictionary<Ingredient, Transform>();

    private Transform _chefCube;
    private SignalBus _signalBus;
    private OrderSystem _order;
    private Sprite _squareSprite;
    private ScoringSystem _scoring;

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
      
      _squareSprite = Sprite.Create(
        Texture2D.whiteTexture,
        new Rect(0, 0, 1, 1),
        new Vector2(0.5f, 0.5f), 1f);

      _signalBus.Subscribe<OrderChangedSignal>(s =>
        Debug.Log($"[Order] NEW ORDER. Need first: {s.Recipe[0]}"));
      _signalBus.Subscribe<OrderItemMatchedSignal>(s =>
        Debug.Log($"[Order] MATCHED {s.Type}. Next needed: {_order.Current}")); // если заинжектишь OrderSystem
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
      // Пауза/резюм по пробелу — главная проверка дня.
      if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
      {
        if (_clock.IsRunning) _clock.Pause();
        else _clock.Resume();
        Debug.Log($"[Test] Clock running = {_clock.IsRunning}");
      }

      SyncIngredients();
      SyncChef();
    }
    
    public static Color ColorOf(IngredientType type)
    {
      switch (type)
      {
        case IngredientType.TopBun:    return new Color(1f, 0.0f, 0.3f);  // булка — красная
        case IngredientType.BottomBun:    return new Color(1f, 0.0f, 0.9f);  // булка — красная
        case IngredientType.Patty:  return new Color(0.45f, 0.25f, 0.1f);  // котлета — коричневая
        case IngredientType.Cheese: return new Color(1f, 0.6f, 0.0f);      // сыр — оранжевый
        default:                    return Color.white;
      }
    }

    private void SyncIngredients()
    {
      // Создать кубик для новых ингредиентов.
      foreach (var ing in _conveyor.Active)
      {
        if (_cubes.ContainsKey(ing)) continue;
        
        var go = CreateQuad(ColorOf(ing.Type));
        go.transform.localScale = _ingredientScale;
        _cubes[ing] = go.transform;
      }

      // Обновить позиции вдоль наклонной ленты, убрать пропавшие.
      var toRemove = new List<Ingredient>();
      foreach (var kv in _cubes)
      {
        var ing = kv.Key;
        var tr = kv.Value;

        if (!_conveyor.Active.Contains(ing))
        {
          Destroy(tr.gameObject);
          toRemove.Add(ing);
          continue;
        }

        tr.position = _geometry.PositionOf(ing.Side, ing.Progress);
      }

      foreach (var ing in toRemove)
        _cubes.Remove(ing);
    }

    private void SyncChef()
    {
      if (_chefCube == null) return;
      // Повар стоит у устья своей текущей стороны.
      _chefCube.position = _geometry.CatchPoint(_chef.CurrentSide);
    }
    
    private Transform CreateQuad(Color color)
    {
      var go = new GameObject("__TEST_Quad");
      var sr = go.AddComponent<SpriteRenderer>();
      sr.sprite = _squareSprite;   // см. ниже, откуда взять
      sr.color = color;            // в 2D цвет работает сразу, без шейдер-плясок
      return go.transform;
    }
  }
}