namespace BurgerCatch.Core.Ads
{
  /// <summary>
  /// Абстракция над рекламным SDK. Остальной код работает ТОЛЬКО через этот
  /// интерфейс — конкретный SDK (Plugin YG) живёт за ним, наружу не течёт.
  /// </summary>
  public interface IAdService
  {
    /// <summary>Показать interstitial. onClosed — после закрытия (для resume геймплея).</summary>
    void ShowInterstitial(System.Action onClosed = null);

    /// <summary>
    /// Показать rewarded. onReward — ТОЛЬКО при успешном полном просмотре.
    /// onClosed — всегда после закрытия (награда или нет) — для resume.
    /// </summary>
    void ShowRewarded(string rewardId, System.Action onReward, System.Action onClosed = null);
  }
}
