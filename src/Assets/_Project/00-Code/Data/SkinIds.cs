using System.Collections.Generic;
using System.Linq;

namespace BurgerCatch.Data
{
  public static class SkinIds
  {
    private static readonly Dictionary<string, string> _skins;
    
    static SkinIds()
    {
      _skins = new Dictionary<string, string>
      {
        ["Roma"] = "roma",
        ["Nika"] = "nika"
      };
    }
    
    public const string Roma = "roma";
    public const string Nika = "nika";
    
    public static bool IsValid(string skinId) => _skins.ContainsValue(skinId);
    
    public static string GetDisplayName(string skinId) 
    {
      return _skins.FirstOrDefault(x => x.Value == skinId).Key ?? "Unknown";
    }
    
    public static IEnumerable<string> GetAll() => _skins.Values;
  }
}