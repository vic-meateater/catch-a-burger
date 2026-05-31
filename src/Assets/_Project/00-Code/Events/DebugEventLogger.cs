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
    }
    
    public void Dispose()
    {
    }

  }
}