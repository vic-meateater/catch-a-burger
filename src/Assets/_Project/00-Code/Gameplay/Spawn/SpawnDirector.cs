using BurgerCatch.Data;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Order;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Spawn
{
  /// <summary>
  /// Директор спавна. Решает КОГДА/ЧТО/КУДА, держит честность (предохранители)
  /// и растит сложность по числу СОБРАННЫХ бургеров (не по времени — чтобы
  /// не душить буксующего игрока; протухание сложность НЕ растит).
  /// Черновая кривая: точная модель честности по времени полёта — позже,
  /// после фиксации геометрии/скорости.
  /// </summary>
  public sealed class SpawnDirector : IInitializable, System.IDisposable, ITickable
  {
    private readonly GameplayConfig _config;
    private readonly IGameClock _clock;
    private readonly ConveyorSystem _conveyor;
    private readonly OrderSystem _order;
    private readonly ChefController _chef;
    private readonly SignalBus _signalBus;

    private float _timer;
    private float _currentInterval;
    private int _spawnsSinceNeeded;
    private int _burgersCompleted;

    // --- Чистое окно при смене заказа: пока > 0, спавн заморожен ---
    private float _orderChangeTimer;

    /// <summary>Буст OnlyNeeded: пока true — спавнится только нужный по заказу (без мусора).</summary>
    public bool OnlyNeededMode { get; set; }

    public SpawnDirector(
      GameplayConfig config,
      IGameClock clock,
      ConveyorSystem conveyor,
      OrderSystem order,
      ChefController chef,
      SignalBus signalBus)
    {
      _config = config;
      _clock = clock;
      _conveyor = conveyor;
      _order = order;
      _chef = chef;
      _signalBus = signalBus;
    }

    public void Initialize()
    {
      _conveyor.Speed = _config.BaseSpeed;
      _currentInterval = _config.BaseInterval;
      _signalBus.Subscribe<OrderCompletedSignal>(OnBurgerCompleted);
      _signalBus.Subscribe<OrderChangedSignal>(OnOrderChanged);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<OrderCompletedSignal>(OnBurgerCompleted);
      _signalBus.Unsubscribe<OrderChangedSignal>(OnOrderChanged);
    }

    public void Tick()
    {
      float dt = _clock.DeltaTime;
      if (dt <= 0f) return;

      // Чистое окно: лента доигрывает старое, новый спавн заморожен.
      if (_orderChangeTimer > 0f)
      {
        _orderChangeTimer -= dt;
        return;
      }

      _timer += dt;
      if (_timer < _currentInterval) return;

      _timer -= _currentInterval;
      TrySpawnOne();
    }

    // --- Смена заказа: даём передышку на чтение нового рецепта ---
    private void OnOrderChanged(OrderChangedSignal s)
    {
      _orderChangeTimer = _config.OrderChangeWindow;
    }

    // --- Рост сложности: ТОЛЬКО на собранном бургере ---
    private void OnBurgerCompleted(OrderCompletedSignal s)
    {
      _burgersCompleted++;

      float speed = _config.BaseSpeed + _config.SpeedPerBurger * _burgersCompleted;
      _conveyor.Speed = UnityEngine.Mathf.Min(speed, _config.MaxSpeed);

      _currentInterval = UnityEngine.Mathf.Max(
        _config.BaseInterval - _config.IntervalCutPerBurger * _burgersCompleted,
        _config.MinInterval);
    }

    // --- Спавн ---
    private void TrySpawnOne()
    {
      IngredientType needed = _order.Current;
      IngredientType type = ChooseType(needed, out bool wasForced);

      if (!wasForced && _conveyor.Active.Count >= _config.MaxTotalThreats)
        return;

      Side side = ChooseSide();
      if (side == Side.None) return;

      _conveyor.Spawn(side, type);

      if (type == needed)
        _spawnsSinceNeeded = 0;
      else
        _spawnsSinceNeeded++;
    }

    private IngredientType ChooseType(IngredientType needed, out bool wasForced)
    {
      if (_spawnsSinceNeeded >= _config.ForceNeededAfter)
      {
        wasForced = true;
        return needed;
      }

      wasForced = false;

      // Буст OnlyNeeded: мусор не спавним, всегда нужный.
      if (OnlyNeededMode)
        return needed;

      if (UnityEngine.Random.value < _config.NeededChance)
        return needed;

      return ChooseJunk(needed);
    }

    private IngredientType ChooseJunk(IngredientType needed)
    {
      IngredientType junk;
      do
      {
        junk = (IngredientType)UnityEngine.Random.Range(0, 3);
      } while (junk == needed);

      return junk;
    }

    private Side ChooseSide()
    {
      Side near = _chef.CurrentSide;
      Side far = near == Side.Left ? Side.Right : Side.Left;

      if (CountThreatsOn(far) >= _config.MaxThreatsOnFarSide)
        return near;

      return UnityEngine.Random.value < 0.5f ? Side.Left : Side.Right;
    }

    private int CountThreatsOn(Side side)
    {
      int count = 0;
      var active = _conveyor.Active;
      for (int i = 0; i < active.Count; i++)
        if (active[i].Side == side)
          count++;
      return count;
    }
  }
}