using System.Collections.Generic;
using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Каталог всех скинов. Системы читают его, списки не хардкодят.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/SkinCatalog")]
  public sealed class SkinCatalog : ScriptableObject
  {
    [SerializeField] private List<SkinDefinition> _skins = new List<SkinDefinition>();

    public IReadOnlyList<SkinDefinition> Skins => _skins;

    /// <summary>Найти скин по Id. null, если нет в каталоге.</summary>
    public SkinDefinition GetById(string id)
    {
      for (int i = 0; i < _skins.Count; i++)
        if (_skins[i] != null && _skins[i].Id == id)
          return _skins[i];
      return null;
    }
  }
}
