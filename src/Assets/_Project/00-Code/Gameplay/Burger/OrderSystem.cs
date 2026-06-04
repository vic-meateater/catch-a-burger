using System;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Burger;
using BurgerCatch.Gameplay.Conveyor;
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
    public IngredientType[] CurrentRecipe => Recipe;

    
    private readonly SignalBus _signalBus;
    private readonly BurgerStack _stack;

    // Day 11: захардкоженный рецепт. Позже — случайный из конфига рецептов.
    private static readonly IngredientType[] Recipe =
    {
      IngredientType.Bun,
      IngredientType.Patty,
      IngredientType.Cheese,
      IngredientType.Bun,
    };

    private int _index; // указатель: какой ингредиент рецепта нужен сейчас

    public OrderSystem(SignalBus signalBus, BurgerStack stack)
    {
      _signalBus = signalBus;
      _stack = stack;
    }

    /// <summary>Ингредиент, который нужен прямо сейчас.</summary>
    public IngredientType Current => Recipe[_index];

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

        if (_index >= Recipe.Length)
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
      _signalBus.Fire(new OrderChangedSignal(Recipe)); // новый тикет: UI, директор (передышка)
    }
  }
}