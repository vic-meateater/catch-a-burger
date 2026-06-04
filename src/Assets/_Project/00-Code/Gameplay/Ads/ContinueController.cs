using System;
using BurgerCatch.Core.Ads;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Lives;
using Zenject;

namespace BurgerCatch.Gameplay.Ads
{
  /// <summary>
  /// Rewarded "продолжить" (+1 жизнь). Кнопку даст UI (Фаза 4) — она стрельнёт
  /// ContinueRequestedSignal. Жизнь восстанавливается ТОЛЬКО при успешном просмотре.
  /// </summary>
  public sealed class ContinueController : IInitializable, IDisposable
  {
    private const string RewardId = "continue";

    private readonly SignalBus _signalBus;
    private readonly IAdService _adService;
    private readonly LivesSystem _lives;

    public ContinueController(SignalBus signalBus, IAdService adService, LivesSystem lives)
    {
      _signalBus = signalBus;
      _adService = adService;
      _lives = lives;
    }

    public void Initialize()
    {
      _signalBus.Subscribe<ContinueRequestedSignal>(OnContinueRequested);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<ContinueRequestedSignal>(OnContinueRequested);
    }

    private void OnContinueRequested(ContinueRequestedSignal _)
    {
      _adService.ShowRewarded(RewardId, OnReward, OnClosed);
    }

    // Просмотрел до конца → вернуть жизнь.
    private void OnReward()
    {
      _lives.Revive();
    }

    // Закрытие (с наградой или без) → возобновить забег.
    private void OnClosed()
    {
      _signalBus.Fire(new RunResumedSignal());
    }
  }
}
