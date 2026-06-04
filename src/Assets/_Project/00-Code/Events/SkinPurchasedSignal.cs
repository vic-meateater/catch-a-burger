namespace BurgerCatch.Events
{
  /// <summary>Скин куплен (за валюту или инап).</summary>
  public sealed class SkinPurchasedSignal
  {
    public string Id { get; }
    public SkinPurchasedSignal(string id) => Id = id;
  }
}
