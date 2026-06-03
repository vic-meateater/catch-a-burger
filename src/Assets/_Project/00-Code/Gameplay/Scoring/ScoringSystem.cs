using System;
using BurgerCatch.Events;
using Zenject;

namespace BurgerCatch.Gameplay.Scoring
{
  /// <summary>
  /// Базовая экономика забега: цена текущего бургера и общий счёт.
  /// Грязный слой списывает цену, протухание стреляет факт, сборка начисляет в счёт.
  /// Без комбо/множителей (отдельная задача).
  /// </summary>
  public sealed class ScoringSystem : IInitializable, IDisposable
  {
    private const int BasePrice = 100;
    private const int DirtyPenalty = 10;
    private const int MinPrice = 0;

    private readonly SignalBus _signalBus;

    public int CurrentPrice { get; private set; }
    public int RunScore { get; private set; }

    public ScoringSystem(SignalBus signalBus)
    {
      _signalBus = signalBus;
    }

    public void Initialize()
    {
      CurrentPrice = BasePrice;
      RunScore = 0;

      _signalBus.Subscribe<OrderItemWrongSignal>(OnOrderItemWrong);
      _signalBus.Subscribe<OrderCompletedSignal>(OnOrderCompleted);
      _signalBus.Subscribe<OrderChangedSignal>(OnOrderChanged);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<OrderItemWrongSignal>(OnOrderItemWrong);
      _signalBus.Unsubscribe<OrderCompletedSignal>(OnOrderCompleted);
      _signalBus.Unsubscribe<OrderChangedSignal>(OnOrderChanged);
    }

    // Грязный слой: списать цену, при достижении 0 — протухание (ровно один раз).
    private void OnOrderItemWrong(OrderItemWrongSignal _)
    {
      // Цена уже на минимуме — больше ничего не меняется, повторно spoiled не стреляем.
      if (CurrentPrice <= MinPrice) return;

      CurrentPrice -= DirtyPenalty;
      if (CurrentPrice < MinPrice) CurrentPrice = MinPrice;

      _signalBus.Fire(new BurgerPriceChangedSignal(CurrentPrice));

      // Цена ИМЕННО в этот момент стала 0 (была > 0) → бургер протух.
      if (CurrentPrice == MinPrice)
        _signalBus.Fire(new BurgerSpoiledSignal());
    }

    // Бургер собран по заказу: текущая цена идёт в общий счёт.
    private void OnOrderCompleted(OrderCompletedSignal _)
    {
      RunScore += CurrentPrice;
      _signalBus.Fire(new RunScoreChangedSignal(RunScore));
      // Цену НЕ сбрасываем здесь — сброс на OrderChangedSignal (новый заказ).
    }

    // Начался новый бургер (после сборки ИЛИ протухания): цена обратно к базе.
    private void OnOrderChanged(OrderChangedSignal _)
    {
      CurrentPrice = BasePrice;
      _signalBus.Fire(new BurgerPriceChangedSignal(CurrentPrice));
    }
  }
}
