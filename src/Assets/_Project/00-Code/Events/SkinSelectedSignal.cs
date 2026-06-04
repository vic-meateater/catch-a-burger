namespace BurgerCatch.Events
{
  /// <summary>Выбран активный скин.</summary>
  public sealed class SkinSelectedSignal
  {
    public string Id { get; }
    public SkinSelectedSignal(string id) => Id = id;
  }
}
