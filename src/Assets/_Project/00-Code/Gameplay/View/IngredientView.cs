using System.Collections.Generic;
using BurgerCatch.Data;
using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Gameplay.View
{
  /// <summary>
  /// Спрайтовая отрисовка едущих ингредиентов (чистовая замена лесов).
  /// ТОНКИЙ слой: только отражает ConveyorSystem.Active — появился ингредиент
  /// в логике → создаём спрайт, исчез (пойман/упал/отбит) → уничтожаем.
  /// Движением НЕ управляет, позицию читает из геометрии по Progress.
  /// </summary>
  public sealed class IngredientView : MonoBehaviour
  {
    [Tooltip("Префаб спрайта ингредиента (GameObject со SpriteRenderer). Картинку подставим из каталога.")]
    [SerializeField] private SpriteRenderer _ingredientPrefab;

    [Tooltip("Размер спрайта ингредиента (разработчик подгонит под арт).")]
    [SerializeField] private Vector3 _scale = Vector3.one;

    private ConveyorSystem _conveyor;
    private ConveyorGeometry _geometry;
    private IngredientCatalog _catalog;

    // Логический ингредиент -> его спрайт на сцене (как словарь _cubes в лесах).
    private readonly Dictionary<Ingredient, SpriteRenderer> _views
      = new Dictionary<Ingredient, SpriteRenderer>();

    // Буфер для удаления исчезнувших (чтобы не модифицировать словарь в foreach).
    private readonly List<Ingredient> _toRemove = new List<Ingredient>();

    [Inject]
    public void Construct(
      ConveyorSystem conveyor,
      ConveyorGeometry geometry,
      IngredientCatalog catalog)
    {
      _conveyor = conveyor;
      _geometry = geometry;
      _catalog = catalog;
    }

    // LateUpdate: логика (ConveyorSystem.Tick) успевает сдвинуть Progress в этом
    // кадре раньше нас — рисуем уже актуальные позиции без отставания на кадр.
    private void LateUpdate()
    {
      if (_conveyor == null) return;

      SyncViews();
    }

    // Синхронизировать спрайты с логическим списком Active.
    private void SyncViews()
    {
      var active = _conveyor.Active;

      // 1. Новые ингредиенты — создать спрайт из префаба, картинка из каталога.
      for (int i = 0; i < active.Count; i++)
      {
        Ingredient ing = active[i];
        if (_views.ContainsKey(ing)) continue;

        SpriteRenderer view = Instantiate(_ingredientPrefab, transform);
        view.sprite = SpriteFor(ing.Type);
        view.transform.localScale = _scale;
        _views[ing] = view;
      }

      // 2. Обновить позиции, собрать исчезнувших из Active.
      _toRemove.Clear();
      foreach (var kv in _views)
      {
        Ingredient ing = kv.Key;
        SpriteRenderer view = kv.Value;

        if (!Contains(active, ing))
        {
          if (view != null) Destroy(view.gameObject);
          _toRemove.Add(ing);
          continue;
        }

        view.transform.position = _geometry.PositionOf(ing.Side, ing.Progress);
      }

      // 3. Убрать исчезнувших из словаря.
      for (int i = 0; i < _toRemove.Count; i++)
        _views.Remove(_toRemove[i]);
    }

    // Спрайт ТОЛЬКО из каталога (не хардкодим). null — разработчик не заполнил Icon.
    private Sprite SpriteFor(IngredientType type)
    {
      IngredientDefinition def = _catalog != null ? _catalog.GetByType(type) : null;
      if (def == null || def.Icon == null)
      {
        Debug.LogWarning($"[IngredientView] Нет спрайта для {type} в IngredientCatalog.");
        return null;
      }
      return def.Icon;
    }

    // Поиск по списку без LINQ/аллокаций (вызывается каждый кадр).
    private static bool Contains(IReadOnlyList<Ingredient> list, Ingredient ing)
    {
      for (int i = 0; i < list.Count; i++)
        if (ReferenceEquals(list[i], ing))
          return true;
      return false;
    }
  }
}
