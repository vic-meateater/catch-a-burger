namespace BurgerCatch.Events
{
  public sealed class LifeLostSignal
  {
    public int Remaining { get; }

    public LifeLostSignal(int remaining)
    {
      Remaining = remaining;
    }
  }
}
