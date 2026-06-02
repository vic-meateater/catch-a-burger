using BurgerCatch.Gameplay.Conveyor; // IngredientType, Side

namespace BurgerCatch.Events
{
  /// <summary>
  /// Ингредиент доехал до устья ленты. Нейтральный факт — БЕЗ суждения
  /// "поймано/упало". Исход решает CatchResolver (он знает повара).
  /// Лента про повара не знает и стреляет только этот факт.
  /// </summary>
  public sealed class IngredientReachedMouthSignal
  {
    public IngredientType Type { get; }
    public Side Side { get; }

    public IngredientReachedMouthSignal(IngredientType type, Side side)
    {
      Type = type;
      Side = side;
    }
  }
}