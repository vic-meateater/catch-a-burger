using BurgerCatch.Gameplay.Conveyor; // Side

namespace BurgerCatch.Events
{
  /// <summary>Повар сменил сторону. Директору нужно для расчёта честности спавна.</summary>
  public sealed class ChefMovedSignal
  {
    public Side Side { get; }
    public ChefMovedSignal(Side side) => Side = side;
  }
}