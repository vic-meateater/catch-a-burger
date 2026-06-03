namespace BurgerCatch.Events
{
  /// <summary>Цена текущего бургера изменилась → UI обновит ценник (Фаза UI).</summary>
  public sealed class BurgerPriceChangedSignal
  {
    public int Price { get; }
    public BurgerPriceChangedSignal(int price) => Price = price;
  }
}
