using System.Collections.Generic;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Order;
using UnityEngine;
using Zenject;

namespace BurgerCatch_Sandbox
{
  /// <summary>
  /// ВРЕМЕННЫЕ ЛЕСА. Грязный индикатор заказа: ряд цветных квадратов сверху,
  /// текущий нужный — подсвечен (крупнее + ярче). Умрёт с приходом настоящего
  /// UI на Canvas. Никакой UI-архитектуры — те же спрайт-квадраты.
  /// </summary>
  public sealed class TestOrderHud : MonoBehaviour
  {
    [SerializeField] private float _topY = 4.5f; // высота ряда
    [SerializeField] private float _stepX = 1.1f; // расстояние между иконками
    [SerializeField] private float _normalSize = 0.7f;
    [SerializeField] private float _activeSize = 1.0f; // текущий — крупнее

    private OrderSystem _order;
    private SignalBus _signalBus;

    private Sprite _square;
    private readonly List<SpriteRenderer> _icons = new List<SpriteRenderer>();
    private IngredientType[] _recipe;

    [Inject]
    public void Construct(OrderSystem order, SignalBus signalBus)
    {
      _order = order;
      _signalBus = signalBus;
    }

    private void Start()
    {
      Debug.Log($"[HUD] start. order={_order != null}, bus={_signalBus != null}");

      _square = Sprite.Create(
        Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
        new Vector2(0.5f, 0.5f), 1f);

      _signalBus.Subscribe<OrderChangedSignal>(OnOrderChanged);
      _signalBus.Subscribe<OrderItemMatchedSignal>(_ => Redraw());
      
      _recipe = _order.CurrentRecipe; // см. ниже
      BuildIcons();
      Redraw();
    }

    private void OnDestroy()
    {
      _signalBus.TryUnsubscribe<OrderChangedSignal>(OnOrderChanged);
      // matched-подписка анонимная; для лесов не критично, но в бою так не делают
    }

    private void OnOrderChanged(OrderChangedSignal s)
    {
      _recipe = s.Recipe;
      BuildIcons();
      Redraw();
    }

    private void BuildIcons()
    {
      // Снести старые иконки, создать по числу слотов рецепта.
      foreach (var sr in _icons)
        if (sr != null)
          Destroy(sr.gameObject);
      _icons.Clear();

      if (_recipe == null) return;

      float startX = -(_recipe.Length - 1) * _stepX * 0.5f; // центрируем ряд
      for (int i = 0; i < _recipe.Length; i++)
      {
        var go = new GameObject($"__HUD_slot_{i}");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _square;
        sr.sortingOrder = 100; // поверх всего
        go.transform.position = new Vector3(startX + i * _stepX, _topY, 0f);
        _icons.Add(sr);
      }
    }

    private void Redraw()
    {
      if (_recipe == null) return;

      int current = _order.CurrentIndex;
      for (int i = 0; i < _icons.Count; i++)
      {
        var sr = _icons[i];
        if (sr == null) continue;

        Color c = ColorOf(_recipe[i]);
        bool isCurrent = i == current;

        // Уже собранные (до указателя) — притушить; текущий — яркий+крупный.
        if (i < current) c *= 0.4f; // сделано — тускло
        else if (!isCurrent) c *= 0.8f; // впереди — чуть тусклее

        sr.color = c;
        float size = isCurrent ? _activeSize : _normalSize;
        sr.transform.localScale = new Vector3(size, size, 1f);
      }
    }

    // Индекс текущего нужного = позиция Current в рецепте.
    // Проще: считаем, сколько уже собрано, по совпадению Current с рецептом.
    private int CurrentIndex()
    {
      // OrderSystem.Current — нужный сейчас. Находим его позицию начиная
      // с предполагаемой. Грязно, но для лесов сойдёт: ищем первый слот,
      // тип которого == Current и который ещё не пройден.
      // Надёжнее — отдать индекс из OrderSystem (см. примечание ниже).
      for (int i = 0; i < _recipe.Length; i++)
        if (_recipe[i] == _order.Current)
          return i;
      return _recipe.Length;
    }

    private static Color ColorOf(IngredientType type)
    {
      switch (type)
      {
        case IngredientType.Bun: return new Color(1, 0.0f, 0.3f);
        case IngredientType.Patty: return new Color(0.45f, 0.25f, 0.1f);
        case IngredientType.Cheese: return new Color(1f, 0.6f, 0.0f);
        default: return Color.white;
      }
    }
  }
}