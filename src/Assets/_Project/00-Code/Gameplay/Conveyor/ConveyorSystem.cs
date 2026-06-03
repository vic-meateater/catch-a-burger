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
    /// <summary>
    /// Попытка отбить ингредиент на стороне side.
    /// Бьёт БЛИЖАЙШИЙ к устью (макс. Progress) в зоне досягаемости
    /// (Progress >= minProgress). Если такого нет — промах (false).
    /// Возвращает true и тип сбитого через out, если попал.
    /// </summary>
    public bool TryHit(Side side, float minProgress, out IngredientType hitType)
    {
      int bestIndex = -1;
      float bestProgress = -1f;

      for (int i = 0; i < _active.Count; i++)
      {
        var ing = _active[i];
        if (ing.Side != side) continue;
        if (ing.Progress < minProgress) continue;   // вне зоны досягаемости
        if (ing.Progress > bestProgress)             // ближайший к устью
        {
          bestProgress = ing.Progress;
          bestIndex = i;
        }
      }

      if (bestIndex < 0)
      {
        hitType = default;
        return false; // промах: в зоне никого
      }

      hitType = _active[bestIndex].Type;
      _active.RemoveAt(bestIndex);   // отбитый исчезает (не пойман, не упал)
      return true;
    }
  }
}