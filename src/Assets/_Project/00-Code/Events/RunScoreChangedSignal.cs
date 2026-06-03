namespace BurgerCatch.Events
{
  /// <summary>Общий счёт забега изменился → UI обновит счётчик (Фаза UI).</summary>
  public sealed class RunScoreChangedSignal
  {
    public int Score { get; }
    public RunScoreChangedSignal(int score) => Score = score;
  }
}
