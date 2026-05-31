namespace BurgerCatch.Core.Flow
{
  /// <summary>
  /// Состояния игрового автомата. Управляются GameFlowController.
  /// Booting   — ждём SDK (сцена Bootstrap).
  /// Menu      — главное меню (сцена MainMenu).
  /// Ready     — забег загружен, ждём старта/отсчёта.
  /// Running   — активный геймплей, игровое время идёт.
  /// Paused    — заморозка (фокус вкладки / реклама / ручная пауза).
  /// Resuming  — отсчёт 3-2-1, единый выход из любой паузы.
  /// GameOver  — забег окончен.
  /// </summary>
  public enum GameState
  {
    Booting,
    Menu,
    Ready,
    Running,
    Paused,
    Resuming,
    GameOver
  }
}