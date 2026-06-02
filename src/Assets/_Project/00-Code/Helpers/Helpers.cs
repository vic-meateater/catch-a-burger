using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;

namespace BurgerCatch.Helpers
{
  public static class Helpers
  {
    public static Color ColorOf(IngredientType type)
    {
      switch (type)
      {
        case IngredientType.Bun:    return new Color(0.95f, 0.75f, 0.3f);  // булка — жёлтая
        case IngredientType.Patty:  return new Color(0.45f, 0.25f, 0.1f);  // котлета — коричневая
        case IngredientType.Cheese: return new Color(1f, 0.6f, 0.0f);      // сыр — оранжевый
        default:                    return Color.white;
      }
    }
  }
}