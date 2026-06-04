# Задача: GameplayConfig (ScriptableObject) + чистое окно при смене заказа

> Контекст — в `CLAUDE.md`. Две части: (1) вынести параметры спавн-директора в
> ScriptableObject; (2) добавить "чистое окно" — пауза спавна при смене заказа.
> Трогаешь ТОЛЬКО `SpawnDirector` и создаёшь новые файлы. Раздел ГРАНИЦЫ — строго.

---

## Часть 1: GameplayConfig

Сейчас в `SpawnDirector` (namespace `BurgerCatch.Gameplay.Spawn`) параметры —
это `private const`. Вынести их в ScriptableObject, чтобы крутить в инспекторе.

### Создать `GameplayConfig` (ScriptableObject)

- Namespace `BurgerCatch.Data`, файл в `00-Code/Data/`.
- `[CreateAssetMenu(menuName = "BurgerCatch/GameplayConfig")]`.
- Поля (`[SerializeField]` с публичными геттерами ИЛИ public-поля — на твой выбор,
  но значения по умолчанию проставить ровно как текущие const):
  - `BaseInterval = 1.2f`
  - `BaseSpeed = 0.2f`
  - `SpeedPerBurger = 0.015f`
  - `IntervalCutPerBurger = 0.03f`
  - `MaxSpeed = 0.6f`
  - `MinInterval = 0.5f`
  - `ForceNeededAfter = 3` (int)
  - `MaxTotalThreats = 6` (int)
  - `MaxThreatsOnFarSide = 1` (int)
  - `NeededChance = 0.3f`
  - `OrderChangeWindow = 1.5f`  (НОВОЕ — длительность чистого окна, Часть 2)

### Подключить в SpawnDirector

- `SpawnDirector` получает `GameplayConfig` через конструктор (Zenject).
- Заменить все обращения к `const` на `_config.<Поле>`.
- УДАЛИТЬ старые `const` из `SpawnDirector` (они переехали в конфиг).
- Логика директора НЕ меняется — только источник чисел: был const, стал конфиг.

### Регистрация в Zenject

`GameplayConfig` — это ассет на сцене/в проекте. Биндить так:
```csharp
[SerializeField] private GameplayConfig _gameplayConfig;
// в InstallBindings:
Container.Bind<GameplayConfig>().FromInstance(_gameplayConfig).AsSingle();
```
(Разработчик сам создаст ассет конфига и перетащит в поле инсталлера —
ты только добавляешь биндинг и поле.)

---

## Часть 2: Чистое окно при смене заказа

При смене заказа директор НЕ спавнит НИЧЕГО в течение `OrderChangeWindow` секунд
(игрок читает новый рецепт). Лента живёт, что уже едет — доигрывается.

### Логика

- `SpawnDirector` уже подписан на `OrderCompletedSignal` (для роста сложности).
  Дополнительно подписаться на **`OrderChangedSignal`** (namespace `BurgerCatch.Events`,
  УЖЕ существует, несёт `IngredientType[] Recipe`).
- На `OrderChangedSignal`: завести таймер окна = `_config.OrderChangeWindow`.
- Пока таймер окна > 0: в `Tick()` уменьшать его на игровое время
  (`_clock.DeltaTime`) и **НЕ спавнить** (ранний `return` до спавн-логики).
- Окно тикает на ИГРОВОМ времени (на паузе замирает, как всё остальное).
- `OrderChangedSignal` летит и при сборке, и при протухании — это и есть единая
  точка передышки, отдельная обработка не нужна.

### Важно

- Окно блокирует ЛЮБОЙ спавн (и нужный, и мусор) — это полная передышка.
- НЕ сбрасывать таймер интервала спавна странно — после окна спавн продолжается
  штатно. Достаточно: пока окно активно — `return` из `Tick` до спавна.

---

## ГРАНИЦЫ — что НЕ делать

- **НЕ трогать** другие системы: `OrderSystem`, `BurgerStack`, `ScoringSystem`,
  `LivesSystem`, `ConveyorSystem`, `ChefController`, `CatchResolver`, `HitResolver`.
- **НЕ выносить** в конфиг параметры цены/жизней — только спавн-директор.
  (Цена/жизни поедут в отдельный EconomyConfig потом, не в этой задаче.)
- **НЕ менять** логику выбора стороны/типа, предохранители, рост сложности —
  только заменить источник чисел (const → конфиг) и добавить окно.
- **НЕ менять** сигнатуры существующих сигналов.
- **НЕ добавлять** пакеты/зависимости.

---

## Как проверить

1. Создать ассет `GameplayConfig` (правый клик в Project → Create → BurgerCatch →
   GameplayConfig), перетащить в поле инсталлера. Запустить — игра ведёт себя
   как раньше (числа те же). Поменять в инспекторе `BaseSpeed` → поведение
   меняется без перекомпиляции. Значит конфиг работает.
2. Собрать бургер или дать протухнуть → ~1.5 сек новый спавн не идёт (лента
   доигрывает старое, новых ингредиентов нет), потом поток возобновляется.
3. Поставить паузу во время окна → окно замирает (тикает на игровом времени).

---

## Файлы

- Создать: `GameplayConfig.cs` в `00-Code/Data/`.
- Правка: `SpawnDirector.cs` (const → конфиг, подписка на OrderChanged, таймер окна).
- Правка: `GameplayInstaller.cs` (поле + биндинг GameplayConfig).

Комментарии — на русском.
