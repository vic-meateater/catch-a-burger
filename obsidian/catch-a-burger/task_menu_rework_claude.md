# Задача: переделка меню — текущий герой на главном + окно выбора героя

> Контекст — `CLAUDE.md` + UI-вью Фазы 4B уже есть. Меняем структуру меню:
> на главном экране показывается ТЕКУЩИЙ выбранный герой крупно; выбор героя —
> в отдельной панели (только из КУПЛЕННЫХ); покупка новых — остаётся в магазине
> (ShopView НЕ трогаем). Раздел ГРАНИЦЫ строго.

---

## Что меняется

СЕЙЧАС `MenuView` строит список карточек всех скинов прямо на главном экране.
НАДО:
1. `MenuView` (главный экран) — показывает крупно ТЕКУЩЕГО героя (спрайт + имя),
   баланс, кнопки [Играть] [Сменить героя] [Магазин] [Лидерборд]. Никаких карточек
   скинов на главном.
2. Новый `SkinSelectView` — отдельная панель (overlay). Показывает карточки ТОЛЬКО
   купленных героев. Тап по карточке → выбрать героя. Покупки тут НЕТ.
3. `ShopView` — без изменений (там покупка скинов/бустов/no-ads).

---

## Принцип (не нарушать)

UI тонкий: показывает и дёргает сервисы. Выбор героя → `IShopService.SelectSkin(id)`.
Состояние читается из `ISaveService.Data` / `SkinCatalog`. Логику не дублировать.

---

## Часть 1: переделать MenuView

Namespace `BurgerCatch.UI`. Зависимости (`[Inject]`): `SignalBus`,
`ICurrencyService`, `SkinCatalog`, `ISaveService`, `ISceneLoader`,
`GameFlowController`.

УБРАТЬ из MenuView: построение списка карточек скинов (`BuildSkins`,
`_skinCards`, `_skinCardPrefab`, `_skinsParent`, обработчики выбора). Это уезжает
в SkinSelectView.

`[SerializeField]`:
- `TMP_Text _balanceText` — баланс.
- `Image _currentHeroIcon` — крупный спрайт текущего героя.
- `TMP_Text _currentHeroName` — имя текущего героя.
- `Button _playButton`
- `Button _selectHeroButton` — открыть панель выбора героя.
- `Button _shopButton`
- `Button _leaderboardButton`
- `GameObject _skinSelectPanel` — панель выбора (включать по кнопке).
- `GameObject _shopPanel` — панель магазина (как было).

Поведение:
- Баланс: при старте из `_currency.Balance`, обновлять на `CurrencyChangedSignal`.
- Текущий герой: показать спрайт+имя выбранного скина. Брать id из
  `_saveService.Data.SelectedSkin`, данные (имя, иконку) — из
  `_skinCatalog.GetById(id)`. Обновлять при `SkinSelectedSignal` (игрок сменил
  героя в панели выбора).
- `_playButton` → `_sceneLoader.Load("Gameplay", () => _flow.SetState(GameState.Ready))`.
  (ВАЖНО: имя сцены — проверь, как называется сцена забега в проекте; вероятно
  "Gameplay". Поставь "Gameplay", добавь TODO-коммент свериться.)
- `_selectHeroButton` → `_skinSelectPanel.SetActive(true)`.
- `_shopButton` → `_shopPanel.SetActive(true)`.
- `_leaderboardButton` → TODO (показ таблицы, как было).

Метод обновления текущего героя вынеси отдельно (`RefreshCurrentHero()`), вызывать
при старте и на `SkinSelectedSignal`.

---

## Часть 2: новый SkinSelectView

Namespace `BurgerCatch.UI`, файл `00-Code/UI/SkinSelectView.cs`. Панель выбора
героя из КУПЛЕННЫХ. Зависимости: `SignalBus`, `IShopService`, `SkinCatalog`,
`ISaveService`.

`[SerializeField]`:
- `Transform _cardsParent` — контейнер карточек.
- `SkinCardView _cardPrefab` — префаб карточки (уже есть компонент SkinCardView).
- `Button _closeButton` — закрыть панель.

Поведение:
- При `OnEnable` (панель показана): построить карточки ТОЛЬКО купленных героев.
  Перебрать `_skinCatalog.Skins`, для каждого проверить
  `_shop.IsSkinOwned(def.Id)` — если куплен, создать карточку. Некупленные
  пропустить (их покупают в магазине).
- Карточка через `card.Bind(def, OnCardClicked)`. Состояние через
  `card.SetState(owned:true, selected: def.Id == SelectedSkin)`.
- `OnCardClicked(string id)` → `_shop.SelectSkin(id)`.
- На `SkinSelectedSignal` — обновить выделение карточек (selected-маркер на новом).
- `_closeButton` → `gameObject.SetActive(false)` (или скрыть корень панели).
- Подписки/отписки и очистка карточек (Destroy при перестроении) — по образцу
  существующих вью. Guard `if (_signalBus == null) return;` в OnDisable.

---

## ГРАНИЦЫ

- **НЕ трогать** `ShopView`, `ShopItemCardView`, геймплейное ядро, сервисы 4A.
- `SkinCardView` использовать как есть (Bind/SetState/SkinId) — НЕ менять.
- **НЕ создавать** Canvas/префабы/панели — разработчик соберёт в инспекторе.
  Ты пишешь только C#-классы вью с [SerializeField].
- **НЕ добавлять** покупку в SkinSelectView — только выбор из купленных.
- **НЕ менять** сигнатуры сигналов/сервисов. Использовать существующие
  `SkinSelectedSignal`, `CurrencyChangedSignal`, `IShopService.SelectSkin/IsSkinOwned`.
- Guard на null `_signalBus` в OnDisable во всех вью.

---

## Как проверить (после ручной сборки)

1. Главный экран показывает текущего героя (спрайт+имя), баланс, 4 кнопки.
2. [Сменить героя] открывает панель — там только купленные герои.
3. Тап по герою в панели → выбор меняется, главный экран обновил текущего героя.
4. Некупленные в панели выбора НЕ появляются (они в магазине).
5. [Магазин] открывает ShopView (как раньше).

---

## Файлы

Правка: `MenuView.cs` (убрать карточки, добавить текущего героя + кнопку выбора).
Создать: `SkinSelectView.cs`.

Комментарии — на русском.
