using BurgerCatch.Bootstrap;
using BurgerCatch.Core.Ads;
using BurgerCatch.Core.Economy;
using BurgerCatch.Core.Flow;
using BurgerCatch.Core.Iap;
using BurgerCatch.Core.Leaderboard;
using BurgerCatch.Core.Platform;
using BurgerCatch.Core.Saves;
using BurgerCatch.Core.Shop;
using BurgerCatch.Data;
using BurgerCatch.Events;
using BurgerCatch.Yandex;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Installers
{
  public sealed class ProjectInstaller : MonoInstaller
  {
    [SerializeField] private SkinCatalog _skinCatalog;
    [SerializeField] private BoostCatalog _boostCatalog;

    public override void InstallBindings()
    {
      Signals();
      Platform();
      Saves();
      Ads();
      Catalogs();
      Economy();
      Flow();
      GameBootstrap();
    }

    private void Signals()
    {
      SignalBusInstaller.Install(Container);

      Container.DeclareSignal<GameStateChangedSignal>().OptionalSubscriber();

      // Сигналы меты (живут в project scope — их стреляют project-сервисы).
      Container.DeclareSignal<CurrencyChangedSignal>().OptionalSubscriber();
      Container.DeclareSignal<SkinPurchasedSignal>().OptionalSubscriber();
      Container.DeclareSignal<SkinSelectedSignal>().OptionalSubscriber();
      Container.DeclareSignal<BoostPurchasedSignal>().OptionalSubscriber();
      Container.DeclareSignal<NoAdsActivatedSignal>().OptionalSubscriber();
    }
    
    private void Platform()
    {
      Container.Bind<IPlatformService>().To<YandexPlatformService>().AsSingle();
    }

    private void Saves()
    {
      Container.Bind<ISaveService>().To<YandexSaveService>().AsSingle();
    }

    private void Ads()
    {
      // Реклама живёт всю игру (project scope).
      Container.Bind<IAdService>().To<YandexAdService>().AsSingle();
    }

    private void Catalogs()
    {
      // Data-driven каталоги — ассеты из инспектора.
      Container.Bind<SkinCatalog>().FromInstance(_skinCatalog).AsSingle();
      Container.Bind<BoostCatalog>().FromInstance(_boostCatalog).AsSingle();
    }

    private void Economy()
    {
      // Валюта, магазин, лидерборд, инапы — мета, живёт всю игру.
      Container.Bind<ICurrencyService>().To<CurrencyService>().AsSingle();
      Container.Bind<IShopService>().To<ShopService>().AsSingle();
      Container.Bind<ILeaderboardService>().To<YandexLeaderboardService>().AsSingle();
      Container.BindInterfacesAndSelfTo<YandexIapService>().AsSingle();
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