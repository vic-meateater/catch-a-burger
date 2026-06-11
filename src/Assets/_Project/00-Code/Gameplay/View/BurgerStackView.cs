using System.Collections.Generic;
using BurgerCatch.Data;
using BurgerCatch.Events;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Gameplay.View
{
  /// <summary>
  /// Растущая башня бургера: слои снизу вверх от якоря, по порядку поимки.
  /// ТОНКИЙ слой: слушает сигналы BurgerStack (логика там), сам ничего не решает.
  /// Грязный слой — тот же ингредиент, но затемнённый.
  /// </summary>
  public sealed class BurgerStackView : MonoBehaviour
  {
    [Tooltip("Якорь — низ бургера. Башня растёт от него вверх. Разработчик поставит на сцене.")]
    [SerializeField] private Transform _stackAnchor;

    [Tooltip("Префаб слоя (GameObject со SpriteRenderer). Картинку подставим из каталога.")]
    [SerializeField] private SpriteRenderer _layerPrefab;

    [Tooltip("ФИКСИРОВАННАЯ высота слоя (не по спрайту). Разработчик подгонит под арт.")]
    [SerializeField] private float _layerHeight = 0.3f;

    [Tooltip("Множитель яркости грязного слоя (0..1, меньше = темнее).")]
    [SerializeField] private float _dirtyDarken = 0.5f;

    private SignalBus _signalBus;
    private IngredientCatalog _catalog;

    // Нарисованные слои по порядку (снизу вверх) — для очистки и индекса позиции.
    private readonly List<SpriteRenderer> _layers = new List<SpriteRenderer>();

    [Inject]
    public void Construct(SignalBus signalBus, IngredientCatalog catalog)
    {
      _signalBus = signalBus;
      _catalog = catalog;
    }

    private void OnEnable()
    {
      _signalBus.Subscribe<BurgerLayerAddedSignal>(OnLayerAdded);
      _signalBus.Subscribe<BurgerStackClearedSignal>(OnStackCleared);
    }

    private void OnDisable()
    {
      // Guard: объект мог отключиться до инъекции зависимостей.
      if (_signalBus == null) return;

      _signalBus.Unsubscribe<BurgerLayerAddedSignal>(OnLayerAdded);
      _signalBus.Unsubscribe<BurgerStackClearedSignal>(OnStackCleared);
    }

    // Логика положила слой — рисуем его поверх уже нарисованных.
    private void OnLayerAdded(BurgerLayerAddedSignal s)
    {
      if (_layerPrefab == null || _stackAnchor == null) return;

      SpriteRenderer layer = Instantiate(_layerPrefab, _stackAnchor);

      // Картинка ТОЛЬКО из каталога (не хардкодим).
      IngredientDefinition def = _catalog != null ? _catalog.GetByType(s.Type) : null;
      if (def != null && def.Icon != null)
        layer.sprite = def.Icon;
      else
        Debug.LogWarning($"[BurgerStackView] Нет спрайта для {s.Type} в IngredientCatalog.");

      // Позиция: якорь + вверх на (число уже нарисованных слоёв) * высоту слоя.
      // Первый слой — у якоря, каждый следующий выше. Высота ФИКСИРОВАННАЯ.
      int index = _layers.Count;
      layer.transform.position = _stackAnchor.position + Vector3.up * (index * _layerHeight);

      // Грязный слой — затемняем (тот же ингредиент, но темнее; alpha не трогаем).
      if (s.IsDirty)
      {
        Color c = layer.color * _dirtyDarken;
        c.a = layer.color.a;
        layer.color = c;
      }

      _layers.Add(layer);
    }

    // Башня обнулилась (продажа/протухание) — снести все нарисованные слои.
    private void OnStackCleared(BurgerStackClearedSignal s)
    {
      for (int i = 0; i < _layers.Count; i++)
        if (_layers[i] != null) Destroy(_layers[i].gameObject);
      _layers.Clear();
    }
  }
}
