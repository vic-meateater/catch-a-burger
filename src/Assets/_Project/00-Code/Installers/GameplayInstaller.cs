using BurgerCatch.Events;
using BurgerCatch.Gameplay.Burger;
using BurgerCatch.Gameplay.Chef;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Input;
using BurgerCatch.Gameplay.Lives;
using BurgerCatch.Gameplay.Order;
using BurgerCatch.Gameplay.Time;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Installers
{
  public sealed class GameplayInstaller : MonoInstaller
  {
    [SerializeField] private ConveyorGeometry _geometry;
    
    public override void InstallBindings()
    {
      Container.BindInterfacesAndSelfTo<GameClock>().AsSingle();
      Container.BindInterfacesAndSelfTo<ChefController>().AsSingle();
      Container.BindInterfacesAndSelfTo<ConveyorSystem>().AsSingle();
      Container.BindInterfacesAndSelfTo<NewInputService>().AsSingle();
      Container.BindInterfacesAndSelfTo<CatchResolver>().AsSingle();
      Container.BindInterfacesAndSelfTo<LivesSystem>().AsSingle();
      
      Container.Bind<BurgerStack>().AsSingle();
      Container.BindInterfacesAndSelfTo<OrderSystem>().AsSingle();
      
      Container.Bind<ConveyorGeometry>().FromInstance(_geometry).AsSingle();

      Signals();
    }

    private void Signals()
    {
      Container.DeclareSignal<IngredientCaughtSignal>().OptionalSubscriber();
      Container.DeclareSignal<IngredientDroppedSignal>().OptionalSubscriber();
      Container.DeclareSignal<ChefMovedSignal>().OptionalSubscriber();
      Container.DeclareSignal<IngredientReachedMouthSignal>().OptionalSubscriber();

      Container.DeclareSignal<LifeLostSignal>().OptionalSubscriber();
      Container.DeclareSignal<LifeGainedSignal>().OptionalSubscriber();
      Container.DeclareSignal<GameOverTriggeredSignal>().OptionalSubscriber();
      
      Container.DeclareSignal<BurgerLayerAddedSignal>().OptionalSubscriber();
      Container.DeclareSignal<BurgerStackClearedSignal>().OptionalSubscriber();
      Container.DeclareSignal<OrderItemMatchedSignal>().OptionalSubscriber();
      Container.DeclareSignal<OrderItemWrongSignal>().OptionalSubscriber();
      Container.DeclareSignal<OrderCompletedSignal>().OptionalSubscriber();
      Container.DeclareSignal<OrderChangedSignal>().OptionalSubscriber();

      Container.BindInterfacesAndSelfTo<DebugEventLogger>().AsSingle();
    }
  }
}
