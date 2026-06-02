using BurgerCatch.Events;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Input;
using Zenject;

namespace BurgerCatch.Gameplay.Chef
{
  /// <summary>
  /// Повар. По намерению ввода перемещается между сторонами.
  /// Логика "перейти vs ударить" живёт ЗДЕСЬ, не во вводе:
  /// - не на этой стороне -> перейти;
  /// - уже на этой стороне -> ударить (Day 9, пока заглушка).
  /// </summary>
  public sealed class ChefController : IInitializable, System.IDisposable
  {
    public Side CurrentSide { get; private set; } = Side.Left;

    private readonly IInputService _input;
    private readonly SignalBus _signalBus;

    public ChefController(IInputService input, SignalBus signalBus)
    {
      _input = input;
      _signalBus = signalBus;
    }

    public void Initialize() => _input.SideTapped += OnSideTapped;
    public void Dispose()    => _input.SideTapped -= OnSideTapped;

    private void OnSideTapped(Side side)
    {
      if (side != CurrentSide)
      {
        MoveTo(side);
      }
      else
      {
        Hit(); // Day 9: отбивание сковородой
      }
    }

    private void MoveTo(Side side)
    {
      CurrentSide = side;
      _signalBus.Fire(new ChefMovedSignal(side));
    }

    private void Hit()
    {
      // Day 9: запросить отбивание ингредиента на текущей стороне.
      // Пока пусто — глагол удара ещё не реализован.
    }
  }
}