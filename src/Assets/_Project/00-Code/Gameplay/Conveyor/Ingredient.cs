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
    Bun = 0,
    Patty = 1,
    Cheese = 2,
  }

  public class Ingredient
  {
    public Side Side;
    public IngredientType Type;
    public float Progress;
  }
}
