using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Gameplay.Burger
{
  public readonly struct BurgerLayer
  {
    public readonly IngredientType Type;
    public readonly bool IsDirty;

    public BurgerLayer(IngredientType type, bool isDirty)
    {
      Type = type;
      IsDirty = isDirty;
    }
  }
}