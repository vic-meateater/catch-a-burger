using System.Collections.Generic;

namespace YG
{
  public partial class SavesYG
  {
    public int bc_bestScore;
    public int bc_softCurrency;
    public string bc_selectedSkin;
    public List<string> bc_ownedSkins;

    // Бусты плоско: два параллельных списка (тип как int + количество).
    // Словарь не кладём — сериализация плагина дружит с простыми списками.
    public List<int> bc_boostTypes;
    public List<int> bc_boostCounts;

    public bool bc_noAds;
  }
}