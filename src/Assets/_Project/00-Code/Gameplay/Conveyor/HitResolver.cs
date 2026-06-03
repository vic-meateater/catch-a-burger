using System;
using BurgerCatch.Events;
using Zenject;

namespace BurgerCatch.Gameplay.Conveyor
{
  /// <summary>
  /// Исполняет удар сковородой. На ChefHitSignal бьёт ближайший к устью
  /// ингредиент в зоне досягаемости через ConveyorSystem.TryHit.
  /// При попадании — стреляет IngredientHitSignal. Промах — норма, ничего не делаем.
  /// (Симметрично ловле: повар стреляет сигнал, резолвер исполняет по ленте.)
  /// </summary>
  public sealed class HitResolver : IInitializable, IDisposable
  {
    // Порог зоны досягаемости: бьём только то, что уже близко к устью.
    // Позже уедет в конфиг.
    private const float MinHitProgress = 0.6f;

    private readonly SignalBus _signalBus;
    private readonly ConveyorSystem _conveyor;

    public HitResolver(SignalBus signalBus, ConveyorSystem conveyor)
    {
      _signalBus = signalBus;
      _conveyor = conveyor;
    }

    public void Initialize() => _signalBus.Subscribe<ChefHitSignal>(OnChefHit);
    public void Dispose()    => _signalBus.Unsubscribe<ChefHitSignal>(OnChefHit);

    private void OnChefHit(ChefHitSignal signal)
    {
      // Попал — сообщаем факт. Промах (в зоне никого) — молча, кулдаун уже потрачен у ChefController.
      if (_conveyor.TryHit(signal.Side, MinHitProgress, out var type))
        _signalBus.Fire(new IngredientHitSignal(type, signal.Side));
    }
  }
}
