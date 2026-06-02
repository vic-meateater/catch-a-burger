using System;
using BurgerCatch.Gameplay.Conveyor;

namespace BurgerCatch.Gameplay.Input
{
  public interface IInputService
  {
    event Action<Side> SideTapped;
  }
}