using System.Collections.Generic;
using BurgerCatch.Core.Saves;
using BurgerCatch.Core.Shop;
using BurgerCatch.Data;
using BurgerCatch.Events;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BurgerCatch.UI
{
  /// <summary>
  /// Панель выбора героя из КУПЛЕННЫХ. Тап по карточке → выбрать героя.
  /// Покупки тут НЕТ (новых героев покупают в магазине). ТОНКИЙ слой:
  /// выбор идёт через IShopService.SelectSkin, состояние — из ISaveService/SkinCatalog.
  /// </summary>
  public sealed class SkinSelectView : MonoBehaviour
  {
    [Tooltip("Контейнер карточек купленных героев.")]
    [SerializeField] private Transform _cardsParent;

    [Tooltip("Префаб карточки героя (компонент SkinCardView).")]
    [SerializeField] private SkinCardView _cardPrefab;

    [Tooltip("Кнопка закрыть панель.")]
    [SerializeField] private Button _closeButton;

    private SignalBus _signalBus;
    private IShopService _shop;
    private SkinCatalog _skinCatalog;
    private ISaveService _saveService;

    // Созданные карточки — чтобы обновлять выделение по сигналу.
    private readonly List<SkinCardView> _cards = new List<SkinCardView>();

    [Inject]
    public void Construct(
      SignalBus signalBus,
      IShopService shop,
      SkinCatalog skinCatalog,
      ISaveService saveService)
    {
      _signalBus = signalBus;
      _shop = shop;
      _skinCatalog = skinCatalog;
      _saveService = saveService;
    }

    private void OnEnable()
    {
      _signalBus.Subscribe<SkinSelectedSignal>(OnSkinSelected);

      if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);

      // Панель показана — строим список купленных.
      BuildOwnedCards();
    }

    private void OnDisable()
    {
      // Guard: панель могла отключиться до инъекции зависимостей.
      if (_signalBus == null) return;

      _signalBus.Unsubscribe<SkinSelectedSignal>(OnSkinSelected);

      if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    // Построить карточки ТОЛЬКО купленных героев.
    private void BuildOwnedCards()
    {
      // Снести прошлые.
      for (int i = 0; i < _cards.Count; i++)
        if (_cards[i] != null) Destroy(_cards[i].gameObject);
      _cards.Clear();

      if (_skinCatalog == null || _cardPrefab == null || _cardsParent == null)
        return;

      string selected = _saveService.Data.SelectedSkin;

      var skins = _skinCatalog.Skins;
      for (int i = 0; i < skins.Count; i++)
      {
        var def = skins[i];
        if (def == null) continue;
        if (!_shop.IsSkinOwned(def.Id)) continue; // некупленных тут нет — они в магазине

        SkinCardView card = Instantiate(_cardPrefab, _cardsParent);
        card.Bind(def, OnCardClicked);
        card.SetState(owned: true, selected: def.Id == selected);
        _cards.Add(card);
      }
    }

    // Тап по карточке — выбрать героя (логика выбора в IShopService).
    private void OnCardClicked(string id)
    {
      _shop.SelectSkin(id);
    }

    // Выбор сменился — обновить selected-маркер на карточках.
    private void OnSkinSelected(SkinSelectedSignal s) => RefreshSelection();

    private void RefreshSelection()
    {
      string selected = _saveService.Data.SelectedSkin;

      for (int i = 0; i < _cards.Count; i++)
      {
        SkinCardView card = _cards[i];
        if (card == null) continue;

        // Здесь все карточки — купленные, поэтому owned всегда true.
        card.SetState(owned: true, selected: card.SkinId == selected);
      }
    }

    // Закрыть панель.
    private void OnCloseClicked()
    {
      gameObject.SetActive(false);
    }
  }
}
