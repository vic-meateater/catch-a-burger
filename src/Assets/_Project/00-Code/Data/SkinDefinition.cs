using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>Описание скина-героя. Data-driven: новый герой = новый ассет.</summary>
  [CreateAssetMenu(menuName = "BurgerCatch/Skin")]
  public sealed class SkinDefinition : ScriptableObject
  {
    [SerializeField] private string _id;            // напр. "roma", "nika"
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;          // для будущего UI
    [SerializeField] private int _price;            // в softCurrency; 0 — бесплатный/только инап
    [SerializeField] private bool _iapOnly;         // только за реальные деньги
    [SerializeField] private string _iapId;         // id покупки в Яндексе, если IapOnly

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public int Price => _price;
    public bool IapOnly => _iapOnly;
    public string IapId => _iapId;
  }
}
