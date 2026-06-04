namespace BurgerCatch.Core.Economy
{
  /// <summary>Мягкая валюта забега: заработок и траты.</summary>
  public interface ICurrencyService
  {
    int Balance { get; }

    /// <summary>Начислить (заработок за забег).</summary>
    void Add(int amount);

    /// <summary>Списать на покупку. false, если не хватает.</summary>
    bool TrySpend(int amount);
  }
}
