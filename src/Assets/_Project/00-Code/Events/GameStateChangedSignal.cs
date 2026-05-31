using BurgerCatch.Core.Flow;

namespace BurgerCatch.Events
{
  public sealed class GameStateChangedSignal
  {
    public GameState Current { get; }
    public GameState Previous { get; }

    public GameStateChangedSignal(GameState current, GameState previous)
    {
      Current = current;
      Previous = previous;
    } 
  }
}