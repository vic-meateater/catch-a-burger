using BurgerCatch.Core.Saves;
using BurgerCatch.Events;
using Zenject;

namespace BurgerCatch.Core.Economy
{
  /// <summary>
  /// Мягкая валюта поверх PlayerData.SoftCurrency. Любое изменение сохраняется
  /// и стреляет CurrencyChangedSignal (на него подпишется UI).
  /// </summary>
  public sealed class CurrencyService : ICurrencyService
  {
    private readonly ISaveService _saveService;
    private readonly SignalBus _signalBus;

    public CurrencyService(ISaveService saveService, SignalBus signalBus)
    {
      _saveService = saveService;
      _signalBus = signalBus;
    }

    public int Balance => _saveService.Data.SoftCurrency;

    public void Add(int amount)
    {
      if (amount <= 0) return;

      _saveService.Data.SoftCurrency += amount;
      _saveService.Save();
      _signalBus.Fire(new CurrencyChangedSignal(Balance));
    }

    public bool TrySpend(int amount)
    {
      if (amount <= 0) return true;          // нечего списывать — успех
      if (Balance < amount) return false;    // не хватает

      _saveService.Data.SoftCurrency -= amount;
      _saveService.Save();
      _signalBus.Fire(new CurrencyChangedSignal(Balance));
      return true;
    }
  }
}
