using System;
using BurgerCatch.Core.Ads;
using BurgerCatch.Core.Saves;
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
    private readonly ISaveService _saveService;

    private int _gameOverCount;

    public InterstitialController(
      SignalBus signalBus,
      IAdService adService,
      GameplayConfig config,
      ISaveService saveService)
    {
      _signalBus = signalBus;
      _adService = adService;
      _config = config;
      _saveService = saveService;
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
      // Куплено «убрать рекламу» — interstitial не показываем (rewarded остаётся).
      if (_saveService.Data.NoAds) return;

      _gameOverCount++;

      int n = _config.InterstitialEveryNGameovers;
      if (n <= 0) return;                 // защита от деления/бесконечного показа
      if (_gameOverCount < n) return;

      _gameOverCount = 0;
      _adService.ShowInterstitial();
    }
  }
}
