using System;
using BurgerCatch.Data;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Burger;
using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Gameplay.Order
{
  /// <summary>
  /// "Тикет": текущий рецепт + указатель на нужный следующий ингредиент.
  /// Решает, чистый слой или грязный, двигает указатель только на правильном.
  /// При завершении заказа выдаёт новый. НЕ знает про цену.
  /// </summary>
  public sealed class OrderSystem : IInitializable, IDisposable
  {
    public int CurrentIndex => _index;
    public IngredientType[] CurrentRecipe => _recipe;


    private readonly SignalBus _signalBus;
    private readonly BurgerStack _stack;
    private readonly RecipeCatalog _recipeCatalog;

    // Заглушка на случай пустого/невалидного каталога — чтобы не крашить
    // (используется только если каталог не дал рецепт и _recipe ещё пуст).
    private static readonly IngredientType[] FallbackRecipe =
    {
      IngredientType.BottomBun,
      IngredientType.Patty,
      IngredientType.Cheese,
      IngredientType.TopBun,
    };

    // Текущий рецепт (случайный из каталога). Раньше был static-хардкод.
    private IngredientType[] _recipe;

    private int _index; // указатель: какой ингредиент рецепта нужен сейчас

    public OrderSystem(SignalBus signalBus, BurgerStack stack, RecipeCatalog recipeCatalog)
    {
      _signalBus = signalBus;
      _stack = stack;
      _recipeCatalog = recipeCatalog;
    }

    /// <summary>Ингредиент, который нужен прямо сейчас.</summary>
    public IngredientType Current => _recipe[_index];

    public void Initialize()
    {
      StartNewOrder();
      _signalBus.Subscribe<IngredientCaughtSignal>(OnCaught);
      _signalBus.Subscribe<BurgerSpoiledSignal>(OnBurgerSpoiled);
    }


    public void Dispose()
    {
      _signalBus.Unsubscribe<IngredientCaughtSignal>(OnCaught);
      _signalBus.Unsubscribe<BurgerSpoiledSignal>(OnBurgerSpoiled);
    }

    private void OnCaught(IngredientCaughtSignal s)
    {
      if (s.Type == Current)
      {
        _stack.AddLayer(s.Type, isDirty: false);
        _index++;

        if (_index >= _recipe.Length)
        {
          // Бургер собран. Сначала завершаем (это сбросит _index=0 и
          // выдаст новый заказ через OrderChanged), и только это сообщаем.
          CompleteOrder();
        }
        else
        {
          // Бургер ещё собирается: указатель валиден, сообщаем "слой совпал".
          _signalBus.Fire(new OrderItemMatchedSignal(s.Type));
        }
      }
    }

    private void OnBurgerSpoiled(BurgerSpoiledSignal s)
    {
      _stack.Clear();
      StartNewOrder();
    }

    private void CompleteOrder()
    {
      // Бургер собран. Продажа за текущую цену — реагирует ScoringSystem (Day 13).
      _signalBus.Fire(new OrderCompletedSignal());
      _stack.Clear();
      StartNewOrder();
    }

    private void StartNewOrder()
    {
      _index = 0;
      _recipe = PickRecipe();
      _signalBus.Fire(new OrderChangedSignal(_recipe)); // новый тикет: UI, директор (передышка)
    }

    // Случайный рецепт из каталога. При пустом/невалидном каталоге — ошибка в лог
    // и заглушка (прошлый рецепт, если был; иначе FallbackRecipe), без краша.
    private IngredientType[] PickRecipe()
    {
      RecipeDefinition def = _recipeCatalog != null ? _recipeCatalog.GetRandom() : null;

      if (def != null && def.Sequence != null && def.Sequence.Count > 0)
        return def.ToArray();

      Debug.LogError("[OrderSystem] RecipeCatalog пуст или невалиден — используется заглушка.");
      return _recipe ?? FallbackRecipe;
    }
  }
}