using BurgerCatch.Data;

namespace BurgerCatch.Events
{
  /// <summary>Буст активирован (эффект включён, таймер пошёл).</summary>
  public sealed class BoostActivatedSignal
  {
    public BoostType Type { get; }
    public BoostActivatedSignal(BoostType type) => Type = type;
  }
}
