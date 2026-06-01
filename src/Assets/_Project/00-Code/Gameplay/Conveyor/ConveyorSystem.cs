using System.Collections.Generic;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Conveyor
{
  public sealed class ConveyorSystem : ITickable
  {
    private const float BASE_SPEED = 0.2f;

    private readonly IGameClock _clock;
    private readonly ChefController _chef;
    private readonly SignalBus _signalBus;
    private readonly List<Ingredient> _active = new List<Ingredient>();

    public ConveyorSystem(IGameClock clock, ChefController chef, SignalBus signalBus)
    {
      _clock = clock;
      _chef = chef;
      _signalBus = signalBus;
    }

    public IReadOnlyList<Ingredient> Active => _active;

    public void Spawn(Side side, IngredientType type)
    {
      _active.Add(new Ingredient { Side = side, Type = type, Progress = 0f });
    }

    public void Tick()
    {
      float dt = _clock.DeltaTime;

      if (dt <= 0f) return;

      for (int i = _active.Count - 1; i >= 0; i--)
      {
        var ing = _active[i];
        ing.Progress += BASE_SPEED * dt;

        if (ing.Progress < 1f) continue;

        // Доехал до устья: повар на этой стороне — ловит, иначе срыв на пол.
        if (_chef.CurrentSide == ing.Side)
          _signalBus.Fire(new IngredientCaughtSignal(ing.Type, ing.Side));
        else
          _signalBus.Fire(new IngredientDroppedSignal(ing.Type, ing.Side));

        _active.RemoveAt(i);
      }
    }
  }
}
