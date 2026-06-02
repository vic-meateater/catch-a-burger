using System.Collections.Generic;
using System.Linq;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Time;
using BurgerCatch.Helpers;
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
    [SerializeField] private float _spawnEvery = 1.2f;
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
      SignalBus signalBus)
    {
      _clock = clock;
      _conveyor = conveyor;
      _geometry = geometry;
      _chef = chef;
      _signalBus = signalBus;
    }

    private readonly Dictionary<Ingredient, Transform> _cubes
      = new Dictionary<Ingredient, Transform>();

    private Transform _chefCube;
    private float _spawnTimer;
    private SignalBus _signalBus;

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
      
      _signalBus.Subscribe<IngredientCaughtSignal>(s =>
        Debug.Log($"[Test] CAUGHT {s.Type} on {s.Side}"));
      _signalBus.Subscribe<IngredientDroppedSignal>(s =>
        Debug.Log($"[Test] DROPPED {s.Type} on {s.Side}"));
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

      // Спавн по реальному времени (это леса), только когда время идёт.
      if (_clock.IsRunning)
      {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnEvery)
        {
          _spawnTimer = 0f;
          var side = Random.value < 0.5f ? Side.Left : Side.Right;
          var type = (IngredientType)Random.Range(0, 3);
          _conveyor.Spawn(side, type);
        }
      }

      SyncIngredients();
      SyncChef();
    }

    private void SyncIngredients()
    {
      // Создать кубик для новых ингредиентов.
      foreach (var ing in _conveyor.Active)
      {
        if (_cubes.ContainsKey(ing)) continue;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "__TEST_Ingredient";
        go.transform.localScale = _ingredientScale;
        
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
          rend.material.color = Helpers.ColorOf(ing.Type);
        
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
  }
}