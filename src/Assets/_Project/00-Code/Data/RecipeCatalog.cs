using System.Collections.Generic;
using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Каталог рецептов: OrderSystem берёт случайный на каждый новый заказ.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/RecipeCatalog")]
  public sealed class RecipeCatalog : ScriptableObject
  {
    [SerializeField] private List<RecipeDefinition> _recipes = new List<RecipeDefinition>();

    public IReadOnlyList<RecipeDefinition> Recipes => _recipes;

    /// <summary>Случайный рецепт. null, если список пуст (OrderSystem обработает).</summary>
    public RecipeDefinition GetRandom()
    {
      if (_recipes == null || _recipes.Count == 0) return null;
      int i = Random.Range(0, _recipes.Count);
      return _recipes[i];
    }
  }
}
