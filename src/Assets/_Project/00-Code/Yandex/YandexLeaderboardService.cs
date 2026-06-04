using BurgerCatch.Core.Leaderboard;
using YG;

namespace BurgerCatch.Yandex
{
  /// <summary>
  /// Лидерборд через Plugin YG. ЕДИНСТВЕННОЕ место с YG2.* по лидерборду.
  /// YG2.SetLeaderboard сам проверяет авторизацию игрока и включённость модуля —
  /// если не выполнено, отправка молча пропускается (в редакторе логирует заглушку).
  /// </summary>
  public sealed class YandexLeaderboardService : ILeaderboardService
  {
    // TODO: задать реальное техническое имя лидерборда из дашборда Яндекса.
    private const string TechnicalName = "score";

    public void SubmitScore(int score)
    {
      YG2.SetLeaderboard(TechnicalName, score);
    }
  }
}
