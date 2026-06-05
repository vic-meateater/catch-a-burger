using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>
  /// Вешает на тип ингредиента визуал/имя (для HUD/арта). Вариант А: главный
  /// идентификатор — enum IngredientType, этот SO лишь добавляет данные к типу.
  /// </summary>
  [CreateAssetMenu(menuName = "BurgerCatch/Ingredient")]
  public sealed class IngredientDefinition : ScriptableObject
  {
    [SerializeField] private IngredientType _type;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;          // для HUD/арта; может быть пустым пока

    public IngredientType Type => _type;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
  }
}
