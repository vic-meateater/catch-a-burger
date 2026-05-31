using BurgerCatch.Bootstrap;
using BurgerCatch.Core.Flow;
using BurgerCatch.Core.Platform;
using BurgerCatch.Core.Saves;
using BurgerCatch.Events;
using BurgerCatch.Yandex;
using Zenject;

namespace BurgerCatch.Installers
{
  public sealed class ProjectInstaller : MonoInstaller
  {
    public override void InstallBindings()
    {
      Signals();
      Platform();
      Saves();
      Flow();
      GameBootstrap();
    }

    private void Signals()
    {
      SignalBusInstaller.Install(Container);

      Container.DeclareSignal<GameStateChangedSignal>().OptionalSubscriber();
    }
    
    private void Platform()
    {
      Container.Bind<IPlatformService>().To<YandexPlatformService>().AsSingle();
    }

    private void Saves()
    {
      Container.Bind<ISaveService>().To<YandexSaveService>().AsSingle();
    }

    private void Flow()
    {
      Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
      Container.Bind<GameFlowController>().AsSingle();
    }
    
    private void GameBootstrap()
    {
      Container.BindInterfacesAndSelfTo<BootstrapFlow>().AsSingle().NonLazy();
    }
  }
}