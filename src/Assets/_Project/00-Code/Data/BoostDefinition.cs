using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Описание буста-товара. Data-driven.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/Boost")]
  public sealed class BoostDefinition : ScriptableObject
  {
    [SerializeField] private BoostType _type;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _price;            // в softCurrency

    public BoostType Type => _type;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public int Price => _price;
  }
}
