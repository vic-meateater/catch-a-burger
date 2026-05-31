using System.Collections.Generic;
using BurgerCatch.Core.Saves;
using BurgerCatch.Data;
using YG;

namespace BurgerCatch.Yandex
{
  public sealed class YandexSaveService : ISaveService
  {
    public PlayerData Data { get; private set; }

    public void Load()
    {
      var s = YG2.saves;

      // Первый запуск: транспорт пустой -> создаём дефолт и сразу сохраняем.
      bool isFresh = string.IsNullOrEmpty(s.bc_selectedSkin)
                     && (s.bc_ownedSkins == null || s.bc_ownedSkins.Count == 0);

      if (isFresh)
      {
        Data = PlayerData.CreateDefault();
        Save(); // зафиксировать дефолт в транспорте
        return;
      }

      Data = new PlayerData
      {
        BestScore = s.bc_bestScore,
        SoftCurrency = s.bc_softCurrency,
        SelectedSkin = s.bc_selectedSkin,
        OwnedSkins = s.bc_ownedSkins ?? new List<string>(),
        NoAds = s.bc_noAds,
        BoostInventory = UnpackBoosts(s.bc_boostTypes, s.bc_boostCounts)
      };
    }

    public void Save()
    {
      var s = YG2.saves;

      s.bc_bestScore = Data.BestScore;
      s.bc_softCurrency = Data.SoftCurrency;
      s.bc_selectedSkin = Data.SelectedSkin;
      s.bc_ownedSkins = Data.OwnedSkins;
      s.bc_noAds = Data.NoAds;

      PackBoosts(Data.BoostInventory, out var types, out var counts);
      s.bc_boostTypes = types;
      s.bc_boostCounts = counts;

      YG2.SaveProgress();
    }

    private static Dictionary<BoostType, int> UnpackBoosts(
      List<int> types, List<int> counts)
    {
      var dict = new Dictionary<BoostType, int>();
      if (types == null || counts == null) return dict;

      int n = types.Count < counts.Count ? types.Count : counts.Count;
      for (int i = 0; i < n; i++)
        dict[(BoostType) types[i]] = counts[i];

      return dict;
    }

    private static void PackBoosts(
      Dictionary<BoostType, int> dict,
      out List<int> types, out List<int> counts)
    {
      types = new List<int>();
      counts = new List<int>();
      if (dict == null) return;

      foreach (var kv in dict)
      {
        types.Add((int) kv.Key);
        counts.Add(kv.Value);
      }
    }
  }
}