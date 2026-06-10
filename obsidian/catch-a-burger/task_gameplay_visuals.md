# Задача: визуал геймплея — едущие ингредиенты + башня бургера

> Контекст — `CLAUDE.md`. Вся ЛОГИКА готова: ингредиенты едут (`ConveyorSystem`),
> стек копится (`BurgerStack`). Сейчас их рисуют ЛЕСА (`TestIngredientRun` —
> серые квадраты). Задача: заменить лесную отрисовку ингредиентов на настоящие
> СПРАЙТОВЫЕ вью, читающие ту же логику. Повар ПОКА остаётся квадратом (его скин —
> отдельная будущая задача, не трогать). Раздел ГРАНИЦЫ строго.

---

## Что есть (использовать, НЕ менять)

- `ConveyorSystem` (namespace `BurgerCatch.Gameplay.Conveyor`):
  - `IReadOnlyList<Ingredient> Active` — едущие ингредиенты.
  - `Ingredient { Side Side; IngredientType Type; float Progress; }` (0=спавн, 1=устье).
- `ConveyorGeometry`: `Vector3 PositionOf(Side side, float progress)` — мировая
  позиция ингредиента на ленте.
- `IngredientCatalog` (namespace `BurgerCatch.Data`):
  `IngredientDefinition GetByType(IngredientType)`, у `IngredientDefinition` есть
  `Sprite Icon`.
- `BurgerStack` (namespace `BurgerCatch.Gameplay.Burger`):
  - `IReadOnlyList<BurgerLayer> Layers`; `BurgerLayer { IngredientType Type; bool IsDirty; }`
- Сигналы (namespace `BurgerCatch.Events`, УЖЕ есть):
  - `BurgerLayerAddedSignal { IngredientType Type; bool IsDirty; int TotalLayers; }`
  - `BurgerStackClearedSignal {}`

---

## Вью 1: IngredientView (едущие ингредиенты спрайтами)

MonoBehaviour, namespace `BurgerCatch.UI` (или `BurgerCatch.Gameplay.View`), на
сцене Gameplay. Заменяет отрисовку ингредиентов из лесов.

Зависимости (`[Inject]`): `ConveyorSystem`, `ConveyorGeometry`, `IngredientCatalog`.

`[SerializeField]`:
- `SpriteRenderer _ingredientPrefab` — префаб спрайта ингредиента (разработчик
  сделает: GameObject с SpriteRenderer).
- `Vector3 _scale = Vector3.one` (опц., размер).

Поведение (как леса, но спрайтами):
- В `Update()` (или LateUpdate) синхронизировать спрайты с `ConveyorSystem.Active`:
  - для нового ингредиента в Active, которого ещё нет — создать спрайт из префаба,
    спрайт-картинку взять `_catalog.GetByType(ing.Type).Icon`;
  - позиция = `_geometry.PositionOf(ing.Side, ing.Progress)`;
  - ингредиент исчез из Active (пойман/упал/отбит) — уничтожить его спрайт.
- Держать словарь `Dictionary<Ingredient, SpriteRenderer>` (как в лесах).
- НЕ трогать логику движения — только отражать `Active`.

(По сути это чистовой аналог `SyncIngredients()` из лесов, но через каталог-спрайты
вместо CreatePrimitive/цвета.)

---

## Вью 2: BurgerStackView (растущая башня бургера)

MonoBehaviour, на сцене Gameplay. Рисует собираемый бургер слоями снизу вверх.

Зависимости (`[Inject]`): `SignalBus`, `IngredientCatalog`.

`[SerializeField]`:
- `Transform _stackAnchor` — точка-якорь, ОТ которой растёт башня (низ бургера).
  Разработчик поставит на сцене.
- `SpriteRenderer _layerPrefab` — префаб слоя (SpriteRenderer).
- `float _layerHeight = 0.3f` — фиксированная высота слоя (разработчик подгонит).
- `float _dirtyDarken = 0.5f` — множитель яркости для грязных слоёв (темнее).

Поведение:
- На `BurgerLayerAddedSignal`:
  - создать спрайт слоя из префаба, родитель — `_stackAnchor` (или просто в мире от
    якоря);
  - картинка = `_catalog.GetByType(s.Type).Icon`;
  - позиция: якорь + вверх на `(индекс_слоя) * _layerHeight`. Индекс слоя =
    (число уже нарисованных слоёв). Снизу вверх: первый слой у якоря, каждый
    следующий выше.
  - если `s.IsDirty` — затемнить спрайт: `color = Color.white * _dirtyDarken`
    (или умножить базовый цвет на `_dirtyDarken`, alpha=1). Грязный = тот же
    ингредиент, но темнее.
- На `BurgerStackClearedSignal`: уничтожить все нарисованные слои (башня обнулилась —
  продажа/протухание).
- Хранить список созданных слоёв (`List<SpriteRenderer>`) для очистки.
- Подписки в OnEnable/после Construct, отписки в OnDisable. Guard
  `if (_signalBus == null) return;` в OnDisable.

ВАЖНО: высота слоя ФИКСИРОВАННАЯ (`_layerHeight`), не по высоте спрайта.
Слои строго снизу вверх по порядку добавления (грязные тоже занимают слой).

---

## Удаление лесов

- В `TestIngredientRun` УБРАТЬ отрисовку ИНГРЕДИЕНТОВ (создание/синхронизацию
  кубиков ингредиентов — `SyncIngredients`, словарь `_cubes`, `ColorOf`).
- ОСТАВИТЬ в лесах: квадрат-ПОВАРА (`SyncChef`, создание чефа), управление паузой
  (пробел) и `_clock.Resume()` — повар пока квадрат, его визуал отдельная задача.
- Если после вырезания ингредиентов леса становятся почти пустыми — это нормально,
  оставь рабочий минимум (повар + пауза + resume).

---

## Регистрация

Вью — MonoBehaviour на сцене Gameplay, инжектятся через SceneContext (как
существующие вью). Биндить в инсталлере НЕ обязательно (если на сцене под
SceneContext). Если нужно — добавь комментарий разработчику.

`IngredientCatalog` уже биндится (добавлен в прошлой задаче). Если нет —
добавь биндинг через FromInstance в GameplayInstaller (поле + Bind).

---

## ГРАНИЦЫ

- **НЕ трогать** логику: `ConveyorSystem`, `BurgerStack`, `OrderSystem`,
  `SpawnDirector`, `CatchResolver`, `HitResolver`, `ScoringSystem`, `GameClock`.
  Только ЧИТАТЬ их состояние/сигналы.
- **НЕ трогать** повара (он остаётся квадратом в лесах — отдельная задача).
- **НЕ создавать** префабы/спрайты/якоря — разработчик в инспекторе. Только
  C#-классы вью с [SerializeField].
- **НЕ менять** сигнатуры сигналов, существующие вью UI (HUD/Menu/Shop).
- **НЕ добавлять** пакеты.
- Спрайт ингредиента берётся ТОЛЬКО из `IngredientCatalog` (не хардкодить).

---

## Как проверить (после ручной сборки)

1. Создать префаб ингредиента (SpriteRenderer) и слоя бургера, заполнить
   IngredientCatalog спрайтами (Icon на каждый тип), поставить _stackAnchor.
2. Запуск: по лентам едут СПРАЙТЫ ингредиентов (не квадраты), позиция верная.
3. Ловишь нужный → в башне у якоря появляется слой снизу вверх; грязный (поймал не
   то) — темнее.
4. Собрал/протух → башня очистилась.
5. Повар — пока квадрат (норма).

---

## Файлы

Создать: `IngredientView.cs`, `BurgerStackView.cs` (в 00-Code/UI/ или
00-Code/Gameplay/View/).
Правка: `TestIngredientRun.cs` (убрать отрисовку ингредиентов, оставить повара).
Возможно `GameplayInstaller.cs` (биндинг IngredientCatalog, если не забиндан).

Комментарии — на русском.
