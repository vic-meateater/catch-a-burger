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
      Container.BindInterfacesAndSelfTo<ConveyorSystem>().AsSingle();
    }
  }
}