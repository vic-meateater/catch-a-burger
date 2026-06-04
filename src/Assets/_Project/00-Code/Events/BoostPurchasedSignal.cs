using BurgerCatch.Data;

namespace BurgerCatch.Events
{
  /// <summary>Буст-расходник куплен (в инвентарь +1).</summary>
  public sealed class BoostPurchasedSignal
  {
    public BoostType Type { get; }
    public BoostPurchasedSignal(BoostType type) => Type = type;
  }
}
