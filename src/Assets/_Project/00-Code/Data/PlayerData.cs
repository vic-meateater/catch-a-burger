using System.Collections.Generic;

namespace BurgerCatch.Data
{
  public sealed class PlayerData
  {
    public int BestScore;
    public int SoftCurrency;

    /// <summary>Id выбранного героя-скина (напр. "roma", "nika").</summary>
    public string SelectedSkin;

    /// <summary>Id всех купленных/доступных скинов.</summary>
    public List<string> OwnedSkins;

    /// <summary>Запас бустов-расходников: тип -> количество.</summary>
    public Dictionary<BoostType, int> BoostInventory;

    /// <summary>Куплено «убрать рекламу».</summary>
    public bool NoAds;

    /// <summary>Дефолт для нового игрока (первый запуск).</summary>
    public static PlayerData CreateDefault()
    {
      return new PlayerData
      {
        BestScore = 0,
        SoftCurrency = 0,
        SelectedSkin = SkinIds.Roma,            // герой по умолчанию
        OwnedSkins = new List<string> { SkinIds.Roma, SkinIds.Nika }, // оба стартовых
        BoostInventory = new Dictionary<BoostType, int>(),
        NoAds = false
      };
    }
  }
}