using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Events
{
  /// <summary>Пойман нужный по заказу ингредиент (чистый слой).</summary>
  public sealed class OrderItemMatchedSignal
  {
    public IngredientType Type { get; }
    public OrderItemMatchedSignal(IngredientType type) => Type = type;
  }
}