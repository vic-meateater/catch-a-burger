# Фаза 4B: UI-вью (HUD, Game Over, Меню, Магазин)

> Контекст — `CLAUDE.md` + логика из Фазы 4A уже готова (сервисы, сигналы).
> Ты пишешь UI-вью (MonoBehaviour-классы) КАК ТОНКИЙ СЛОЙ. Разработчик потом
> расставит ссылки на UI-элементы в [SerializeField] в инспекторе и соберёт Canvas.
> Код — с подробными комментариями (для каждого поля/метода). Раздел ГРАНИЦЫ строго.

---

## ГЛАВНЫЙ ПРИНЦИП (нарушение = переделка)

UI НИЧЕГО НЕ РЕШАЕТ. Вью только:
- ПОКАЗЫВАЕТ: подписался на сигнал → обновил Text/Image;
- ДЁРГАЕТ: нажали кнопку → вызвал метод сервиса.

Вся логика (валюта, покупки, заказ, жизни) УЖЕ в сервисах 4A. UI её НЕ дублирует.
Кнопка "купить" НЕ списывает валюту — зовёт `IShopService.TryBuySkin()`.
Текст баланса НЕ считает — слушает `CurrencyChangedSignal`.

---

## Общие правила для всех вью

- MonoBehaviour, namespace `BurgerCatch.UI`, папка `00-Code/UI/`.
- Ссылки на UI-элементы (Text/TMP_Text, Button, Image, GameObject) — через
  `[SerializeField] private ...` (разработчик привяжет в инспекторе). Используй
  `TMPro.TMP_Text` для текста (проект на TextMeshPro), `UnityEngine.UI.Button`,
  `UnityEngine.UI.Image`.
- Зависимости (сервисы, SignalBus) — через Zenject `[Inject]` метод Construct
  (как в существующих лесах). НЕ через конструктор (это MonoBehaviour).
- Подписка на сигналы в `OnEnable` (или после Construct), отписка в `OnDisable`.
  Кнопки: `button.onClick.AddListener(...)` в Start/OnEnable, RemoveListener в OnDisable.
- ВАЖНО: при инициализации брать ТЕКУЩЕЕ значение из сервиса (не ждать только
  сигнал) — иначе UI пустой до первого изменения. Напр. баланс показать сразу из
  `_currency.Balance`, потом обновлять по сигналу. (Та же логика "не надейся только
  на будущий сигнал", что и везде в проекте.)
- Каждое поле и метод — КОММЕНТАРИЙ на русском: что это и зачем.

---

## Вью 1: HudView (сцена Gameplay) — заказ, жизни, счёт

Заменяет грязный тестовый индикатор. Namespace `BurgerCatch.UI`.

Зависимости (`[Inject]`): `SignalBus`, `OrderSystem`, `ScoringSystem`.

`[SerializeField]`:
- `Transform _orderSlotsParent` — контейнер, куда лягут иконки заказа.
- `Image _orderSlotPrefab` — префаб одной иконки слота заказа (разработчик сделает).
- `Sprite[] _ingredientSprites` ИЛИ способ сопоставить IngredientType→Sprite
  (сделай поле-массив или сериализуемый словарь-замену: два массива
  `IngredientType[] _types` + `Sprite[] _sprites`, метод поиска). Комментарием
  объясни, что разработчик заполнит соответствие.
- `TMP_Text _scoreText` — счёт забега.
- `Transform _livesParent` + `Image _lifePrefab` ИЛИ `Image[] _lifeIcons` (3 сердца).

Поведение:
- На `OrderChangedSignal` (несёт `IngredientType[] Recipe`) — перестроить ряд иконок
  заказа (создать по числу слотов из префаба, проставить спрайты по типам).
- На `OrderItemMatchedSignal` / при изменении `OrderSystem.CurrentIndex` — подсветить
  текущий нужный слот (например, масштаб/прозрачность; собранные — тусклые).
  Брать индекс из `OrderSystem.CurrentIndex`.
- Счёт: обновлять `_scoreText` из `ScoringSystem.RunScore`. Если есть сигнал
  изменения счёта — слушать его; иначе обновлять при `OrderCompletedSignal`.
- Жизни: на `LifeLostSignal` (несёт `Remaining`) — обновить отображение сердец
  (показать `Remaining` штук). При старте — полное число.

---

## Вью 2: GameOverView (overlay поверх Gameplay)

Зависимости: `SignalBus`, `ScoringSystem`, `ISaveService` (для рекорда).

`[SerializeField]`:
- `GameObject _root` — корневая панель (включать/выключать).
- `TMP_Text _scoreText`, `TMP_Text _bestText`.
- `Button _continueButton` — "продолжить за рекламу".
- `Button _menuButton` — "в меню".

Поведение:
- Скрыт по умолчанию (`_root.SetActive(false)`).
- На `GameOverTriggeredSignal` — показать панель, заполнить счёт
  (`ScoringSystem.RunScore`) и рекорд (`saveService.Data.BestScore`).
- `_continueButton` → стрельнуть `ContinueRequestedSignal` (логика продолжения —
  в ContinueController из Фазы 3). После успешного continue (сигнал
  `RunResumedSignal`) — скрыть панель.
- `_menuButton` → загрузить сцену меню. Через существующий `ISceneLoader` (inject)
  или стрельнуть сигнал, который слушает GameFlow. Используй `ISceneLoader` если он
  доступен; иначе TODO-коммент.

---

## Вью 3: MenuView (сцена MainMenu) — выбор героя, баланс, играть

Зависимости: `SignalBus`, `ICurrencyService`, `IShopService`, `SkinCatalog`,
`ISaveService`, `ISceneLoader`.

`[SerializeField]`:
- `TMP_Text _balanceText`.
- `Transform _skinsParent` + `Button _skinButtonPrefab` (кнопка выбора героя).
- `Button _playButton`, `Button _shopButton`, `Button _leaderboardButton`.

Поведение:
- Баланс: при старте из `_currency.Balance`, обновлять на `CurrencyChangedSignal`.
- Список героев: пройти `SkinCatalog.Skins`, создать кнопку на каждого. Кнопка
  показывает, выбран ли (SelectedSkin), куплен ли (OwnedSkins). Нажатие на
  купленного → `IShopService.SelectSkin(id)`. На некупленного → можно увести в
  магазин или ничего (TODO-коммент, разработчик решит).
- `_playButton` → `sceneLoader.Load("Gameplay")`.
- `_shopButton` → открыть магазин (если магазин — отдельная панель, включить её;
  если отдельная сцена — загрузить). Сделай через `[SerializeField] GameObject
  _shopPanel` toggle ИЛИ TODO. 
- `_leaderboardButton` → `ILeaderboardService` (если есть метод показа) или TODO.

---

## Вью 4: ShopView — скины, бусты, no-ads

Зависимости: `SignalBus`, `ICurrencyService`, `IShopService`, `IIapService`,
`SkinCatalog`, `BoostCatalog`, `ISaveService`.

`[SerializeField]`:
- `TMP_Text _balanceText`.
- `Transform _skinsParent` + `Button _shopItemPrefab` (карточка товара).
- `Transform _boostsParent` (+ тот же или отдельный префаб карточки).
- `Button _noAdsButton`.
- `Button _closeButton`.

Поведение:
- Баланс — как в меню.
- Скины: из `SkinCatalog`. Карточка показывает цену/иконку/состояние (куплен/
  доступен/iap-only). Нажатие:
  - обычный за валюту → `IShopService.TryBuySkin(id)`;
  - `IapOnly` → `IIapService.Buy(skin.IapId)`.
  - на `SkinPurchasedSignal` — обновить карточки.
- Бусты: из `BoostCatalog`. Нажатие → `IShopService.TryBuyBoost(type)`. Обновлять
  по `BoostPurchasedSignal`.
- no-ads: `_noAdsButton` → `IIapService.Buy("noads")`. На `NoAdsActivatedSignal` —
  скрыть/задизейблить кнопку.
- `_closeButton` → закрыть панель.
- Если денег не хватило (`TryBuy*` вернул false) — визуальный фидбек (тряхнуть
  баланс / лог). Минимально — TODO-коммент, разработчик добавит.

---

## ГРАНИЦЫ — что НЕ делать

- **НЕ писать игровую логику в UI.** Не списывать валюту, не менять PlayerData, не
  считать цену/жизни в вью. Только вызовы сервисов + отображение.
- **НЕ создавать** Canvas, префабы, не расставлять элементы — это разработчик в
  инспекторе. Ты пишешь только C#-классы вью с [SerializeField]-ссылками.
- **НЕ трогать** сервисы 4A, геймплейное ядро, сигналы (только подписываться/
  стрелять существующие; новый сигнал — только если явно нужен, с декларацией).
- **НЕ менять** существующие сигнатуры.
- **НЕ удалять** тестовые леса (`TestIngredientRun`, `TestOrderHud`) — разработчик
  удалит сам, когда новый UI заработает.
- **НЕ добавлять** пакеты. TMP уже есть.

---

## Регистрация в Zenject

UI-вью на сцене инжектятся через SceneContext. Если вью на сцене и помечены для
инжекта — Zenject подхватит при наличии SceneContext. Для вью, создающих
зависимости через [Inject] Construct — убедись, что они под SceneContext.
Биндить сами вью в инсталлере НЕ обязательно, если они MonoBehaviour на сцене
(Zenject инжектит их через ZenjectBinding/контекст). Если нужно — добавь
комментарий, как разработчику привязать (ZenjectBinding компонент или
GameObjectContext).

---

## Как проверить (после ручной сборки Canvas)

1. HUD: заказ виден иконками, текущий подсвечен, жизни убывают, счёт растёт.
2. Game over: панель всплывает со счётом/рекордом, кнопки работают.
3. Меню: баланс, выбор героя, играть уводит в забег.
4. Магазин: покупка за валюту списывает, no-ads дизейблит кнопку.

---

## Файлы

Создать в `00-Code/UI/`: `HudView.cs`, `GameOverView.cs`, `MenuView.cs`,
`ShopView.cs`. При необходимости — мелкие вспомогательные вью для карточек
(`SkinCardView`, `ShopItemCardView`) — если выносишь карточку в отдельный
компонент (рекомендуется для чистоты).

Комментарии — подробные, на русском, у каждого поля и метода.
