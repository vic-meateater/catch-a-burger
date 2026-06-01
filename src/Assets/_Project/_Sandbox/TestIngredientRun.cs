using System.Collections.Generic;
using System.Linq;
using BurgerCatch.Gameplay.Conveyor;
using BurgerCatch.Gameplay.Time;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project._Sandbox
{
  public class TestIngredientRun : MonoBehaviour
  {
    [SerializeField] private float _spawnEvery = 1.5f;
    [SerializeField] private float _laneX = 2f; // X левой/правой ленты
    [SerializeField] private float _topY = 4f; // Progress=0
    [SerializeField] private float _bottomY = -4f; // Progress=1
    [SerializeField] private Button _switchSideButton;

    private IGameClock _clock;
    private ConveyorSystem _conveyor;

    [Inject]
    public void Construct(IGameClock clock, ConveyorSystem conveyorSystem)
    {
      _clock = clock;
      _conveyor = conveyorSystem;
    }


    // Карта: ингредиент -> его кубик
    private readonly Dictionary<Ingredient, Transform> _cubes
      = new Dictionary<Ingredient, Transform>();

    private float _spawnTimer;

    private void Start()
    {
      // Игровое время по умолчанию заморожено (IsRunning=false),
      // поэтому сразу запускаем, иначе ничего не поедет.
      _clock.Resume();
      _switchSideButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
      if (_clock.IsRunning) _clock.Pause();
      else _clock.Resume();
      Debug.Log($"[Test] Clock running = {_clock.IsRunning}");

    }

    private void Update()
    {
      // Спавн по реальному времени (это леса, не геймплей) — но только
      // когда игровое время идёт, чтобы на паузе не плодились.
      if (_clock.IsRunning)
      {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnEvery)
        {
          _spawnTimer = 0f;
          var side = Random.value < 0.5f ? Side.Left : Side.Right;
          _conveyor.Spawn(side);
        }
      }

      SyncCubes();
    }

    private void SyncCubes()
    {
      // Создать кубик для новых ингредиентов
      foreach (var ing in _conveyor.Active)
      {
        if (_cubes.ContainsKey(ing)) continue;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TestIngredient";
        _cubes[ing] = go.transform;
      }

      // Обновить позиции и убрать пропавшие
      var toRemove = new List<Ingredient>();
      foreach (var kv in _cubes)
      {
        var ing = kv.Key;
        var tr = kv.Value;

        if (!_conveyor.Active.Contains(ing))
        {
          Destroy(tr.gameObject);
          toRemove.Add(ing);
          continue;
        }

        float x = ing.Side == Side.Left ? -_laneX : _laneX;
        float y = Mathf.Lerp(_topY, _bottomY, ing.Progress);
        tr.position = new Vector3(x, y, 0f);
      }

      foreach (var ing in toRemove)
        _cubes.Remove(ing);
    }
  }
}