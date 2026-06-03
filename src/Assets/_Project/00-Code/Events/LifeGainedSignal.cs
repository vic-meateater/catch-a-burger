namespace BurgerCatch.Events
{
  public sealed class LifeGainedSignal
  {
    public int Current { get; }

    public LifeGainedSignal(int current)
    {
      Current = current;
    }
  }
}
