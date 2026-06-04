namespace BurgerCatch.Core.Leaderboard
{
  /// <summary>Отправка счёта в лидерборд платформы.</summary>
  public interface ILeaderboardService
  {
    void SubmitScore(int score);
  }
}
