using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Events
{
  /// <summary>Выдан новый заказ. UI покажет тикет, директор даст передышку (Фаза 2).</summary>
  public sealed class OrderChangedSignal
  {
    public IngredientType[] Recipe { get; }
    public OrderChangedSignal(IngredientType[] recipe) => Recipe = recipe;
  }
}