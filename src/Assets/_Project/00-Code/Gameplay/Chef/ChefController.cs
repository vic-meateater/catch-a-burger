using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Gameplay.Chef
{
  /// <summary>
  /// Минимальный повар: хранит сторону, на которой стоит.
  /// Ловля решается по этой стороне. Ввод/доскок/удар — позже.
  /// </summary>
  public sealed class ChefController
  {
    public Side CurrentSide { get; private set; } = Side.Left;

    public void MoveTo(Side side) => CurrentSide = side;

    public void Toggle()
      => CurrentSide = CurrentSide == Side.Left ? Side.Right : Side.Left;
  }
}
