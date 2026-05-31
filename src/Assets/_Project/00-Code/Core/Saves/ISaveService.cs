using BurgerCatch.Data;

namespace BurgerCatch.Core.Saves
{
  public interface ISaveService
  {
    PlayerData Data { get; }

    /// <summary>Прочитать из транспорта в модель. Вызывать после готовности SDK.</summary>
    void Load();

    /// <summary>Записать модель в транспорт и попросить плагин сохранить.</summary>
    void Save();
  }
}