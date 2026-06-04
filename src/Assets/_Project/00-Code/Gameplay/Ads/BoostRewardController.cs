using System;
using BurgerCatch.Core.Ads;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Boost;
using Zenject;

namespace BurgerCatch.Gameplay.Ads
{
  /// <summary>
  /// Rewarded-буст в забеге. Кнопку даст UI (Фаза 4) — она стрельнёт
  /// BoostRewardRequestedSignal с типом буста. Буст активируется ТОЛЬКО при просмотре.
  /// </summary>
  public sealed class BoostRewardController : IInitializable, IDisposable
  {
    private const string RewardId = "boost";

    private readonly SignalBus _signalBus;
    private readonly IAdService _adService;
    private readonly BoostController _boost;

    public BoostRewardController(SignalBus signalBus, IAdService adService, BoostController boost)
    {
      _signalBus = signalBus;
      _adService = adService;
      _boost = boost;
    }

    public void Initialize()
    {
      _signalBus.Subscribe<BoostRewardRequestedSignal>(OnBoostRequested);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<BoostRewardRequestedSignal>(OnBoostRequested);
    }

    private void OnBoostRequested(BoostRewardRequestedSignal s)
    {
      // Тип буста фиксируем в замыкании — onReward активирует именно его.
      var type = s.Type;
      _adService.ShowRewarded(RewardId, () => _boost.Activate(type));
    }
  }
}
