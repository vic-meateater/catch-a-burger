using System;
using BurgerCatch.Gameplay.Conveyor;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace BurgerCatch.Gameplay.Input
{
  public sealed class NewInputService : IInputService, ITickable
  {
    public event Action<Side> SideTapped;

    public void Tick()
    {
      HandlePointer();
      HandleKeyboard();
    }

    private void HandlePointer()
    {
      var pointer = Pointer.current;
      if (pointer == null) return;

      if (!pointer.press.wasPressedThisFrame) return;

      Vector2 pos = pointer.position.ReadValue();
      Side side = pos.x < Screen.width * 0.5f ? Side.Left : Side.Right;
      SideTapped?.Invoke(side);
    }

    private void HandleKeyboard()
    {
      var kb = Keyboard.current;
      if (kb == null) return;
      
      if(kb.leftArrowKey.wasPressedThisFrame)
        SideTapped?.Invoke(Side.Left);
      else if(kb.rightArrowKey.wasPressedThisFrame)
        SideTapped?.Invoke(Side.Right);
    }
  }
}