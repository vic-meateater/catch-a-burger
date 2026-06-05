using System.Collections.Generic;
using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Каталог ингредиентов: данные (имя/спрайт) по типу.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/IngredientCatalog")]
  public sealed class IngredientCatalog : ScriptableObject
  {
    [SerializeField] private List<IngredientDefinition> _ingredients = new List<IngredientDefinition>();

    public IReadOnlyList<IngredientDefinition> Ingredients => _ingredients;

    /// <summary>Найти описание по типу. null, если нет в каталоге.</summary>
    public IngredientDefinition GetByType(IngredientType type)
    {
      for (int i = 0; i < _ingredients.Count; i++)
        if (_ingredients[i] != null && _ingredients[i].Type == type)
          return _ingredients[i];
      return null;
    }
  }
}
