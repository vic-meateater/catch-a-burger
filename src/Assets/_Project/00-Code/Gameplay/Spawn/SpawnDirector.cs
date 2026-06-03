using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Order;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Spawn
{
  /// <summary>
  /// Шаг 1 + предохранитель 1: директор видит доску (позиция повара + что на лентах)
  /// и не заваливает ДАЛЬНЮЮ сторону (где повара нет), чтобы не создавать
  /// нечестную вилку "не успел добежать". Читает состояние напрямую (pull) —
  /// директор это наблюдатель доски, а не реагирующий на события.
  /// </summary>
  public sealed class SpawnDirector : ITickable
  {
    private const float SpawnInterval = 1.2f; // интервал спавна
    private const float ForceNeededAfter = .3f; // форс нужного
    private const int MaxTotalThreats = 6; // потолок плотности на всём поле

    // Предохранитель 1: сколько угроз терпим на ДАЛЬНЕЙ стороне (где повара нет).
    private const int MaxThreatsOnFarSide = 1;

    private readonly IGameClock _clock;
    private readonly ConveyorSystem _conveyor;
    private readonly OrderSystem _order;
    private readonly ChefController _chef; // ← новые "глаза": где повар

    private float _timer;
    private int _spawnsSinceNeeded;

    public SpawnDirector(
      IGameClock clock,
      ConveyorSystem conveyor,
      OrderSystem order,
      ChefController chef)
    {
      _clock = clock;
      _conveyor = conveyor;
      _order = order;
      _chef = chef;
    }

    public void Tick()
    {
      float dt = _clock.DeltaTime;
      if (dt <= 0f) return;

      _timer += dt;
      if (_timer < SpawnInterval) return;

      _timer -= SpawnInterval;
      TrySpawnOne();
    }

    private void TrySpawnOne()
    {
      IngredientType needed = _order.Current;
      IngredientType type = ChooseType(needed, out bool wasForced);

      // Общий лимит пробивает только форсированный нужный.
      if (!wasForced && _conveyor.Active.Count >= MaxTotalThreats)
        return;

      Side side = ChooseSide();
      if (side == Side.None) return;

      // Спавн подтверждён — ТЕПЕРЬ фиксируем состояние.
      _conveyor.Spawn(side, type);

      // Счётчик "сколько подряд НЕ было нужного":
      // заспавнили нужный -> сброс; мусор -> +1.
      if (type == needed)
        _spawnsSinceNeeded = 0;
      else
        _spawnsSinceNeeded++;
    }

    /// <summary>
    /// Выбор стороны с предохранителем: не заваливаем дальнюю сторону.
    /// Ближняя (где повар) — спавнить можно свободно: игрок уже там.
    /// Дальняя — только если на ней мало угроз (игрок успеет добежать).
    /// </summary>
    private Side ChooseSide()
    {
      Side near = _chef.CurrentSide;
      Side far = near == Side.Left ? Side.Right : Side.Left;

      int threatsFar = CountThreatsOn(far);

      // Дальняя перегружена -> спавним только на ближнюю.
      if (threatsFar >= MaxThreatsOnFarSide)
        return near;

      // Иначе можно на любую — случайно, но это уже честно
      // (на дальней ещё есть запас по угрозам).
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

    private IngredientType ChooseType(IngredientType needed, out bool wasForced)
    {
      // Форс нужного, если давно не было.
      if (_spawnsSinceNeeded >= ForceNeededAfter)
      {
        wasForced = true;
        return needed;
      }

      wasForced = false;

      // 50/50: нужный или мусор.
      if (UnityEngine.Random.value < 0.5f)
        return needed;

      return ChooseJunk(needed);
    }

    private IngredientType ChooseJunk(IngredientType needed)
    {
      IngredientType junk;
      do
      {
        junk = (IngredientType) UnityEngine.Random.Range(0, 3);
      } while (junk == needed);

      return junk;
    }
  }
}