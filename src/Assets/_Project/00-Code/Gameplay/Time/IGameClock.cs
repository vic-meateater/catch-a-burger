namespace BurgerCatch.Gameplay.Time
{
  public interface IGameClock
  {
    bool IsRunning { get; }
    float DeltaTime { get; }
    float ElapsedTime { get; }
    
    void Resume();
    void Pause();
    void Restart();
  }
}