using System;
using BurgerCatch.Core.Ads;
using YG;

namespace BurgerCatch.Yandex
{
  /// <summary>
  /// Реализация рекламы через Plugin YG (PluginYourGames). ЕДИНСТВЕННОЕ место с
  /// YG2.* по рекламе — SDK за абстракцией IAdService, наружу не течёт.
  /// Вокруг показа: GameplayStop перед, GameplayStart после фактического закрытия
  /// (по реальному колбэку плагина). GameplayStart/Stop идемпотентны — лишний вызов безопасен.
  /// </summary>
  public sealed class YandexAdService : IAdService
  {
    public void ShowInterstitial(Action onClosed = null)
    {
      // Плагин молча не покажет рекламу, если показ уже идёт или не вышел
      // частотный таймер — колбэк закрытия тогда не придёт. Не трогаем платформу,
      // сразу отдаём управление, чтобы не подвиснуть.
      if (YG2.nowAdsShow || !YG2.isTimerAdvCompleted)
      {
        onClosed?.Invoke();
        return;
      }

      YG2.GameplayStop();

      // Одноразовый колбэк закрытия/ошибки: resume + onClosed, затем отписка.
      Action onClose = null;
      onClose = () =>
      {
        YG2.onCloseInterAdv -= onClose;
        YG2.onErrorInterAdv -= onClose;
        onClosed?.Invoke();
        YG2.GameplayStart();
      };
      YG2.onCloseInterAdv += onClose;
      YG2.onErrorInterAdv += onClose;

      YG2.InterstitialAdvShow();
    }

    public void ShowRewarded(string rewardId, Action onReward, Action onClosed = null)
    {
      // Если уже идёт показ — плагин проигнорирует вызов (колбэков не будет).
      if (YG2.nowAdsShow)
      {
        onClosed?.Invoke();
        return;
      }

      YG2.GameplayStop();

      // Одноразовый колбэк закрытия (с наградой или без) / ошибки: resume + onClosed.
      Action onClose = null;
      onClose = () =>
      {
        YG2.onCloseRewardedAdv -= onClose;
        YG2.onErrorRewardedAdv -= onClose;
        onClosed?.Invoke();
        YG2.GameplayStart();
      };
      YG2.onCloseRewardedAdv += onClose;
      YG2.onErrorRewardedAdv += onClose;

      // Награда — ТОЛЬКО при успешном полном просмотре (per-call колбэк плагина).
      YG2.RewardedAdvShow(rewardId, () => onReward?.Invoke());
    }
  }
}
