using System;
using BurgerCatch.Core.Platform;
using YG;

namespace BurgerCatch.Yandex
{
  public sealed class YandexPlatformService : IPlatformService, IDisposable
  {
    public bool IsReady => YG2.isSDKEnabled;

    private Action _readyCallback;
    private bool _subscribed;

    public void WhenReady(Action callback)
    {
      // Гонка инициализации: SDK мог инициализироваться РАНЬШЕ,
      // чем мы успели подписаться. Поэтому сначала проверяем флаг.
      if (IsReady)
      {
        callback?.Invoke();
        return;
      }

      _readyCallback = callback;

      if (!_subscribed)
      {
        YG2.onGetSDKData += OnSdkReady;
        _subscribed = true;
      }
    }

    private void OnSdkReady()
    {
      Unsubscribe();
      _readyCallback?.Invoke();
      _readyCallback = null;
    }

    private void Unsubscribe()
    {
      if (!_subscribed) return;
      YG2.onGetSDKData -= OnSdkReady;
      _subscribed = false;
    }

    public void Dispose() => Unsubscribe();
  }
}