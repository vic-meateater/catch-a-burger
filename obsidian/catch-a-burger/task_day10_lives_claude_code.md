# Задача: система жизней забега (LivesSystem)

> Контекст проекта — в файле `CLAUDE.md` в корне. Прочитай его перед началом.
> Это ИЗОЛИРОВАННАЯ задача. Реализуй ТОЛЬКО то, что описано ниже.
> Ничего из уже существующего кода НЕ переписывай и НЕ рефакторь.

---

## Что нужно сделать

Система жизней для одного забега аркады. Игрок начинает с 3 жизнями.
Когда ингредиент срывается с ленты на пол — теряется жизнь. Когда жизни
кончились — забег окончен (стреляется сигнал game over).

Заложить метод восстановления жизни с потолком (понадобится позже для
пикапа-сердца), но НЕ вызывать его ниоткуда сейчас.

---

## Архитектурный контекст (соблюдать строго)

Проект на **Unity 6 + Zenject (Extenject)**. Событийная архитектура через
**Zenject SignalBus**. Системы — НЕ MonoBehaviour, это обычные классы,
биндятся в Zenject-контейнере и общаются сигналами.

**Существующие сигналы (УЖЕ есть в проекте, namespace `BurgerCatch.Events`) —
НЕ создавать заново, импортировать и использовать:**

```csharp
// Ингредиент сорвался с устья ленты на пол. УЖЕ существует.
public sealed class IngredientDroppedSignal
{
    public IngredientType Type { get; }   // BurgerCatch.Gameplay.Conveyor
    public Side Side { get; }             // BurgerCatch.Gameplay.Conveyor
    public IngredientDroppedSignal(IngredientType type, Side side) { ... }
}
```

`IngredientDroppedSignal` стреляется существующей системой `CatchResolver`,
когда повар не успел и ингредиент упал. Это твой ВХОД — система жизней
подписывается на него.

**Паттерн существующих систем (следовать ему точь-в-точь):**
- класс `sealed`, реализует `IInitializable` и `IDisposable` (Zenject);
- подписка на сигналы в `Initialize()`, отписка в `Dispose()`;
- зависимости через конструктор (`SignalBus` инжектится);
- сигналы — `sealed`, иммутабельные (данные через конструктор, только геттеры).

---

## Требования к реализации

### 1. Новые сигналы (namespace `BurgerCatch.Events`)

- `LifeLostSignal` — несёт `int Remaining` (сколько жизней осталось). Для будущего UI.
- `LifeGainedSignal` — несёт `int Current` (текущее число жизней). Для пикапа (Day 14).
- `GameOverTriggeredSignal` — пустой сигнал-факт. Поворотная точка забега.

Все три — `sealed`, иммутабельные.

### 2. Класс `LivesSystem` (namespace `BurgerCatch.Gameplay.Lives`)

- `sealed`, реализует `IInitializable`, `IDisposable`.
- Зависимость: `SignalBus` через конструктор.
- Старт жизней = 3, потолок = 5. Это БАЛАНС-ПАРАМЕТРЫ: вынести в `private const`
  (`StartLives`, `MaxLives`), НЕ магические числа в логике.
- Публичное свойство `int Current { get; }`.
- В `Initialize()`: выставить `Current = StartLives`, подписаться на
  `IngredientDroppedSignal`.
- В `Dispose()`: отписаться.
- На `IngredientDroppedSignal`: уменьшить жизнь на 1, выстрелить `LifeLostSignal`
  с остатком. Если жизни дошли до 0 — выставить 0, выстрелить
  `GameOverTriggeredSignal`.
- Публичный метод `Gain()`: восстановить 1 жизнь, но НЕ выше `MaxLives`,
  выстрелить `LifeGainedSignal`. **Ниоткуда не вызывается сейчас** — задел на будущее.

### 3. КРИТИЧНО — защита от двойного game over

После того как жизни кончились, на лентах ещё едут ингредиенты, и они продолжат
доезжать и стрелять `IngredientDroppedSignal`. Без защиты счётчик уйдёт в минус,
а `LifeLostSignal` и `GameOverTriggeredSignal` стрельнут несколько раз.

Требование: ввести флаг `_isOver`. После game over дальнейшие падения
игнорируются. `GameOverTriggeredSignal` стреляется РОВНО ОДИН раз за забег.
`Gain()` после game over тоже ничего не делает.

### 4. Регистрация в Zenject

В существующем `GameplayInstaller` (namespace `BurgerCatch.Installers`) добавить:

```csharp
Container.DeclareSignal<LifeLostSignal>().OptionalSubscriber();
Container.DeclareSignal<LifeGainedSignal>().OptionalSubscriber();
Container.DeclareSignal<GameOverTriggeredSignal>().OptionalSubscriber();
Container.BindInterfacesAndSelfTo<LivesSystem>().AsSingle();
```

`OptionalSubscriber()` — потому что боевых слушателей (UI, реклама) ещё нет.
Добавить ТОЛЬКО эти строки, не трогая остальные биндинги в инсталлере.

---

## ГРАНИЦЫ — что НЕ делать

- **НЕ останавливать игру по game over.** `GameClock`, ленты, спавн продолжают
  идти. Связка «game over → стоп → экран результата» — отдельная будущая задача.
  Здесь только стрельнуть `GameOverTriggeredSignal`.
- **НЕ создавать UI** (сердечки, экраны). Это другая фаза.
- **НЕ трогать** `ConveyorSystem`, `CatchResolver`, `ChefController`, `GameClock`,
  `ConveyorGeometry`, существующие сигналы. Только читать `IngredientDroppedSignal`.
- **НЕ добавлять** перезапуск забега, рестарт, очки, заказ — это другие задачи.
- **НЕ реализовывать** пикап-сердце. Только метод `Gain()` как задел.
- **НЕ менять** Active Input Handling, настройки проекта, зависимости.
- **НЕ добавлять** новые пакеты/библиотеки.

---

## Как проверить (критерий приёмки)

Временный тест (в существующих лесах `_Sandbox`, подписки на сигналы):
1. Дать трём ингредиентам упасть (повар не на той стороне).
2. Ожидаемо в консоли: `LifeLost remaining 2` → `1` → `0` + `GameOver`.
3. После game over дальнейшие падения НЕ должны плодить `LifeLost` или повторный
   `GameOver` (проверка флага `_isOver`).

Если `Initialize()` не вызывается (game over с первого падения) — проверить,
что класс реализует `IInitializable` и забинден через `BindInterfacesAndSelfTo`.

---

## Файлы, которые нужно создать

- `LifeLostSignal.cs`, `LifeGainedSignal.cs`, `GameOverTriggeredSignal.cs`
  в папке сигналов (`00-Code/Events/`).
- `LivesSystem.cs` в `00-Code/Gameplay/Lives/`.
- Правка существующего `GameplayInstaller.cs` (только добавить строки из п. 4).

Комментарии в коде — на русском.
