using BurgerCatch.Data;

namespace BurgerCatch.Events
{
  /// <summary>Запрос активировать буст за просмотр rewarded. Стрельнёт кнопка буста (Фаза 4).</summary>
  public sealed class BoostRewardRequestedSignal
  {
    public BoostType Type { get; }
    public BoostRewardRequestedSignal(BoostType type) => Type = type;
  }
}
