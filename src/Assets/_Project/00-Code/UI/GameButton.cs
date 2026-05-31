using System;
using BurgerCatch.Core.Flow;
using BurgerCatch.Events;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  public class GameButton : MonoBehaviour
  {
    [SerializeField] private Button _gameButton;
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
      _gameButton.onClick.AddListener(OnGameButtonClicked);
    }

    private void OnGameButtonClicked()
    {
      _sceneLoader.Load("Game", () => _flow.SetState(GameState.Ready));
    }
  }
}