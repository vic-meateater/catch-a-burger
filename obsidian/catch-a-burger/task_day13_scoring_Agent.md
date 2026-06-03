# Задача: система цены и счёта забега (ScoringSystem)

> Контекст проекта — в `CLAUDE.md` в корне. Прочитай перед началом.
> ИЗОЛИРОВАННАЯ задача. Реализуй ТОЛЬКО описанное ниже.
> НЕ переписывай и НЕ рефактори существующий код. Особое внимание — раздел ГРАНИЦЫ.

---

## Что нужно сделать

Базовая экономика забега (без комбо — комбо будет отдельной задачей позже):

- У текущего собираемого бургера есть цена. База = 100 (целое, НЕ float).
- За каждый грязный слой (поймали не тот ингредиент) цена −10, минимум 0.
- Когда цена дошла до 0 — бургер «протух»: стреляется сигнал `BurgerSpoiledSignal`.
- Когда бургер собран по заказу — текущая цена начисляется в общий счёт забега,
  цена сбрасывается к 100 для следующего бургера.
- Общий счёт забега (`RunScore`) хранится в этой же системе (для рекорда/лидерборда).

---

## Архитектурный контекст (соблюдать строго)

Unity 6 + Zenject (Extenject). Событийная архитектура через **Zenject SignalBus**.
Системы — обычные классы (НЕ MonoBehaviour), биндятся в контейнере, общаются сигналами.

**ВСЯ экономика — в целых числах (int). НИКАКОГО float.** Деньги/очки хранятся
в наименьшей целой единице (база 100 = условный $1.00, отображается делением на 100
только в UI — но UI не в этой задаче).

**Существующие сигналы (УЖЕ есть, namespace `BurgerCatch.Events`) — НЕ создавать
заново, импортировать и подписаться:**

```csharp
// Пойман НЕ тот ингредиент (грязный слой добавлен). УЖЕ существует.
public sealed class OrderItemWrongSignal
{
    public IngredientType Type { get; }   // BurgerCatch.Gameplay.Conveyor
}

// Бургер собран по заказу (пора продать). УЖЕ существует.
public sealed class OrderCompletedSignal { }

// Выдан новый заказ (старый собран ИЛИ протух). УЖЕ существует.
public sealed class OrderChangedSignal
{
    public IngredientType[] Recipe { get; }
}
```

`OrderItemWrongSignal` — твой вход для штрафа. `OrderCompletedSignal` — вход для
начисления в счёт. `OrderChangedSignal` — сигнал, что начался новый бургер
(сбросить цену к базе).

**Паттерн существующих систем (следовать точь-в-точь):**
- класс `sealed`, реализует `IInitializable` и `IDisposable`;
- подписка в `Initialize()`, отписка в `Dispose()`;
- зависимости через конструктор (`SignalBus`);
- сигналы — `sealed`, иммутабельные (данные через конструктор, только геттеры).

---

## Требования к реализации

### 1. Новый сигнал (namespace `BurgerCatch.Events`)

- `BurgerSpoiledSignal` — пустой сигнал-факт. Стреляется, когда цена дошла до 0.
  (На него позже подпишутся OrderSystem и BurgerStack — НЕ в этой задаче, см. ГРАНИЦЫ.)

Опционально для UI (объяви, если просто): `BurgerPriceChangedSignal` с `int Price`
и `RunScoreChangedSignal` с `int Score` — чтобы UI потом показывал цену и счёт.
Если делаешь — `OptionalSubscriber()`.

### 2. Класс `ScoringSystem` (namespace `BurgerCatch.Gameplay.Scoring`)

- `sealed`, реализует `IInitializable`, `IDisposable`.
- Зависимость: `SignalBus` через конструктор.
- Константы (НЕ магические числа в логике):
  - `BasePrice = 100`
  - `DirtyPenalty = 10`
  - `MinPrice = 0`
- Поля/свойства:
  - `int CurrentPrice` — цена текущего бургера (стартует с `BasePrice`).
  - `int RunScore` — общий счёт забега (стартует с 0).
- В `Initialize()`: `CurrentPrice = BasePrice`, `RunScore = 0`, подписаться на
  `OrderItemWrongSignal`, `OrderCompletedSignal`, `OrderChangedSignal`.
- В `Dispose()`: отписаться от всех трёх.

### 3. Логика

**На `OrderItemWrongSignal` (грязный слой):**
- `CurrentPrice -= DirtyPenalty`, но не ниже `MinPrice`.
- (Если делаешь `BurgerPriceChangedSignal` — выстрелить его.)
- Если `CurrentPrice == 0` (стало 0 в этот момент) → выстрелить `BurgerSpoiledSignal`.
  Цену НЕ сбрасывать здесь — сброс произойдёт на `OrderChangedSignal`, когда
  OrderSystem выдаст новый заказ в ответ на протухание (см. примечание ниже).

**На `OrderCompletedSignal` (бургер собран → продажа):**
- `RunScore += CurrentPrice`.
- (Если делаешь `RunScoreChangedSignal` — выстрелить его.)
- Цену НЕ сбрасывать здесь — сброс на `OrderChangedSignal` (OrderSystem выдаёт
  новый заказ после сборки).

**На `OrderChangedSignal` (начался новый бургер — после сборки ИЛИ протухания):**
- `CurrentPrice = BasePrice`.
- (Если делаешь `BurgerPriceChangedSignal` — выстрелить его.)

Почему сброс цены именно на `OrderChangedSignal`: и сборка, и протухание ведут к
новому заказу, который OrderSystem сигналит через `OrderChangedSignal`. Сброс цены
в одной точке (на новый заказ) проще и надёжнее, чем дублировать его в двух местах.

### 4. Защита от повторного протухания

`BurgerSpoiledSignal` должен стрелять РОВНО ОДИН раз за бургер. Если цена уже 0 и
прилетает ещё `OrderItemWrongSignal` — НЕ стрелять `BurgerSpoiledSignal` повторно
(цена и так 0, ничего не изменилось). Достаточно проверки: стрелять spoiled только
когда цена ИМЕННО в этот момент стала 0 (была > 0, стала 0), а не когда она уже 0.

### 5. Регистрация в Zenject

В существующем `GameplayInstaller` добавить ТОЛЬКО:

```csharp
Container.DeclareSignal<BurgerSpoiledSignal>().OptionalSubscriber();
// если делал price/score сигналы — задекларировать их с OptionalSubscriber()
Container.BindInterfacesAndSelfTo<ScoringSystem>().AsSingle();
```

Не трогать остальные биндинги в инсталлере.

---

## ГРАНИЦЫ — что НЕ делать (КРИТИЧНО)

- **НЕ трогать `OrderSystem`.** Это ядро игры, реализовано и проверено. Подписку
  OrderSystem на `BurgerSpoiledSignal` (выдать новый заказ при протухании) добавит
  разработчик ВРУЧНУЮ, отдельно. Твоя задача — только СТРЕЛЬНУТЬ `BurgerSpoiledSignal`.
- **НЕ трогать `BurgerStack`.** Очистку стека при протухании тоже подключит
  разработчик вручную.
- **НЕ реализовывать комбо/множители.** Только базовая цена. Комбо — отдельная задача.
- **НЕ делать UI** (отображение цены/счёта). Только сигналы для будущего UI.
- **НЕ трогать** существующие сигналы, `CatchResolver`, `ConveyorSystem`,
  `ChefController`, `GameClock`, `LivesSystem`.
- **НЕ начислять/списывать** внутриигровую валюту игрока (это другой слой, Фаза 4).
  `RunScore` — это очки ТЕКУЩЕГО забега, не валюта.
- **НЕ добавлять** пакеты/зависимости, не менять настройки проекта.

---

## Как проверить (критерий приёмки)

Временные логи (в `_Sandbox`, подписки на сигналы):
1. Поймал не тот ингредиент → цена падает на 10 (100 → 90 → 80 ...).
2. Цена дошла до 0 → один `BurgerSpoiledSignal`. Дальнейшие грязные слои НЕ плодят
   повторный spoiled.
3. Собрал бургер чисто → `RunScore` += текущая цена.
4. После сборки/протухания (новый заказ) → цена снова 100.

Примечание: полный цикл протухания (стек очистился, новый заказ выдан) заработает
ТОЛЬКО после того, как разработчик вручную подпишет OrderSystem/BurgerStack на
`BurgerSpoiledSignal`. В рамках ЭТОЙ задачи достаточно, что `BurgerSpoiledSignal`
корректно стреляется один раз.

---

## Файлы

- Создать: `BurgerSpoiledSignal.cs` (+ опц. `BurgerPriceChangedSignal.cs`,
  `RunScoreChangedSignal.cs`) в `00-Code/Events/`.
- Создать: `ScoringSystem.cs` в `00-Code/Gameplay/Scoring/`.
- Правка: `GameplayInstaller.cs` — только добавить строки из п. 5.

Комментарии — на русском.
