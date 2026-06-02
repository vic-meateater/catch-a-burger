using System.Collections.Generic;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Gameplay.Conveyor
{
  public sealed class ConveyorSystem : ITickable
  {
    private readonly IGameClock _clock;
    private readonly SignalBus _signalBus;
    private readonly List<Ingredient> _active = new List<Ingredient>();
    private float _speed = 0.2f;

    public float Speed
    {
      get => _speed;
      set => _speed = UnityEngine.Mathf.Max(0f, value);
    }

    public ConveyorSystem(IGameClock clock, SignalBus signalBus)
    {
      _clock = clock;
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
        ing.Progress += _speed * dt;

        if (ing.Progress < 1f) continue;

        // Лента сообщает ТОЛЬКО факт: ингредиент достиг устья.
        // Поймано/упало решает другая система — лента про повара не знает.
        _signalBus.Fire(new IngredientReachedMouthSignal(ing.Type, ing.Side));
        _active.RemoveAt(i);
      }
    }
  }
}