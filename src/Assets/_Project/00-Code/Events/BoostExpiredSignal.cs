using BurgerCatch.Data;

namespace BurgerCatch.Events
{
  /// <summary>Буст истёк (эффект снят).</summary>
  public sealed class BoostExpiredSignal
  {
    public BoostType Type { get; }
    public BoostExpiredSignal(BoostType type) => Type = type;
  }
}
