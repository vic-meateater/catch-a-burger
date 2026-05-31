using System;

namespace BurgerCatch.Core.Platform
{
  /// <summary>
  /// Абстракция над платформенным SDK (Yandex/Plugin YG и др.).
  /// Прячет конкретный плагин за интерфейсом — единственная точка
  /// контакта с внешним SDK во всём проекте.
  /// </summary>
  public interface IPlatformService
  {
    /// <summary>SDK уже инициализирован и готов к работе.</summary>
    bool IsReady { get; }

    /// <summary>
    /// Подписаться на готовность SDK.
    /// ВАЖНО: если SDK уже готов на момент подписки, колбэк
    /// вызывается немедленно (закрывает гонку инициализации).
    /// </summary>
    void WhenReady(Action callback);
  }
}