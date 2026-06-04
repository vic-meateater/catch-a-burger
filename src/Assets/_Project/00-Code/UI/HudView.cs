using System.Collections.Generic;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Order;
using BurgerCatch.Gameplay.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Игровой HUD: ряд иконок заказа (с подсветкой текущего), счёт и жизни.
  /// ТОНКИЙ слой — только слушает сигналы и читает сервисы, своей логики нет.
  /// </summary>
  public sealed class HudView : MonoBehaviour
  {
    // --- Заказ (тикет) ---
    [Header("Заказ")]
    [Tooltip("Контейнер, в который складываются иконки слотов заказа.")]
    [SerializeField] private Transform _orderSlotsParent;

    [Tooltip("Префаб одной иконки слота заказа (Image). Разработчик сделает префаб.")]
    [SerializeField] private Image _orderSlotPrefab;

    // Сопоставление IngredientType -> Sprite двумя параллельными массивами.
    // РАЗРАБОТЧИК заполняет: _types[i] соответствует _sprites[i].
    // (Сериализуемого словаря в Unity нет — поэтому два массива + ручной поиск.)
    [Tooltip("Типы ингредиентов (в паре со спрайтами ниже, по индексу).")]
    [SerializeField] private IngredientType[] _types;

    [Tooltip("Спрайты ингредиентов (в паре с типами выше, по индексу).")]
    [SerializeField] private Sprite[] _sprites;

    [Header("Подсветка слотов")]
    [Tooltip("Масштаб текущего нужного слота (выделяем крупнее).")]
    [SerializeField] private float _currentScale = 1.2f;

    [Tooltip("Прозрачность уже собранных слотов (тусклые).")]
    [SerializeField] private float _doneAlpha = 0.4f;

    // --- Счёт ---
    [Header("Счёт")]
    [Tooltip("Текст счёта забега.")]
    [SerializeField] private TMP_Text _scoreText;

    // --- Жизни ---
    [Header("Жизни")]
    [Tooltip("Иконки-сердца (по убыванию гасим лишние). Напр. 3 штуки.")]
    [SerializeField] private Image[] _lifeIcons;

    private SignalBus _signalBus;
    private OrderSystem _order;
    private ScoringSystem _scoring;

    // Созданные иконки текущего заказа — чтобы перестраивать/подсвечивать.
    private readonly List<Image> _orderSlots = new List<Image>();

    /// <summary>Зависимости от Zenject (MonoBehaviour — через метод, не конструктор).</summary>
    [Inject]
    public void Construct(SignalBus signalBus, OrderSystem order, ScoringSystem scoring)
    {
      _signalBus = signalBus;
      _order = order;
      _scoring = scoring;
    }

    private void OnEnable()
    {
      _signalBus.Subscribe<OrderChangedSignal>(OnOrderChanged);
      _signalBus.Subscribe<OrderItemMatchedSignal>(OnOrderItemMatched);
      _signalBus.Subscribe<OrderCompletedSignal>(OnOrderCompleted);
      _signalBus.Subscribe<LifeLostSignal>(OnLifeLost);
      _signalBus.Subscribe<LifeGainedSignal>(OnLifeGained);

      // Берём ТЕКУЩЕЕ состояние сразу, не ждём первого сигнала.
      RebuildOrder(_order.CurrentRecipe);
      RefreshScore();
      RefreshAllLives();
    }

    private void OnDisable()
    {
      _signalBus.Unsubscribe<OrderChangedSignal>(OnOrderChanged);
      _signalBus.Unsubscribe<OrderItemMatchedSignal>(OnOrderItemMatched);
      _signalBus.Unsubscribe<OrderCompletedSignal>(OnOrderCompleted);
      _signalBus.Unsubscribe<LifeLostSignal>(OnLifeLost);
      _signalBus.Unsubscribe<LifeGainedSignal>(OnLifeGained);
    }

    // --- Заказ ---

    // Новый заказ: пересобрать ряд иконок по рецепту.
    private void OnOrderChanged(OrderChangedSignal s) => RebuildOrder(s.Recipe);

    // Совпал слой / собран бургер — просто обновить подсветку по указателю.
    private void OnOrderItemMatched(OrderItemMatchedSignal s) => RefreshHighlight();

    // Удалить старые иконки и создать новые под текущий рецепт.
    private void RebuildOrder(IngredientType[] recipe)
    {
      // Снести прошлые.
      for (int i = 0; i < _orderSlots.Count; i++)
        if (_orderSlots[i] != null) Destroy(_orderSlots[i].gameObject);
      _orderSlots.Clear();

      if (recipe == null || _orderSlotPrefab == null || _orderSlotsParent == null)
        return;

      // По одной иконке на каждый ингредиент рецепта.
      for (int i = 0; i < recipe.Length; i++)
      {
        Image slot = Instantiate(_orderSlotPrefab, _orderSlotsParent);
        slot.sprite = SpriteFor(recipe[i]);
        _orderSlots.Add(slot);
      }

      RefreshHighlight();
    }

    // Подсветить текущий нужный слот, собранные — приглушить.
    private void RefreshHighlight()
    {
      int current = _order.CurrentIndex;

      for (int i = 0; i < _orderSlots.Count; i++)
      {
        Image slot = _orderSlots[i];
        if (slot == null) continue;

        bool isDone = i < current;     // уже собран
        bool isCurrent = i == current; // нужен сейчас

        // Масштаб: текущий крупнее, остальные обычные.
        float scale = isCurrent ? _currentScale : 1f;
        slot.transform.localScale = Vector3.one * scale;

        // Прозрачность: собранные тусклые.
        Color c = slot.color;
        c.a = isDone ? _doneAlpha : 1f;
        slot.color = c;
      }
    }

    // Поиск спрайта по типу в параллельных массивах (заполняет разработчик).
    private Sprite SpriteFor(IngredientType type)
    {
      if (_types != null && _sprites != null)
        for (int i = 0; i < _types.Length && i < _sprites.Length; i++)
          if (_types[i] == type)
            return _sprites[i];
      return null;
    }

    // --- Счёт ---

    private void OnOrderCompleted(OrderCompletedSignal s)
    {
      RefreshScore();
      RefreshHighlight(); // заказ сбросился — подсветка на первый слот
    }

    // Счёт читаем из сервиса (он — источник истины).
    private void RefreshScore()
    {
      if (_scoreText != null)
        _scoreText.text = _scoring.RunScore.ToString();
    }

    // --- Жизни ---

    // Потеря жизни несёт, сколько осталось — показываем столько сердец.
    private void OnLifeLost(LifeLostSignal s) => SetLives(s.Remaining);

    // Возврат жизни (continue/пикап) несёт текущее число.
    private void OnLifeGained(LifeGainedSignal s) => SetLives(s.Current);

    // При старте — все сердца зажжены (полное число).
    private void RefreshAllLives()
    {
      if (_lifeIcons != null) SetLives(_lifeIcons.Length);
    }

    // Зажечь первые count сердец, остальные погасить.
    private void SetLives(int count)
    {
      if (_lifeIcons == null) return;
      for (int i = 0; i < _lifeIcons.Length; i++)
        if (_lifeIcons[i] != null)
          _lifeIcons[i].enabled = i < count;
    }
  }
}
