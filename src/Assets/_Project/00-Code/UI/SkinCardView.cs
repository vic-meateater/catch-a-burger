using System;
using BurgerCatch.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Карточка скина-героя. ТОНКИЙ слой: только показывает данные и сообщает о
  /// нажатии наружу через колбэк. Никакой логики покупки/выбора внутри —
  /// решает родительское вью (MenuView/ShopView), дёргая сервисы.
  /// Используется и в меню (выбор героя), и в магазине (покупка).
  /// </summary>
  public sealed class SkinCardView : MonoBehaviour
  {
    [Tooltip("Кнопка всей карточки. Клик уходит наружу через колбэк из Bind().")]
    [SerializeField] private Button _button;

    [Tooltip("Иконка героя.")]
    [SerializeField] private Image _icon;

    [Tooltip("Имя героя.")]
    [SerializeField] private TMP_Text _nameText;

    [Tooltip("Цена в валюте. Можно скрыть для уже купленных.")]
    [SerializeField] private TMP_Text _priceText;

    [Tooltip("Маркер «выбран» (галочка/рамка). Включается, если этот скин активный.")]
    [SerializeField] private GameObject _selectedMarker;

    [Tooltip("Маркер «куплен» (без цены). Включается, если скин во владении.")]
    [SerializeField] private GameObject _ownedMarker;

    // Запоминаем id, чтобы родитель по колбэку знал, какая карточка нажата.
    private string _skinId;

    /// <summary>Id скина этой карточки (для родителя).</summary>
    public string SkinId => _skinId;

    /// <summary>
    /// Заполнить карточку данными скина и подписать клик на внешний обработчик.
    /// Родитель передаёт onClick — он и решает, выбрать/купить/увести в магазин.
    /// </summary>
    public void Bind(SkinDefinition def, Action<string> onClick)
    {
      _skinId = def.Id;

      if (_icon != null) _icon.sprite = def.Icon;
      if (_nameText != null) _nameText.text = def.DisplayName;
      if (_priceText != null) _priceText.text = def.Price.ToString();

      // Клик пробрасываем наружу с id этой карточки.
      _button.onClick.RemoveAllListeners();
      _button.onClick.AddListener(() => onClick?.Invoke(_skinId));
    }

    /// <summary>
    /// Обновить визуальное состояние карточки: куплен / выбран.
    /// Цену прячем у купленных (платить больше нечем).
    /// </summary>
    public void SetState(bool owned, bool selected)
    {
      if (_ownedMarker != null) _ownedMarker.SetActive(owned);
      if (_selectedMarker != null) _selectedMarker.SetActive(selected);

      // Купленному цена не нужна.
      if (_priceText != null) _priceText.gameObject.SetActive(!owned);
    }
  }
}
