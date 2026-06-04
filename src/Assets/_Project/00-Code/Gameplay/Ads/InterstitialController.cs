using System;
using BurgerCatch.Core.Ads;
using BurgerCatch.Data;
using BurgerCatch.Events;
using Zenject;

namespace BurgerCatch.Gameplay.Ads
{
  /// <summary>
  /// Interstitial на game over с частотным лимитом: не на каждый game over,
  /// а на каждый N-й (N из конфига). Событийно, без таймеров.
  /// </summary>
  public sealed class InterstitialController : IInitializable, IDisposable
  {
    private readonly SignalBus _signalBus;
    private readonly IAdService _adService;
    private readonly GameplayConfig _config;

    private int _gameOverCount;

    public InterstitialController(SignalBus signalBus, IAdService adService, GameplayConfig config)
    {
      _signalBus = signalBus;
      _adService = adService;
      _config = config;
    }

    public void Initialize()
    {
      _signalBus.Subscribe<GameOverTriggeredSignal>(OnGameOver);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<GameOverTriggeredSignal>(OnGameOver);
    }

    private void OnGameOver(GameOverTriggeredSignal _)
    {
      _gameOverCount++;

      int n = _config.InterstitialEveryNGameovers;
      if (n <= 0) return;                 // защита от деления/бесконечного показа
      if (_gameOverCount < n) return;

      _gameOverCount = 0;
      _adService.ShowInterstitial();
    }
  }
}
