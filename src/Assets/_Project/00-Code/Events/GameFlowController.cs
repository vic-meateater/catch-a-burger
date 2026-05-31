using BurgerCatch.Core.Flow;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Events
{
  public sealed class GameFlowController
  {
    public GameState Current { get; private set; } = GameState.Booting;

    private readonly SignalBus _signalBus;

    public GameFlowController(SignalBus signalBus)
    {
      _signalBus = signalBus;
    }

    /// <summary>
    /// Единственная точка смены состояния. Все переходы — через неё.
    /// </summary>
    public void SetState(GameState next)
    {
      if (next == Current) return;

      var previous = Current;
      Current = next;

      Debug.Log($"[Flow] {previous} -> {next}");
      _signalBus.Fire(new GameStateChangedSignal(next, previous));
    }
  }
}