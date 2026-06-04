# Фаза 3: Монетизация (реклама + бусты)

> Контекст — `CLAUDE.md`. Это БОЛЬШАЯ фаза, выполняй по разделам по порядку.
> Монетизация СОБЫТИЙНАЯ (не таймерная). Вся реклама привязана к игровым
> событиям. Реклама — через Plugin YG (PluginYG2). Точные API даны ниже —
> используй ИМЕННО их, не выдумывай имена методов.

---

## Принципы (критично)

1. **Событийная реклама, НЕ таймерная.** Никаких "показать рекламу через N секунд
   игры". Только на события: game over, кнопка continue, кнопка буста.
2. **Абстракция над SDK.** Plugin YG (`YG2.*`) упоминается ТОЛЬКО в классе
   `YandexAdService` (namespace `BurgerCatch.Yandex`). Остальной код работает через
   интерфейс `IAdService`. Это правило проекта — SDK не течёт наружу.
3. **Rewarded-награда выдаётся ТОЛЬКО по колбэку успешного просмотра**, не по
   запуску показа. Иначе игрок получит награду без просмотра ИЛИ посмотрит без
   награды.
4. **Геймплей паузится на время рекламы.** Перед показом — пауза, после закрытия —
   возобновление через отсчёт (если отсчёт уже есть; если нет — просто resume).
5. Бусты — расходники, берутся ПЕРЕД забегом. В забеге — один rewarded-буст.

---

## Точные API Plugin YG (PluginYG2) — использовать ИМЕННО эти

```csharp
using YG;

// Полноэкранная (interstitial) реклама:
YG2.InterstitialAdvShow();

// Rewarded реклама с колбэком награды:
YG2.RewardedAdvShow(string id, System.Action onReward);
// onReward вызывается ТОЛЬКО при успешном полном просмотре.

// События пауз геймплея для платформы (вызывать вокруг рекламы):
YG2.GameplayStop();   // перед показом рекламы
YG2.GameplayStart();  // после закрытия рекламы

// Проверка готовности SDK (уже используется в проекте):
YG2.isSDKEnabled;
```

Если какого-то метода в твоей версии плагина нет под этим именем — НЕ выдумывай.
Оставь `// TODO: проверить точное имя в доке плагина` и используй наиболее
вероятное. Разработчик сверит.

---

## Раздел 1: Абстракция рекламы

### `IAdService` (namespace `BurgerCatch.Core.Ads`)

```csharp
public interface IAdService
{
    // Показать interstitial. onClosed — после закрытия (для resume геймплея).
    void ShowInterstitial(System.Action onClosed = null);

    // Показать rewarded. onReward — ТОЛЬКО при успешном просмотре.
    // onClosed — всегда после закрытия (награда или нет) — для resume.
    void ShowRewarded(string rewardId, System.Action onReward, System.Action onClosed = null);
}
```

### `YandexAdService` (namespace `BurgerCatch.Yandex`)

- Реализует `IAdService`. Единственное место с `YG2.*` по рекламе.
- `ShowInterstitial`: вызвать `YG2.GameplayStop()`, `YG2.InterstitialAdvShow()`,
  затем `onClosed?.Invoke()` и `YG2.GameplayStart()`.
  (Если у плагина есть колбэк закрытия interstitial — повесить onClosed на него.
  Если нет — вызвать onClosed сразу после показа. TODO-пометку оставь.)
- `ShowRewarded`: `YG2.GameplayStop()`, затем
  `YG2.RewardedAdvShow(rewardId, () => onReward?.Invoke())`, после закрытия —
  `onClosed?.Invoke()` и `YG2.GameplayStart()`.

Биндинг в **ProjectInstaller** (реклама живёт всю игру):
```csharp
Container.Bind<IAdService>().To<YandexAdService>().AsSingle();
```

---

## Раздел 2: Interstitial на game over (с частотным лимитом)

Создать `InterstitialController` (namespace `BurgerCatch.Gameplay.Ads`,
`IInitializable`/`IDisposable`):

- Зависимости: `SignalBus`, `IAdService`.
- Подписан на `GameOverTriggeredSignal` (УЖЕ существует).
- Частотный лимит: interstitial показывается НЕ на каждый game over, а на каждый
  N-й (N в конфиг, по умолчанию 2). Считать game over'ы, показывать когда счётчик
  достиг N, сбрасывать.
- На показ: `_adService.ShowInterstitial()`.

НЕ делать: показ по таймеру. Только событие game over + счётчик.

---

## Раздел 3: Rewarded "продолжить" (+1 жизнь)

Это для будущего экрана game over (кнопка "продолжить за рекламу"). Сейчас —
система + сигналы, без UI-кнопки (UI в Фазе 4).

- Сигнал `ContinueRequestedSignal` (пустой) — будущая кнопка его стрельнёт.
- Создать `ContinueController` (`BurgerCatch.Gameplay.Ads`, `IInitializable`/`IDisposable`):
  - Зависимости: `SignalBus`, `IAdService`, `LivesSystem`.
  - Подписан на `ContinueRequestedSignal`.
  - На сигнал: `_adService.ShowRewarded("continue", onReward, onClosed)`.
    - `onReward` → восстановить жизнь (`LivesSystem` нужен публичный метод
      восстановления — если нет, добавь `LivesSystem.Revive()` который ставит
      жизни = 1 и сбрасывает флаг game over; см. ГРАНИЦЫ — это разрешённая правка
      LivesSystem).
    - `onClosed` → возобновить забег (стрельнуть `RunResumedSignal`, пустой сигнал,
      на него позже подпишется GameFlow).

ВАЖНО: жизнь восстанавливается ТОЛЬКО в onReward (просмотрел), не в onClosed.

---

## Раздел 4: Rewarded-буст в забеге

- Сигнал `BoostRewardRequestedSignal` с `BoostType Type` (будущая кнопка).
- Создать `BoostRewardController` (`BurgerCatch.Gameplay.Ads`):
  - Зависимости: `SignalBus`, `IAdService`, `BoostController` (см. Раздел 5).
  - На `BoostRewardRequestedSignal`: `ShowRewarded("boost", onReward, onClosed)`.
  - `onReward` → активировать буст (`BoostController.Activate(type)`).

---

## Раздел 5: Система бустов

Три буста (расходники). `BoostType` enum УЖЕ существует
(`BurgerCatch.Data`): `SlowConveyors=0`, `DoublePrice=1`, `OnlyNeeded=2`.

### `BoostController` (`BurgerCatch.Gameplay.Boost`, `IInitializable`/`IDisposable`/`ITickable`)

- Зависимости: `SignalBus`, `IGameClock`, `ConveyorSystem`, `OrderSystem`(?),
  `ScoringSystem`(?), `GameplayConfig`.
- Активные бусты с таймерами (на ИГРОВОМ времени `_clock.DeltaTime`).
- `Activate(BoostType type)`:
  - стрельнуть `BoostActivatedSignal(type)`;
  - запустить эффект + таймер длительности (длительность в конфиг, см. ниже).
- В `Tick()`: уменьшать таймеры активных бустов; по истечении — снять эффект,
  стрельнуть `BoostExpiredSignal(type)`.

### Эффекты бустов

- **SlowConveyors**: `ConveyorSystem.Speed *= slowFactor` (напр. 0.5) на время;
  по истечении — вернуть прежнюю скорость. ВАЖНО: запомнить скорость ДО буста и
  восстановить её (а не выставить базовую — скорость могла вырасти по сложности).
- **DoublePrice**: множитель цены ×2 на время. ScoringSystem должен учитывать
  множитель. Реализуй через флаг/множитель в ScoringSystem (см. ГРАНИЦЫ —
  разрешённая правка: добавить `ScoringSystem.PriceMultiplier`).
- **OnlyNeeded**: на время директор спавнит только нужный по заказу (без мусора).
  Реализуй через флаг, который читает SpawnDirector (разрешённая правка
  SpawnDirector: добавить `bool OnlyNeededMode`).

### Сигналы бустов (`BurgerCatch.Events`)

- `BoostActivatedSignal { BoostType Type }`
- `BoostExpiredSignal { BoostType Type }`

### Параметры в GameplayConfig (добавить)

- `SlowFactor = 0.5f`
- `SlowDuration = 10f`
- `DoublePriceDuration = 10f`
- `OnlyNeededDuration = 10f`

---

## Раздел 6: Регистрация

В **ProjectInstaller**: `IAdService`.

В **GameplayInstaller**: задекларировать новые сигналы
(`ContinueRequestedSignal`, `RunResumedSignal`, `BoostRewardRequestedSignal`,
`BoostActivatedSignal`, `BoostExpiredSignal` — с `.OptionalSubscriber()` где нет
гарантированного слушателя), забиндить контроллеры:
```csharp
Container.BindInterfacesAndSelfTo<InterstitialController>().AsSingle();
Container.BindInterfacesAndSelfTo<ContinueController>().AsSingle();
Container.BindInterfacesAndSelfTo<BoostRewardController>().AsSingle();
Container.BindInterfacesAndSelfTo<BoostController>().AsSingle();
```

---

## ГРАНИЦЫ — что НЕ делать

- **НЕ создавать UI** (кнопки continue/boost, экран game over). Только системы и
  сигналы. UI — Фаза 4. Кнопки потом стрельнут готовые сигналы.
- **НЕ трогать** ядро: `OrderSystem` (логику заказа), `BurgerStack`, `CatchResolver`,
  `HitResolver`, `ConveyorSystem` (кроме чтения/установки `Speed` для буста),
  `ChefController`, `GameClock`.
- **Разрешённые точечные правки существующего** (ТОЛЬКО эти):
  - `LivesSystem`: добавить публичный `Revive()` (жизни=1, сброс флага game over).
  - `ScoringSystem`: добавить `PriceMultiplier` (по умолчанию 1, ×2 при бусте),
    учитывать его при начислении цены за бургер.
  - `SpawnDirector`: добавить `bool OnlyNeededMode` — если true, спавнить только
    нужный (пропускать мусор в ChooseType).
  - `GameplayConfig`: добавить параметры бустов и `InterstitialEveryNGameovers`.
- **НЕ менять** существующие сигнатуры сигналов.
- **НЕ выдумывать** API плагина — использовать данные выше, спорное помечать TODO.
- **НЕ добавлять** пакеты без необходимости. UniTask НЕ добавлять (колбэков достаточно).
- **НЕ реализовывать** инап-покупки (no-ads, скины) — это Фаза 4. Только реклама и бусты.

---

## Как проверить (временные логи в _Sandbox)

1. Game over → каждый 2-й раз показывается interstitial (или лог попытки показа,
   если в редакторе реклама не идёт — Plugin YG в редакторе обычно логирует заглушку).
2. Стрельнуть `ContinueRequestedSignal` вручную → попытка rewarded; в onReward
   жизнь восстановилась.
3. Стрельнуть `BoostRewardRequestedSignal(SlowConveyors)` → скорость лент упала на
   время, через SlowDuration вернулась к прежней.
4. `Activate(DoublePrice)` → собранный бургер даёт ×2 очков на время.
5. `Activate(OnlyNeeded)` → на время по лентам едет только нужный.
6. Все таймеры бустов замирают на паузе игры.

---

## Файлы (примерно)

Создать:
- `00-Code/Core/Ads/IAdService.cs`
- `00-Code/Yandex/YandexAdService.cs`
- `00-Code/Gameplay/Ads/InterstitialController.cs`
- `00-Code/Gameplay/Ads/ContinueController.cs`
- `00-Code/Gameplay/Ads/BoostRewardController.cs`
- `00-Code/Gameplay/Boost/BoostController.cs`
- сигналы в `00-Code/Events/`: `ContinueRequestedSignal`, `RunResumedSignal`,
  `BoostRewardRequestedSignal`, `BoostActivatedSignal`, `BoostExpiredSignal`.

Правка:
- `ProjectInstaller.cs`, `GameplayInstaller.cs`
- `LivesSystem.cs` (Revive), `ScoringSystem.cs` (PriceMultiplier),
  `SpawnDirector.cs` (OnlyNeededMode), `GameplayConfig.cs` (параметры).

Комментарии — на русском.
