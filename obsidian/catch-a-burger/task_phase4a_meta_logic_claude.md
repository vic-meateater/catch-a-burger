# Фаза 4A: Логика меты (валюта, магазин, скины, лидерборд, инапы) — БЕЗ UI

> Контекст — `CLAUDE.md`. Это ЛОГИКА. UI (Canvas, кнопки, экраны) делает
> разработчик вручную — ты НЕ создаёшь UI. Ты пишешь системы + сигналы, которые
> UI будет дёргать и слушать. Всё data-driven через ScriptableObject — добавление
> скина/товара = новый ассет, без кода. Раздел ГРАНИЦЫ — строго.

---

## Принципы

1. **Data-driven.** Скины и товары описаны в ScriptableObject-каталогах. Системы
   читают каталоги, не хардкодят списки. Новый герой = новый ассет в инспекторе.
2. **Абстракция SDK.** Инапы и лидерборд через Plugin YG — `YG2.*` ТОЛЬКО в классах
   слоя `BurgerCatch.Yandex`. Остальное — через интерфейсы.
3. **UI не трогаешь.** Только системы + сигналы. UI подпишется/вызовет потом.
4. **Сохранения уже есть** (`ISaveService`, `PlayerData`). Расширяешь модель,
   не переписываешь.

---

## Точные API Plugin YG (использовать ИМЕННО эти, спорное — TODO)

```csharp
using YG;

// Лидерборд: отправить счёт
YG2.SetLeaderboard(string technicalName, int score);

// Инап-покупка по id (настраивается в дашборде Яндекса):
YG2.BuyPayments(string id);
// Колбэк успешной покупки — событие:
YG2.onPurchaseSuccess += (string id) => { ... };

// Серверное время (для будущей ежедневки, НЕ в этой задаче):
// YG2.ServerTime();
```

---

## Раздел 1: Расширение PlayerData

`PlayerData` (`BurgerCatch.Data`) УЖЕ есть. Убедись, что в ней есть (добавь, чего нет):
- `int BestScore`
- `int SoftCurrency`
- `string SelectedSkin`
- `List<string> OwnedSkins`
- `Dictionary<BoostType,int> BoostInventory`
- `bool NoAds`

Маппинг в `SavesYG` уже есть — если добавляешь поле, добавь и в транспорт/маппинг
(`YandexSaveService`). НЕ ломай существующий маппинг.

---

## Раздел 2: Каталоги (ScriptableObject) — data-driven

### `SkinDefinition` (SO, `BurgerCatch.Data`)
- `string Id` (напр. "roma", "nika")
- `string DisplayName`
- `Sprite Icon` (для будущего UI)
- `int Price` (в softCurrency; 0 если только за инап или бесплатный)
- `bool IapOnly` (true — только за реальные деньги)
- `string IapId` (id покупки в Яндексе, если IapOnly)
- `[CreateAssetMenu(menuName="BurgerCatch/Skin")]`

### `SkinCatalog` (SO, `BurgerCatch.Data`)
- `List<SkinDefinition> Skins`
- `[CreateAssetMenu(menuName="BurgerCatch/SkinCatalog")]`
- Метод `SkinDefinition GetById(string id)`.

### `BoostDefinition` (SO, `BurgerCatch.Data`)
- `BoostType Type`
- `string DisplayName`
- `Sprite Icon`
- `int Price` (в softCurrency)
- `[CreateAssetMenu(menuName="BurgerCatch/Boost")]`

### `BoostCatalog` (SO) — `List<BoostDefinition>` + `GetByType`.

Разработчик сам создаст ассеты (roma, nika, бусты) и каталоги, перетащит в
инсталлер. Ты пишешь классы + биндинг каталогов через `FromInstance`.

---

## Раздел 3: Валюта

### `ICurrencyService` (`BurgerCatch.Core.Economy`)
```csharp
public interface ICurrencyService
{
    int Balance { get; }
    void Add(int amount);          // заработок (за забег)
    bool TrySpend(int amount);     // покупка; false если не хватает
}
```

### `CurrencyService` (`BurgerCatch.Core.Economy`)
- Зависимости: `ISaveService`, `SignalBus`.
- `Balance` читает из `PlayerData.SoftCurrency`.
- `Add`/`TrySpend` меняют `PlayerData.SoftCurrency`, вызывают `_saveService.Save()`,
  стреляют `CurrencyChangedSignal(int newBalance)`.
- Биндинг в **ProjectInstaller** (валюта живёт всю игру).

### Связь с забегом
- Создать `RunRewardController` (`BurgerCatch.Gameplay.Economy`,
  `IInitializable`/`IDisposable`):
  - Подписан на `GameOverTriggeredSignal`.
  - На game over: взять счёт забега (`ScoringSystem.RunScore`), начислить валюту
    (например, 1:1 или по коэффициенту из конфига — `CurrencyPerScore`), вызвать
    `_currency.Add(...)`. Биндинг в GameplayInstaller.
  - Также обновить рекорд: если `RunScore > PlayerData.BestScore` → обновить и
    сохранить, стрельнуть `BestScoreChangedSignal`.

---

## Раздел 4: Лидерборд

### `ILeaderboardService` (`BurgerCatch.Core.Leaderboard`)
```csharp
public interface ILeaderboardService
{
    void SubmitScore(int score);
}
```
### `YandexLeaderboardService` (`BurgerCatch.Yandex`)
- `SubmitScore` → `YG2.SetLeaderboard("<technicalName>", score)`.
  technicalName — константа/поле (разработчик задаст реальное имя из дашборда;
  пока `"score"` + TODO).
- Биндинг в ProjectInstaller.
- `RunRewardController` на новый рекорд вызывает `_leaderboard.SubmitScore(score)`.

---

## Раздел 5: Магазин (логика)

### `IShopService` (`BurgerCatch.Core.Shop`)
```csharp
public interface IShopService
{
    bool TryBuySkin(string skinId);     // за валюту; false если не хватает/iap-only
    bool TryBuyBoost(BoostType type);   // за валюту
    void SelectSkin(string skinId);     // выбрать купленный скин
    bool IsSkinOwned(string skinId);
}
```
### `ShopService` (`BurgerCatch.Core.Shop`)
- Зависимости: `ISaveService`, `ICurrencyService`, `SkinCatalog`, `BoostCatalog`,
  `SignalBus`.
- `TryBuySkin`: найти в каталоге; если `IapOnly` → false (это через инап, не здесь);
  если хватает валюты → `TrySpend`, добавить в `OwnedSkins`, Save, стрельнуть
  `SkinPurchasedSignal(id)`.
- `TryBuyBoost`: списать валюту, `BoostInventory[type]++`, Save, стрельнуть
  `BoostPurchasedSignal(type)`.
- `SelectSkin`: только если owned → `PlayerData.SelectedSkin = id`, Save, стрельнуть
  `SkinSelectedSignal(id)`.
- Биндинг в ProjectInstaller.

---

## Раздел 6: Инапы (реальные деньги)

### `IIapService` (`BurgerCatch.Core.Iap`)
```csharp
public interface IIapService
{
    void Buy(string iapId);   // запускает покупку
}
```
### `YandexIapService` (`BurgerCatch.Yandex`)
- `Buy` → `YG2.BuyPayments(iapId)`.
- В конструкторе/Initialize подписаться на `YG2.onPurchaseSuccess`, в обработчике
  по id определить, что куплено, и применить:
  - id "noads" → `PlayerData.NoAds = true`, Save, стрельнуть `NoAdsActivatedSignal`.
  - id скина (IapOnly) → добавить в OwnedSkins, Save, `SkinPurchasedSignal`.
- Реализует `IInitializable`/`IDisposable` для подписки. Биндинг в ProjectInstaller.

### Связь no-ads с рекламой
- `IAdService`/`InterstitialController` должны учитывать `PlayerData.NoAds`: если
  true — interstitial НЕ показывать (rewarded по желанию игрока показывать можно —
  это его выбор за награду). Разрешённая правка: в `InterstitialController` перед
  показом проверить `_saveService.Data.NoAds`.

---

## Раздел 7: Сигналы (BurgerCatch.Events)

Создать (sealed, иммутабельные), задекларировать с `.OptionalSubscriber()`:
- `CurrencyChangedSignal { int Balance }`
- `BestScoreChangedSignal { int Score }`
- `SkinPurchasedSignal { string Id }`
- `SkinSelectedSignal { string Id }`
- `BoostPurchasedSignal { BoostType Type }`
- `NoAdsActivatedSignal {}`

---

## ГРАНИЦЫ — что НЕ делать

- **НЕ создавать UI**: ни Canvas, ни кнопки, ни экраны, ни префабы UI, ни
  MonoBehaviour-вью меню/магазина. Только системы + сигналы + SO-классы.
- **НЕ трогать геймплейное ядро**: OrderSystem, BurgerStack, ConveyorSystem,
  ChefController, CatchResolver, HitResolver, SpawnDirector, GameClock, LivesSystem.
- **Разрешённые правки существующего** (ТОЛЬКО эти):
  - `PlayerData` + маппинг в `YandexSaveService` — добавить недостающие поля.
  - `InterstitialController` — учитывать `NoAds`.
  - `ScoringSystem` — если нужно публичное `RunScore` (вероятно уже есть) — не менять
    логику, только геттер при необходимости.
- **НЕ выдумывать** API плагина — данные выше, спорное → TODO.
- **НЕ реализовывать** ежедневную награду / serverTime — это позже.
- **НЕ добавлять** пакеты. UniTask не нужен.

---

## Как проверить (временные логи / ContextMenu, без UI)

1. `CurrencyService.Add(100)` → баланс 100, `CurrencyChangedSignal`, сохранилось
   (перезапуск — баланс на месте).
2. Game over с RunScore=50 → валюта +50 (или ×коэф), рекорд обновился, отправка
   в лидерборд (в редакторе — заглушка/лог).
3. `ShopService.TryBuySkin("nika")` при достатке → списалось, nika в OwnedSkins,
   Save. При нехватке → false.
4. `SelectSkin("nika")` → SelectedSkin="nika", сохранилось.
5. `TryBuyBoost(SlowConveyors)` → валюта списана, инвентарь +1.
6. (Инап и лидерборд по-настоящему — только в WebGL на Яндексе, Фаза 6.)

---

## Файлы (примерно)

Создать:
- Data/SO: `SkinDefinition`, `SkinCatalog`, `BoostDefinition`, `BoostCatalog`.
- Core: `ICurrencyService`+`CurrencyService`, `IShopService`+`ShopService`,
  `ILeaderboardService`, `IIapService`.
- Yandex: `YandexLeaderboardService`, `YandexIapService`.
- Gameplay: `RunRewardController`.
- Events: сигналы из Раздела 7.

Правка: `ProjectInstaller`, `GameplayInstaller`, `PlayerData`, `YandexSaveService`,
`InterstitialController`.

Комментарии — на русском.
