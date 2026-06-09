namespace BurgerCatch.Gameplay.Conveyor
{
  public enum Side
  {
    Left,
    Right,
    None
  }

  // Первые типы ингредиентов. Расширяется по мере роста рецептов.
  public enum IngredientType
  {
    TopBun = 0,
    Patty = 1,
    Cheese = 2,
    BottomBun = 3,
  }

  public class Ingredient
  {
    public Side Side;
    public IngredientType Type;
    public float Progress;
  }
}
