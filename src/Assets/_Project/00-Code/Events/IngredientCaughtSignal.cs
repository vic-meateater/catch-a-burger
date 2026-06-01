using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Events
{
  /// <summary>Факт: повар поймал доехавший ингредиент.</summary>
  public sealed class IngredientCaughtSignal
  {
    public IngredientType Type { get; }
    public Side Side { get; }

    public IngredientCaughtSignal(IngredientType type, Side side)
    {
      Type = type;
      Side = side;
    }
  }
}
