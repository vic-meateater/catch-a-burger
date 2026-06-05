# Задача: конфиги ингредиентов и рецептов (data-driven рецепты)

> Контекст — `CLAUDE.md`. Сейчас рецепт ЗАХАРДКОЖЕН в `OrderSystem` (static-массив
> Bun→Patty→Cheese→Bun). Надо: вынести ингредиенты и рецепты в ScriptableObject,
> `OrderSystem` берёт СЛУЧАЙНЫЙ рецепт из каталога. Вариант А: enum `IngredientType`
> ОСТАЁТСЯ главным идентификатором, SO лишь вешает на тип данные (спрайт/имя).
> Это рефактор ядра — раздел ГРАНИЦЫ соблюдать строго.

---

## Принцип (вариант А)

- `IngredientType` (enum, `BurgerCatch.Gameplay.Conveyor`) — ОСТАЁТСЯ. Не убирать,
  не заменять на строки. Везде в коде ингредиент по-прежнему `IngredientType`.
- `IngredientDefinition` (SO) — привязывает к типу визуал/имя (для арта/HUD позже).
- `RecipeDefinition` (SO) — список `IngredientType` (один рецепт = один бургер).
- `RecipeCatalog` (SO) — список рецептов; `OrderSystem` берёт случайный.
- Разработчик САМ наполнит ассеты рецептов в инспекторе. Агент делает классы +
  переключает OrderSystem на каталог.

---

## Часть 1: SO-классы (namespace BurgerCatch.Data, папка 00-Code/Data/)

### IngredientDefinition
- `IngredientType Type`
- `string DisplayName`
- `Sprite Icon` (для HUD/арта; может быть пустым пока)
- `[CreateAssetMenu(menuName="BurgerCatch/Ingredient")]`

### IngredientCatalog
- `List<IngredientDefinition> Ingredients`
- `[CreateAssetMenu(menuName="BurgerCatch/IngredientCatalog")]`
- Метод `IngredientDefinition GetByType(IngredientType type)` (вернуть по типу,
  null если нет).

### RecipeDefinition
- `string DisplayName` (напр. "Чизбургер" — для отладки/будущего UI)
- `List<IngredientType> Sequence` (последовательность ингредиентов по порядку)
- `[CreateAssetMenu(menuName="BurgerCatch/Recipe")]`
- Удобный геттер `IngredientType[] ToArray()` или просто отдавать Sequence.

### RecipeCatalog
- `List<RecipeDefinition> Recipes`
- `[CreateAssetMenu(menuName="BurgerCatch/RecipeCatalog")]`
- Метод `RecipeDefinition GetRandom()` — случайный рецепт из списка.
  Если список пуст — вернуть null (OrderSystem обработает / залогирует).

---

## Часть 2: переключить OrderSystem на каталог

`OrderSystem` (namespace `BurgerCatch.Gameplay.Order`) СЕЙЧАС:
```csharp
private static readonly IngredientType[] Recipe = { Bun, Patty, Cheese, Bun };
```
НАДО:
- Убрать static-хардкод `Recipe`.
- Добавить зависимость `RecipeCatalog` через конструктор (Zenject).
- Хранить ТЕКУЩИЙ рецепт как поле: `private IngredientType[] _recipe;`
- В `StartNewOrder()`: взять `_recipe = _recipeCatalog.GetRandom().Sequence.ToArray()`
  (с защитой от null/пустого каталога — если null, залогировать ошибку и оставить
  прошлый/заглушку, НЕ крашить).
- Все обращения к `Recipe` заменить на `_recipe`:
  - `Current => _recipe[_index]`
  - `CurrentRecipe => _recipe`
  - `_index >= _recipe.Length`
- `CurrentIndex` остаётся как есть.
- ВАЖНО: НЕ менять логику OnCaught/CompleteOrder/протухания/сигналов. Меняется
  ТОЛЬКО источник массива рецепта (хардкод → случайный из каталога) и то, что
  рецепт теперь поле, а не static. Порядок сигналов и сдвиг указателя — НЕ трогать
  (там уже выверенная логика: _index++ ДО сигнала, завершение через CompleteOrder).

ВАЖНО про сигнал: `OrderChangedSignal` несёт `IngredientType[] Recipe` — теперь
передавать туда `_recipe` (актуальный случайный), а не статику.

---

## Часть 3: регистрация в Zenject

`OrderSystem` забинден в GameplayInstaller. Добавить биндинг каталогов:
```csharp
[SerializeField] private RecipeCatalog _recipeCatalog;
[SerializeField] private IngredientCatalog _ingredientCatalog;
// в InstallBindings:
Container.Bind<RecipeCatalog>().FromInstance(_recipeCatalog).AsSingle();
Container.Bind<IngredientCatalog>().FromInstance(_ingredientCatalog).AsSingle();
```
(Разработчик создаст ассеты каталогов, наполнит рецептами и перетащит в поля
инсталлера. Ты только добавляешь поля + биндинг + зависимость в OrderSystem.)

IngredientCatalog биндим тоже (пригодится HUD/арту для спрайтов по типу), даже
если OrderSystem его пока не использует.

---

## ГРАНИЦЫ

- **НЕ убирать/не менять** enum `IngredientType` (вариант А — enum остаётся).
- **НЕ менять** логику OrderSystem кроме источника рецепта (хардкод → каталог,
  static → поле). OnCaught, CompleteOrder, OnBurgerSpoiled, порядок сигналов,
  сдвиг указателя — НЕ трогать.
- **НЕ трогать** ConveyorSystem, SpawnDirector, CatchResolver, HitResolver,
  BurgerStack, ScoringSystem, LivesSystem, UI.
  (SpawnDirector читает `OrderSystem.Current` — это не ломается, Current теперь
  из _recipe. SpawnDirector НЕ трогать.)
- **НЕ менять** сигнатуры сигналов.
- **НЕ наполнять** ассеты рецептов данными — это разработчик. Только классы.
- **НЕ добавлять** пакеты.

---

## Как проверить

1. Создать ассеты: несколько `RecipeDefinition` (разные последовательности),
   собрать их в `RecipeCatalog`; создать `IngredientDefinition` на каждый тип,
   собрать в `IngredientCatalog`. Перетащить каталоги в инсталлер.
2. Запустить: каждый новый заказ (после сборки/протухания) — СЛУЧАЙНЫЙ рецепт из
   каталога (HUD показывает разные бургеры).
3. Логика сборки/грязных слоёв/протухания работает как раньше, на любом рецепте.
4. Пустой каталог → ошибка в логе, без краша.

---

## Файлы

Создать: `IngredientDefinition.cs`, `IngredientCatalog.cs`, `RecipeDefinition.cs`,
`RecipeCatalog.cs` (все в 00-Code/Data/).
Правка: `OrderSystem.cs` (источник рецепта), `GameplayInstaller.cs` (поля+биндинг).

Комментарии — на русском.
