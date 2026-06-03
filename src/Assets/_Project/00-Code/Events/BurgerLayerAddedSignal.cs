using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Events
{
  public sealed class BurgerLayerAddedSignal
  {
    public IngredientType Type { get; }
    public bool IsDirty { get; }
    public int TotalLayers { get; }
    public BurgerLayerAddedSignal(IngredientType type, bool isDirty, int total)
    {
      Type = type; IsDirty = isDirty; TotalLayers = total;
    }
  }
}