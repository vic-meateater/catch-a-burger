using BurgerCatch.Core.Flow;
using BurgerCatch.Events;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  public class MenuButton : MonoBehaviour
  {
    [SerializeField] private Button _menuButton;
    private GameFlowController _flow;
    private ISceneLoader _sceneLoader;

    [Inject]
    public void Construct(GameFlowController flow, ISceneLoader sceneLoader)
    {
      _flow = flow;
      _sceneLoader = sceneLoader;
    }
    private void Start()
    {
      _menuButton.onClick.AddListener(OnMenuButtonClicked);
    }

    private void OnMenuButtonClicked()
    {
      _sceneLoader.Load("MainMenu", () => _flow.SetState(GameState.Menu));
    }
  }
}