using BurgerCatch.Events;
using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Time;
using Zenject;

namespace BurgerCatch.Installers
{
  public sealed class GameplayInstaller : MonoInstaller
  {
    public override void InstallBindings()
    {
      Container.BindInterfacesAndSelfTo<GameClock>().AsSingle();
      Container.BindInterfacesAndSelfTo<ChefController>().AsSingle();
      Container.BindInterfacesAndSelfTo<ConveyorSystem>().AsSingle();

      Signals();
    }

    private void Signals()
    {
      Container.DeclareSignal<IngredientCaughtSignal>().OptionalSubscriber();
      Container.DeclareSignal<IngredientDroppedSignal>().OptionalSubscriber();

      Container.BindInterfacesAndSelfTo<DebugEventLogger>().AsSingle();
    }
  }
}
