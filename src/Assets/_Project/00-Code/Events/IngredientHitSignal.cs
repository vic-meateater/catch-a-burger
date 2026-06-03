using BurgerCatch.Gameplay.Conveyor; // Side, IngredientType

namespace BurgerCatch.Events
{
  /// <summary>Факт: удар сковородой реально сбил ингредиент (для будущих эффектов).</summary>
  public sealed class IngredientHitSignal
  {
    public IngredientType Type { get; }
    public Side Side { get; }

    public IngredientHitSignal(IngredientType type, Side side)
    {
      Type = type;
      Side = side;
    }
  }
}
