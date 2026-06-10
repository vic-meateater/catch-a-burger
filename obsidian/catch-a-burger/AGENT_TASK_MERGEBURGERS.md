# ТЗ для агента: Mergeburgers — финальная версия (Unity, WebGL, Яндекс.Игры)

## Роль агента

Ты — Unity-разработчик. Твоя задача: написать **весь C# код и структуру проекта** для casual merge-игры Mergeburgers. Человек после тебя делает ТОЛЬКО ручную работу в Unity Editor: расставляет префабы по сценам, перетаскивает ссылки в Inspector, подключает спрайты/звуки, создаёт ScriptableObject-ассеты по твоим инструкциям.

Для каждого MonoBehaviour и SO ты ОБЯЗАН написать раздел «Ручная настройка»: на какой объект вешать, какие поля чем заполнять, какие префабы создавать и с какой иерархией.

## Технологический стек (зафиксировано, не менять)

- Unity 6.4.9f1, 2D Renderer URP
- Extenject (Zenject) + OptionalExtras/Signals (SignalBus)
- DOTween Free
- PluginYG2 (Yandex SDK, max-games.ru/plugin-yg)
- TextMeshPro
- ЗАПРЕЩЕНО: Odin Inspector, VContainer, Service Locator, любые новые плагины без явного согласования

## Конвенции кода (обязательны)

- Namespaces: `Mergeburgers.Core / Gameplay / Meta / UI / Yandex / Data / Events / Tutorial / Audio`
- Папки: `Assets/_Project/00-Code, 01-Data, 02-Art, 03-Audio, 04-Prefabs, 05-Scenes`
- Все классы `sealed` по умолчанию
- DI: `[Inject]` метод `Construct(...)` на MonoBehaviour; `[Inject]` конструктор на plain-классах (ОБЯЗАТЕЛЬНО явный, иначе Zenject берёт пустой и зависимости null)
- Подписки на SignalBus: если подписка условная или может отсутствовать — отписка через `TryUnsubscribe`
- Сигналы делятся на information (UI: BankCoinsChanged, EnergyChanged, SessionCoinsChanged) и action (side-effects: GameOver, CashedOut). Autosave подписан ТОЛЬКО на action-сигналы
- Любое время (idle, энергия, оффлайн) — через UNIX seconds (`DateTimeOffset.UtcNow.ToUnixTimeSeconds()`), НИКОГДА `Time.deltaTime` (ломается при паузе/фоне вкладки)
- Debug.Log в горячих путях — под `#if UNITY_EDITOR`. ContextMenu-методы — под `#if UNITY_EDITOR`
- Все балансовые параметры — в ScriptableObject или const-конфигах, не магическими числами в логике
- JsonUtility для сейва: только List<string>, без Dictionary/DateTime

## Сцены и DI-контексты

3 сцены: `Bootstrap` → `MainMenu` → `Game`. ProjectContext.prefab в `Assets/Resources/` с GameInstaller (глобальные сервисы). SceneContext на каждой сцене; GameSceneInstaller только для сцено-специфичных биндингов (Board и связка).

В ProjectContext (глобально): SaveManager, EconomyManager, EnergyManager, IdleIncomeCalculator, RecipeUnlockTracker, TutorialController, InterstitialDispatcher, AutosaveDispatcher, AudioService + AudioSignalListener, IngredientDatabase, RecipeDatabase, AudioConfig, IAdService/ICloudSaveService (Editor-фейки + WebGL-реализации через PluginYG2), AppLifecycleHandler (MonoBehaviour на ProjectContext: OnApplicationFocus/Pause/Quit → ForceSave).

## Механика (ЗАФИКСИРОВАНА, отклонения запрещены)

**Доска:** 5×5, стартовое заполнение ~50%, спавн 1 плитки за результативный свайп.

**Ингредиенты:** 6 базовых (Bun=1, Patty=2, Cheese=3, Lettuce=4, Tomato=5, Sauce=6). Бургеры: Hamburger=100, Cheeseburger=101, Veggieburger=102, BigMac=103, KingBurger=200. Extension-методы IsBaseIngredient (1–99) / IsBurger (100+).

**Состояние клетки:** struct `IngredientCell { Type, Level (0–3), LifeRemaining, BurgerSellPrice }`. Уровни отображаются звёздами: 0 — ничего, 1 — ★, 2 — ★★, 3 — ★★★.

**Merge-правила (свайп двигает все плитки как в 2048):**
- Группа = подряд идущие в линии плитки ОДНОГО ТИПА (бургеры в группы не входят, идут поодиночке и НЕ сливаются)
- 2+ одинаковых одного уровня → 1 плитка уровня+1 (cap = 3; на cap'е просто схлопывание без апа)
- Смешанные уровни в группе → 1 плитка максимального уровня БЕЗ апа (это by design)

**Рецепты (линии 3–6, горизонталь/вертикаль, чтение в обе стороны, авто-срабатывание, длинные проверяются раньше коротких):**
- Hamburger: Bun+Patty+Bun, base 10
- Cheeseburger: Bun+Patty+Cheese+Bun, base 25
- Veggieburger: Bun+Lettuce+Tomato+Bun, base 30
- BigMac: Bun+Patty+Cheese+Lettuce+Bun, base 100
- KingBurger: Bun+Patty+Cheese+Lettuce+Tomato, base 200, MinLevelRequired=3 (хотя бы одна плитка ★★★ в линии)
- Множитель цены: `1 + сумма звёзд в линии`. FinalPrice = base × multiplier, сохраняется в BurgerSellPrice клетки бургера

**Жизнь бургера:** 3 хода, счётчик в углу плитки, по истечении авто-продажа (BurgerSoldSignal с FinalPrice). Бургеры не мержатся между собой.

**Pipeline HandleSwipe (порядок критичен для анимаций):**
1. MergeResolver.Resolve → Operations (Move/Merge с from/to)
2. await BoardAnimator.PlayMoveOperations (DOTween, каждой движущейся плитке SetAsLastSibling() для z-order)
3. _state = NewState; Fire MergeOccurredSignal если был merge
4. ResetTilePositions() + RedrawGrid() — плитки на местах, показывают актуальное состояние (бургеры с life=1 ещё видны)
5. expiring = BurgerLifecycle.FindExpiringBurgers(_state)
6. await Task.WhenAll(PlayBurgerSold на этих плитках) — scale→0 + fade, в OnComplete вернуть scale=1/alpha=1
7. _state = BurgerLifecycle.ApplyTick(_state) — декремент, продажа (Fire BurgerSoldSignal), очистка клеток
8. matchResult = RecipeMatcher.FindAndApply(_state) (многопроходный, лимит 8)
9. RedrawGrid(); Fire BurgerCreatedSignal на каждый матч + await PlayBurgerCreated (scale-вспышка)
10. TileSpawner.SpawnOne + RedrawGrid
11. GameOverChecker.IsGameOver → Fire GameOverSignal
Флаг _isAnimating блокирует свайпы во время пайплайна; _isGameOver блокирует после конца.

**Спавн:** Bun 30%, Patty 30%, Cheese 12%, Lettuce 12%, Tomato 12%, Sauce 4%. Всегда Level 0.

**Tile (визуал):** корневой Image = серая полупрозрачная плашка сетки (не меняется кодом), дочерний `_iconImage` = спрайт ингредиента (enabled=false на пустой), отдельные TMP для буквы-fallback и для уровня/жизни. CanvasGroup на префабе для fade-анимаций.

## Экономика (двухпотоковая)

- `sessionCoins` — заработок текущей Game-сессии (BurgerSoldSignal → AddSessionCoins). HUD на Game показывает session
- `coins` (банк) — общая касса. HUD на MainMenu показывает bank
- `EconomyManager.CashOut()` переносит session→bank, Fire CashedOutSignal (action). Вызов: MainMenuController.Start и кнопка «Забрать награду» в GameOverPopup
- Idle-доход идёт напрямую в bank

## Idle-доход

- Разблокировка рецепта — по ПЕРВОЙ ПРОДАЖЕ (BurgerSoldSignal), не по сборке. RecipeUnlockTracker пишет в save.unlockedRecipes (DisplayName)
- Ставки coins/sec: Hamburger 1, Cheeseburger 3, Veggieburger 3, BigMac 10, KingBurger 50. Cap оффлайна: 8 часов
- IdleIncomeTicker — MonoBehaviour ТОЛЬКО на сцене MainMenu, тики по реальному времени (unix), при Start применяет offline earnings от save.lastSessionEndTime, при OnDestroy обновляет lastSessionEndTime

## Энергия

- Max 5, регенерация 1/300 сек, ленивый пересчёт по unix-времени в getter'е Current (EnergyManager в ProjectContext)
- Кнопка «Играть» → TrySpend(1), иначе не пускает. Rewarded ad → +1
- EnergyHud на MainMenu: «⚡ N/5» + таймер mm:ss до следующей единицы, «ПОЛНО» при максимуме, кнопка «+1 за рекламу» (interactable = !IsFull)

## Реклама (event-driven, НИКОГДА по таймеру)

- InterstitialDispatcher: каждый 7-й проданный бургер, cooldown 120 сек между показами; при cooldown счётчик НЕ сбрасывается
- Rewarded: (а) GameOverPopup «+5 плиток» → Board.ClearRandomTilesAndContinue(5), сброс _isGameOver, popup закрывается, sessionCoins сохраняются; (б) Energy «+1»
- IAdService: EditorAdService (Task.Delay(500) → Success), WebGLAdService через PluginYG2

## Сохранения

- GameSave: version, coins, sessionCoins, energy, energyLastFullTime, lastSessionEndTime, unlockedRecipes (List<string>), ownedUpgrades, highScore, sessionsCount, tutorialPassed
- ICloudSaveService: EditorCloudSave (PlayerPrefs), WebGL через PluginYG2 (SavesYG partial с полем gameSaveJson)
- AutosaveDispatcher: подписки на GameOverSignal + CashedOutSignal, throttle 5 сек, ForceSave из AppLifecycleHandler (focus lost / pause / quit)
- TutorialController.MarkComplete форсит SaveAsync немедленно

## Туториал (FTUE)

- States: Inactive, SwipeAny, MergeAny, BurgerFirst, Complete, AlreadyPassed
- TutorialController — plain class в ProjectContext. Initialize подписывается всегда; состояние решается ЛЕНИВО через публичный EnsureStateInitialized() (вызывает TutorialHud.Start), потому что Zenject Initialize() срабатывает ДО загрузки сейва
- Шаги: любой свайп → любой merge → первый BurgerCreated → Complete (popup «Поздравляем» + AddSessionCoins(50))
- В MarkComplete после Fire(Complete) сразу State = AlreadyPassed (иначе popup повторяется при перезаходе на Game)
- Кнопка «Пропустить» — Skip() без бонуса
- TutorialHud: 3 отдельных TMP-надписи (по одной на шаг) с компонентом локализации YG2 (ключи tutorial_swipe / tutorial_merge / tutorial_burger_first), переключение SetActive; switch ApplyState обязан иметь cases AlreadyPassed/Inactive/default скрывающие всё

## Локализация

YG2 keys (ru/en/tr) на всех TMP с текстом: туториал, GameOver popup, Recipe Book, кнопки. В коде текст НЕ хардкодится — только SetActive нужных надписей. Числа/имена рецептов не переводятся.

## UI

- Canvas Scaler на всех сценах: Scale With Screen Size, 720×1280, Match 0.5. Все элементы с правильными anchor presets (HUD — углы, попапы — stretch, кнопки — фиксированный размер с center). Проверка на aspect 9:16, 16:9, 1:1 — ничего не вылетает и не перекрывается (требование модерации Яндекса)
- MainMenu: фон бургерной (Image stretch) + ShelfImage (отдельный спрайт полки, middle-stretch) + SlotsContainer ДОЧЕРНИЙ к ShelfImage (HLG; разблокированные бургеры — иконки, остальные — прозрачные слоты), CoinsHud (Bank), EnergyHud, кнопка «Играть», IdleRateLabel
- Game: Board (middle-center, фикс. размер), CoinsHud (Session), кнопка «← В Меню» (CashOut + LoadScene), кнопка «📖 Рецепты», GameOverPopup, TutorialHud, FloatingTextSpawner («+N» над плиткой при продаже, DOTween вверх+fade, контейнер = BoardContainer для совпадения координат), RecipeBookPopup
- RecipeBookPopup: открытие по кнопке, backdrop-клик закрывает, Populate при КАЖДОМ открытии; строка = иконки ингредиентов с «+», «=», результат; разблокирован → иконка+цена (+пометка «★★★» у King), заблокирован → иконка результата скрыта, цена «???», подпись «Не открыто» / «Требуется ★★★». Ручное позиционирование иконок курсором (без вложенных HLG)
- GameOverPopup: «Заработано: N», кнопки «Забрать награду» (CashOut → MainMenu) и «+5 плиток за рекламу» (rewarded). При фейле рекламы popup остаётся
- CoinsHud: enum Mode (Bank/Session), подписка на соответствующий сигнал, DOTween-прокрутка значения

## Аудио (Яндекс-специфично!)

Стандартный AudioSource в WebGL вызывает шторку «click to enable audio» → бан модерации. Решение: Web Audio API через .jslib (файлы WebAudioMusic.jslib, WebAudioSound.jslib, Vibration.jslib кладутся в Assets/Plugins/WebGL/ — они УЖЕ ЕСТЬ у заказчика, написать только C#-обёртки с точными именами extern: VibrateJS(int) и др.).
- AudioService: `#if UNITY_WEBGL && !UNITY_EDITOR` → WebAudioSound/Music (грузит .ogg из StreamingAssets/sfx и /music по clip.name), иначе → два AudioSource на DontDestroyOnLoad-объекте
- Звуки: swipe (играть с множителем 0.3 громкости, иначе глушит merge), merge, BurgerCreatedClips[4] (рандомный), burger_sold, button_click. Файлы дублируются: AudioClip-ассет в _Project/03-Audio (для Editor/Inspector) + .ogg с тем же именем в StreamingAssets
- AudioSignalListener (IInitializable): Swipe→Light vibration, Merge→Medium, BurgerCreated→Strong, BurgerSold→звук
- ButtonClickSound — компонент на каждую кнопку (RequireComponent(Button))
- SceneMusicPlayer на каждой сцене (Menu/Game music)

## WebGL / билд

- Compression Gzip + Decompression Fallback, Canvas 720×1280, билд < 30 МБ
- `#if !UNITY_EDITOR`-код проверять переключением Build Target (ошибки невидимы в Editor)

## Definition of Done для агента

1. Компилируется без ошибок под Editor и WebGL Build Target
2. Каждый файл — в правильной папке и namespace
3. Для каждого MonoBehaviour/SO — раздел «Ручная настройка» (объект, иерархия префаба, поля Inspector)
4. Список всех ScriptableObject-ассетов с точными значениями полей (11 IngredientData, 5 RecipeData, RecipeDatabase, IngredientDatabase, AudioConfig)
5. Список DeclareSignal для GameInstaller (MergeOccurredSignal — с .OptionalSubscriber())
6. Smoke-чеклист: новый сейв → FTUE → первый бургер → продажа → cashout → idle → энергия → game over → rewarded continue → перезапуск (туториал не повторяется, popup не выскакивает)

## Что агент НЕ делает (остаётся человеку)

- Расстановка объектов по сценам, перетаскивание ссылок в Inspector
- Импорт спрайтов/звуков, создание SO-ассетов по инструкции
- Настройка PluginYG2 в проекте, Яндекс Консоль
- WebGL-билд и сабмит
