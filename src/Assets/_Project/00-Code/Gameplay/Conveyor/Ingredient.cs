namespace BurgerCatch.Gameplay.Conveyor
{
  public enum Side
  {
    Left,
    Right
  }

  // Первые типы ингредиентов. Расширяется по мере роста рецептов.
  public enum IngredientType
  {
    Default = 0,
    Bun = 1,
    Patty = 2,
    Cheese = 3,
  }

  public class Ingredient
  {
    public Side Side;
    public IngredientType Type;
    public float Progress;
  }
}
