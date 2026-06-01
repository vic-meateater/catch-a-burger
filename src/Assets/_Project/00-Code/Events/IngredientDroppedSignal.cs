using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Events
{
  /// <summary>
  /// Факт: ингредиент сорвался с устья ленты на пол (повара на стороне не было).
  /// Систему жизней слушает отдельно (LifeLost — её забота, не ленты).
  /// </summary>
  public sealed class IngredientDroppedSignal
  {
    public IngredientType Type { get; }
    public Side Side { get; }

    public IngredientDroppedSignal(IngredientType type, Side side)
    {
      Type = type;
      Side = side;
    }
  }
}
