using BurgerCatch.Events;
using BurgerCatch.Gameplay.Chef;
using Zenject;

namespace BurgerCatch.Gameplay.Conveyor
{
  /// <summary>
  /// Решает исход, когда ингредиент достиг устья:
  /// повар на этой стороне -> поймано; иначе -> упало.
  /// Лента про повара НЕ знает — связь только здесь.
  /// </summary>
  public sealed class CatchResolver : IInitializable, System.IDisposable
  {
    private readonly SignalBus _signalBus;
    private readonly ChefController _chef;

    public CatchResolver(SignalBus signalBus, ChefController chef)
    {
      _signalBus = signalBus;
      _chef = chef;
    }

    public void Initialize() => _signalBus.Subscribe<IngredientReachedMouthSignal>(OnReached);
    public void Dispose()    => _signalBus.Unsubscribe<IngredientReachedMouthSignal>(OnReached);

    private void OnReached(IngredientReachedMouthSignal s)
    {
      if (_chef.CurrentSide == s.Side)
        _signalBus.Fire(new IngredientCaughtSignal(s.Type, s.Side));
      else
        _signalBus.Fire(new IngredientDroppedSignal(s.Type, s.Side));
    }
  }
}