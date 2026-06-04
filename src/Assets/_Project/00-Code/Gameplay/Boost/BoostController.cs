using System;
using BurgerCatch.Data;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Scoring;
using BurgerCatch.Gameplay.Spawn;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Boost
{
  /// <summary>
  /// Система бустов-расходников. Эффект + таймер на ИГРОВОМ времени
  /// (на паузе замирает). Каждый тип буста независим.
  /// </summary>
  public sealed class BoostController : IInitializable, IDisposable, ITickable
  {
    private const int TypeCount = 3; // SlowConveyors, DoublePrice, OnlyNeeded

    private readonly SignalBus _signalBus;
    private readonly IGameClock _clock;
    private readonly ConveyorSystem _conveyor;
    private readonly ScoringSystem _scoring;
    private readonly SpawnDirector _spawn;
    private readonly GameplayConfig _config;

    // Остаток времени по каждому типу (индекс = (int)BoostType). 0 = не активен.
    private readonly float[] _remaining = new float[TypeCount];

    // Скорость лент ДО SlowConveyors — чтобы вернуть именно её (скорость могла
    // вырасти по сложности), а не базовую.
    private float _savedSpeed;

    public BoostController(
      SignalBus signalBus,
      IGameClock clock,
      ConveyorSystem conveyor,
      ScoringSystem scoring,
      SpawnDirector spawn,
      GameplayConfig config)
    {
      _signalBus = signalBus;
      _clock = clock;
      _conveyor = conveyor;
      _scoring = scoring;
      _spawn = spawn;
      _config = config;
    }

    public void Initialize() { }

    public void Dispose() { }

    public void Activate(BoostType type)
    {
      bool wasActive = _remaining[(int)type] > 0f;

      // Эффект включаем только если буст ещё не активен — иначе повторная
      // активация лишь продлевает таймер (и не перезатирает _savedSpeed).
      if (!wasActive)
      {
        ApplyEffect(type);
        _signalBus.Fire(new BoostActivatedSignal(type));
      }

      _remaining[(int)type] = DurationOf(type);
    }

    public void Tick()
    {
      float dt = _clock.DeltaTime;
      if (dt <= 0f) return;

      for (int i = 0; i < TypeCount; i++)
      {
        if (_remaining[i] <= 0f) continue;

        _remaining[i] -= dt;
        if (_remaining[i] > 0f) continue;

        _remaining[i] = 0f;
        var type = (BoostType)i;
        RemoveEffect(type);
        _signalBus.Fire(new BoostExpiredSignal(type));
      }
    }

    private float DurationOf(BoostType type)
    {
      switch (type)
      {
        case BoostType.SlowConveyors: return _config.SlowDuration;
        case BoostType.DoublePrice:   return _config.DoublePriceDuration;
        case BoostType.OnlyNeeded:    return _config.OnlyNeededDuration;
        default: return 0f;
      }
    }

    private void ApplyEffect(BoostType type)
    {
      switch (type)
      {
        case BoostType.SlowConveyors:
          _savedSpeed = _conveyor.Speed;            // запомнить текущую (могла вырасти)
          _conveyor.Speed = _savedSpeed * _config.SlowFactor;
          break;
        case BoostType.DoublePrice:
          _scoring.PriceMultiplier = 2;
          break;
        case BoostType.OnlyNeeded:
          _spawn.OnlyNeededMode = true;
          break;
      }
    }

    private void RemoveEffect(BoostType type)
    {
      switch (type)
      {
        case BoostType.SlowConveyors:
          _conveyor.Speed = _savedSpeed;            // вернуть прежнюю, не базовую
          break;
        case BoostType.DoublePrice:
          _scoring.PriceMultiplier = 1;
          break;
        case BoostType.OnlyNeeded:
          _spawn.OnlyNeededMode = false;
          break;
      }
    }
  }
}
