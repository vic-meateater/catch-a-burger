using System;

namespace BurgerCatch.Core.Flow
{
  public interface ISceneLoader
  {
    void Load(string sceneName, Action onLoaded = null);
  }
}