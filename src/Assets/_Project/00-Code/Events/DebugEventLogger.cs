using System;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Events
{
  public sealed class DebugEventLogger : IInitializable, IDisposable
  {
    private readonly SignalBus _signalBus;

    public DebugEventLogger(SignalBus signalBus)
    {
      _signalBus = signalBus;
    }

    public void Initialize()
    {
      _signalBus.Subscribe<IngredientCaughtSignal>(OnCaught);
      _signalBus.Subscribe<IngredientDroppedSignal>(OnDropped);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<IngredientCaughtSignal>(OnCaught);
      _signalBus.Unsubscribe<IngredientDroppedSignal>(OnDropped);
    }

    private void OnCaught(IngredientCaughtSignal s)
      => Debug.Log($"[Event] Caught {s.Type} @ {s.Side}");

    private void OnDropped(IngredientDroppedSignal s)
      => Debug.Log($"[Event] Dropped {s.Type} @ {s.Side}");
  }
}
