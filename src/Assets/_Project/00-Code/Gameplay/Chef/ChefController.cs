using System;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Input;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Chef
{
  /// <summary>
  /// Повар. По намерению ввода перемещается между сторонами.
  /// Логика "перейти vs ударить" живёт ЗДЕСЬ, не во вводе:
  /// - не на этой стороне -> перейти;
  /// - уже на этой стороне -> ударить (Day 9, пока заглушка).
  /// </summary>
  public sealed class ChefController : IInitializable, ITickable, IDisposable
  {
    public Side CurrentSide { get; private set; } = Side.Left;

    private readonly IInputService _input;
    private readonly SignalBus _signalBus;
    private readonly IGameClock _clock;
    private float _hitCooldownLeft;
    private const float HitCooldown = 0.3f;

    public ChefController(IInputService input, SignalBus signalBus, IGameClock clock)
    {
      _input = input;
      _signalBus = signalBus;
      _clock = clock;
    }

    public void Initialize() => _input.SideTapped += OnSideTapped;
    public void Dispose()    => _input.SideTapped -= OnSideTapped;
    
    public void Tick()
    {
      if (_hitCooldownLeft > 0f)
        _hitCooldownLeft -= _clock.DeltaTime;
    }
    
    private void OnSideTapped(Side side)
    {
      if (side != CurrentSide)
      {
        MoveTo(side);
      }
      else
      {
        Hit();
      }
    }

    private void MoveTo(Side side)
    {
      CurrentSide = side;
      _signalBus.Fire(new ChefMovedSignal(side));
    }

    private void Hit()
    {
      if (_hitCooldownLeft > 0f) return;        // ещё на перезарядке
      _hitCooldownLeft = HitCooldown;
      _signalBus.Fire(new ChefHitSignal(CurrentSide)); // исполнит HitResolver
    }

  }
}