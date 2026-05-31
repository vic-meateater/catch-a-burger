using System;
using UnityEngine.SceneManagement;

namespace BurgerCatch.Core.Flow
{
  public sealed class SceneLoader : ISceneLoader
  {
    public void Load(string sceneName, Action onLoaded = null)
    {
      SceneManager.LoadScene(sceneName);
      onLoaded?.Invoke();
    }
  }
}