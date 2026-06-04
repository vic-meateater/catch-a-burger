using System;
using BurgerCatch.Events;
using Zenject;

namespace BurgerCatch.Gameplay.Lives
{
  public sealed class LivesSystem : IInitializable, IDisposable
  {
    private const int StartLives = 3;
    private const int MaxLives = 5;

    private readonly SignalBus _signalBus;

    private bool _isOver;

    public int Current { get; private set; }

    public LivesSystem(SignalBus signalBus)
    {
      _signalBus = signalBus;
    }

    public void Initialize()
    {
      Current = StartLives;
      _signalBus.Subscribe<IngredientDroppedSignal>(OnIngredientDropped);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<IngredientDroppedSignal>(OnIngredientDropped);
    }

    // Продолжение за rewarded: вернуть в игру с одной жизнью, снять game over.
    // Вызывается ТОЛЬКО из onReward (игрок посмотрел рекламу).
    public void Revive()
    {
      _isOver = false;
      Current = 1;
      _signalBus.Fire(new LifeGainedSignal(Current));
    }

    // Задел для пикапа-сердца (Day 14). Сейчас нигде не вызывается.
    public void Gain()
    {
      if (_isOver) return;
      if (Current >= MaxLives) return;

      Current++;
      _signalBus.Fire(new LifeGainedSignal(Current));
    }

    private void OnIngredientDropped(IngredientDroppedSignal _)
    {
      if (_isOver) return;

      Current--;
      if (Current <= 0)
      {
        Current = 0;
        _isOver = true;
        _signalBus.Fire(new LifeLostSignal(Current));
        _signalBus.Fire(new GameOverTriggeredSignal());
      }
      else
      {
        _signalBus.Fire(new LifeLostSignal(Current));
      }
    }
  }
}
