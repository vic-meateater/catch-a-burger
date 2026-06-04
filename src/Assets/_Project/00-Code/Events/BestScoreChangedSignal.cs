namespace BurgerCatch.Events
{
  /// <summary>Обновлён рекорд забега.</summary>
  public sealed class BestScoreChangedSignal
  {
    public int Score { get; }
    public BestScoreChangedSignal(int score) => Score = score;
  }
}
