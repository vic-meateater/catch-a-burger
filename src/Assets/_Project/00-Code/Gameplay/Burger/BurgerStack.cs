using System.Collections.Generic;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Conveyor;
using Zenject;

namespace BurgerCatch.Gameplay.Burger
{
  /// <summary>
  /// Фактически собранный бургер ("тарелка"): слои по порядку поимки,
  /// включая грязные. НЕ знает про заказ и цену — ему говорят, что класть.
  /// Источник правды для визуала бургера.
  /// </summary>
  public sealed class BurgerStack
  {
    private readonly SignalBus _signalBus;
    private readonly List<BurgerLayer> _layers = new List<BurgerLayer>();

    public BurgerStack(SignalBus signalBus)
    {
      _signalBus = signalBus;
    }

    public IReadOnlyList<BurgerLayer> Layers => _layers;
    public int DirtyCount { get; private set; }

    /// <summary>Добавить слой. isDirty решает вызывающий (OrderSystem).</summary>
    public void AddLayer(IngredientType type, bool isDirty)
    {
      var layer = new BurgerLayer(type, isDirty);
      _layers.Add(layer);
      if (isDirty) DirtyCount++;

      _signalBus.Fire(new BurgerLayerAddedSignal(type, isDirty, _layers.Count));
    }

    /// <summary>Очистить тарелку (продажа собранного / протухание). Новый бургер.</summary>
    public void Clear()
    {
      _layers.Clear();
      DirtyCount = 0;
      _signalBus.Fire(new BurgerStackClearedSignal());
    }
  }
}