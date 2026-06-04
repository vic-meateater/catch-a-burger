using UnityEngine;

namespace BurgerCatch.Data
{
  /// <summary>
  /// Параметры спавн-директора. Раньше были private const в SpawnDirector —
  /// вынесены сюда, чтобы крутить в инспекторе без перекомпиляции.
  /// </summary>
  [CreateAssetMenu(menuName = "BurgerCatch/GameplayConfig")]
  public sealed class GameplayConfig : ScriptableObject
  {
    [Header("База")]
    [SerializeField] private float _baseInterval = 1.2f;
    [SerializeField] private float _baseSpeed = 0.2f;

    [Header("Рост за бургер")]
    [SerializeField] private float _speedPerBurger = 0.015f;
    [SerializeField] private float _intervalCutPerBurger = 0.03f;

    [Header("Потолки честной сложности")]
    [SerializeField] private float _maxSpeed = 0.6f;
    [SerializeField] private float _minInterval = 0.5f;

    [Header("Логика спавна")]
    [SerializeField] private int _forceNeededAfter = 3;
    [SerializeField] private int _maxTotalThreats = 6;
    [SerializeField] private int _maxThreatsOnFarSide = 1;
    [SerializeField] private float _neededChance = 0.3f;

    [Header("Чистое окно при смене заказа")]
    [SerializeField] private float _orderChangeWindow = 1.5f;

    public float BaseInterval => _baseInterval;
    public float BaseSpeed => _baseSpeed;

    public float SpeedPerBurger => _speedPerBurger;
    public float IntervalCutPerBurger => _intervalCutPerBurger;

    public float MaxSpeed => _maxSpeed;
    public float MinInterval => _minInterval;

    public int ForceNeededAfter => _forceNeededAfter;
    public int MaxTotalThreats => _maxTotalThreats;
    public int MaxThreatsOnFarSide => _maxThreatsOnFarSide;
    public float NeededChance => _neededChance;

    /// <summary>Длительность паузы спавна при смене заказа (игрок читает рецепт).</summary>
    public float OrderChangeWindow => _orderChangeWindow;
  }
}
