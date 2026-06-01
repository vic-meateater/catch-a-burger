using Zenject;

namespace BurgerCatch.Gameplay.Time
{
  public sealed class GameClock : IGameClock, ITickable
  {
    public bool IsRunning { get; private set; }
    public float DeltaTime { get; private set; }
    public float ElapsedTime { get; private set; }

    public void Tick()
    {
      if (!IsRunning)
      {
        DeltaTime = 0;
        return;
      }

      DeltaTime = UnityEngine.Time.deltaTime;
      ElapsedTime += DeltaTime;
    }

    public void Resume() => IsRunning = true;

    public void Pause() => IsRunning = false;

    public void Restart()
    {
      ElapsedTime = 0;
      DeltaTime = 0;
    }
  }
}