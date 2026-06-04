namespace BurgerCatch.Core.Iap
{
  /// <summary>Инап-покупки за реальные деньги.</summary>
  public interface IIapService
  {
    /// <summary>Запустить покупку по id (настроен в дашборде платформы).</summary>
    void Buy(string iapId);
  }
}
