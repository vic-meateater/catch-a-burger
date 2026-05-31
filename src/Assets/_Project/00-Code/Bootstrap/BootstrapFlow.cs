using BurgerCatch.Core.Flow;
using BurgerCatch.Core.Platform;
using BurgerCatch.Core.Saves;
using BurgerCatch.Events;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Bootstrap
{
  public sealed class BootstrapFlow : IInitializable
  {
    private const string MAIN_MENU_SCENE = "MainMenu";

    private readonly IPlatformService _platform;
    private readonly ISaveService _saves;
    private readonly ISceneLoader _sceneLoader;
    private readonly GameFlowController _flow;

    public BootstrapFlow(
      IPlatformService platform,
      ISaveService saveService,
      ISceneLoader sceneLoader,
      GameFlowController gameFlowController)
    {
      _platform = platform;
      _saves = saveService;
      _sceneLoader = sceneLoader;
      _flow = gameFlowController;
    }

    public void Initialize()
    {
      _platform.WhenReady(OnPlatformReady);
    }

    private void OnPlatformReady()
    {
      _saves.Load();

      Debug.Log($"[Bootstrap] Saves loaded. Best={_saves.Data.BestScore}, " +
                $"Coins={_saves.Data.SoftCurrency}. Loading menu.");

      _sceneLoader.Load(MAIN_MENU_SCENE, () => _flow.SetState(GameState.Menu));
    }
  }
}