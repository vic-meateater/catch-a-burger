using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Events
{
  /// <summary>Пойман НЕ тот (грязный слой). → ScoringSystem спишет цену (Day 13).</summary>

  public sealed class OrderItemWrongSignal
  {
    public IngredientType Type { get; }
    public OrderItemWrongSignal(IngredientType type) => Type = type;
  }
}