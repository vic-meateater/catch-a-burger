using BurgerCatch.Gameplay.Conveyor; // Side

namespace BurgerCatch.Events
{
  /// <summary>Повар ударил сковородой на стороне Side. Исполняет HitResolver.</summary>
  public sealed class ChefHitSignal
  {
    public Side Side { get; }
    public ChefHitSignal(Side side) => Side = side;
  }
}
