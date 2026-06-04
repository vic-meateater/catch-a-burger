using System.Collections.Generic;
using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Каталог бустов-товаров.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/BoostCatalog")]
  public sealed class BoostCatalog : ScriptableObject
  {
    [SerializeField] private List<BoostDefinition> _boosts = new List<BoostDefinition>();

    public IReadOnlyList<BoostDefinition> Boosts => _boosts;

    /// <summary>Найти буст по типу. null, если нет в каталоге.</summary>
    public BoostDefinition GetByType(BoostType type)
    {
      for (int i = 0; i < _boosts.Count; i++)
        if (_boosts[i] != null && _boosts[i].Type == type)
          return _boosts[i];
      return null;
    }
  }
}
