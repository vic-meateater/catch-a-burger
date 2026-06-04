namespace BurgerCatch.Events
{
  /// <summary>Баланс мягкой валюты изменился.</summary>
  public sealed class CurrencyChangedSignal
  {
    public int Balance { get; }
    public CurrencyChangedSignal(int balance) => Balance = balance;
  }
}
