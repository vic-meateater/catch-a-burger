using System.Collections.Generic;
using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Один рецепт = один бургер: последовательность ингредиентов по порядку.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/Recipe")]
  public sealed class RecipeDefinition : ScriptableObject
  {
    [SerializeField] private string _displayName;   // напр. "Чизбургер" — для отладки/UI
    [SerializeField] private List<IngredientType> _sequence = new List<IngredientType>();

    public string DisplayName => _displayName;
    public IReadOnlyList<IngredientType> Sequence => _sequence;

    /// <summary>Рецепт массивом (OrderSystem хранит текущий рецепт как массив).</summary>
    public IngredientType[] ToArray() => _sequence.ToArray();
  }
}
