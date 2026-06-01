using System.Collections.Generic;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Conveyor
{
  public sealed class ConveyorSystem : ITickable
  {
    private const float BASE_SPEED = 0.2f;
    
    private readonly IGameClock _clock;
    private readonly List<Ingredient> _active = new List<Ingredient>();

    public ConveyorSystem(IGameClock clock)
    {
      _clock = clock;
    }

    public IReadOnlyList<Ingredient> Active => _active;

    public void Spawn(Side side)
    {
      _active.Add(new Ingredient { Side = side, Progress = 0f });
    }

    public void Tick()
    {
      float dt = _clock.DeltaTime;
      
      if(dt<=0f) return;

      for (int i = _active.Count - 1; i >= 0; i--)
      {
        var ing = _active[i];
        ing.Progress += BASE_SPEED * dt;
        
        if (ing.Progress >= 1f)
        {
          _active.RemoveAt(i);
        }
      }
    }
  }
}