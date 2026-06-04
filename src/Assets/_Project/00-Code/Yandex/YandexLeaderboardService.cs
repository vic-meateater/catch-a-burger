using BurgerCatch.Core.Leaderboard;
using UnityEngine;

namespace BurgerCatch.Yandex
{
  /// <summary>
  /// Лидерборд через Plugin YG. ЕДИНСТВЕННОЕ место с YG2.* по лидерборду.
  /// </summary>
  public sealed class YandexLeaderboardService : ILeaderboardService
  {
    // TODO: задать реальное техническое имя лидерборда из дашборда Яндекса.
    private const string TechnicalName = "score";

    public void SubmitScore(int score)
    {
      // TODO: модуль лидерборда Plugin YG в текущей сборке НЕ установлен —
      // метода YG2.SetLeaderboard нет, поэтому вызов закомментирован, чтобы не
      // ломать компиляцию. Включить модуль лидерборда в настройках плагина и
      // раскомментировать строку ниже (сверив точное имя метода по доке).
      // YG.YG2.SetLeaderboard(TechnicalName, score);

      Debug.Log($"[Leaderboard] SubmitScore stub: {TechnicalName} = {score}");
    }
  }
}
