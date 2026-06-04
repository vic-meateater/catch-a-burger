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
    // --- База (потом в GameplayConfig) ---
    private const float BaseInterval = 1.2f;
    private const float BaseSpeed = 0.2f;

    // --- Рост за бургер ---
    private const float SpeedPerBurger = 0.015f;
    private const float IntervalCutPerBurger = 0.03f;

    // --- Потолки честной сложности (обязательны!) ---
    private const float MaxSpeed = 0.6f;
    private const float MinInterval = 0.5f;

    // --- Логика спавна ---
    private const int ForceNeededAfter = 3;
    private const int MaxTotalThreats = 6;
    private const int MaxThreatsOnFarSide = 1;
    private const float NeededChance = 0.3f;

    private readonly IGameClock _clock;
    private readonly ConveyorSystem _conveyor;
    private readonly OrderSystem _order;
    private readonly ChefController _chef;
    private readonly SignalBus _signalBus;

    private float _timer;
    private float _currentInterval = BaseInterval;
    private int _spawnsSinceNeeded;
    private int _burgersCompleted;

    public SpawnDirector(
      IGameClock clock,
      ConveyorSystem conveyor,
      OrderSystem order,
      ChefController chef,
      SignalBus signalBus)
    {
      _clock = clock;
      _conveyor = conveyor;
      _order = order;
      _chef = chef;
      _signalBus = signalBus;
    }

    public void Initialize()
    {
      _conveyor.Speed = BaseSpeed;
      _currentInterval = BaseInterval;
      _signalBus.Subscribe<OrderCompletedSignal>(OnBurgerCompleted);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<OrderCompletedSignal>(OnBurgerCompleted);
    }

    public void Tick()
    {
      float dt = _clock.DeltaTime;
      if (dt <= 0f) return;

      _timer += dt;
      if (_timer < _currentInterval) return;

      _timer -= _currentInterval;
      TrySpawnOne();
    }

    // --- Рост сложности: ТОЛЬКО на собранном бургере ---
    private void OnBurgerCompleted(OrderCompletedSignal s)
    {
      _burgersCompleted++;

      float speed = BaseSpeed + SpeedPerBurger * _burgersCompleted;
      _conveyor.Speed = UnityEngine.Mathf.Min(speed, MaxSpeed);

      _currentInterval = UnityEngine.Mathf.Max(
        BaseInterval - IntervalCutPerBurger * _burgersCompleted,
        MinInterval);
    }

    // --- Спавн ---
    private void TrySpawnOne()
    {
      IngredientType needed = _order.Current;
      IngredientType type = ChooseType(needed, out bool wasForced);

      if (!wasForced && _conveyor.Active.Count >= MaxTotalThreats)
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
      if (_spawnsSinceNeeded >= ForceNeededAfter)
      {
        wasForced = true;
        return needed;
      }

      wasForced = false;

      if (UnityEngine.Random.value < NeededChance)
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

      if (CountThreatsOn(far) >= MaxThreatsOnFarSide)
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