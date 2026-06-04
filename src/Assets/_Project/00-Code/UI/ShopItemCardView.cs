using System;
using BurgerCatch.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Универсальная карточка товара-буста в магазине. ТОНКИЙ слой: показывает
  /// иконку/имя/цену и сообщает наружу о нажатии. Покупку выполняет ShopView
  /// через IShopService — карточка ничего не списывает.
  /// </summary>
  public sealed class ShopItemCardView : MonoBehaviour
  {
    [Tooltip("Кнопка карточки. Клик уходит наружу с типом буста.")]
    [SerializeField] private Button _button;

    [Tooltip("Иконка буста.")]
    [SerializeField] private Image _icon;

    [Tooltip("Имя буста.")]
    [SerializeField] private TMP_Text _nameText;

    [Tooltip("Цена в валюте.")]
    [SerializeField] private TMP_Text _priceText;

    // Тип буста этой карточки — пробрасываем родителю по клику.
    private BoostType _type;

    /// <summary>Тип буста этой карточки (для родителя).</summary>
    public BoostType Type => _type;

    /// <summary>
    /// Заполнить карточку данными буста и подписать клик на внешний обработчик.
    /// </summary>
    public void Bind(BoostDefinition def, Action<BoostType> onClick)
    {
      _type = def.Type;

      if (_icon != null) _icon.sprite = def.Icon;
      if (_nameText != null) _nameText.text = def.DisplayName;
      if (_priceText != null) _priceText.text = def.Price.ToString();

      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(() => onClick?.Invoke(_type));
    }
  }
}
