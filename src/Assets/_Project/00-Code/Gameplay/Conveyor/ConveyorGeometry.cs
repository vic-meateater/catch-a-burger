using UnityEngine;

namespace BurgerCatch.Gameplay.Conveyor
{
  /// <summary>
  /// V-образная геометрия: две наклонные ленты (~30°) сходятся к повару.
  /// Левая: спавн слева-сверху -> устье у центра-низа.
  /// Правая: симметрично. Наклон — визуальный, движение равномерное.
  /// (Side, Progress 0..1) -> мировая точка вдоль наклонной ленты.
  /// </summary>
  public sealed class ConveyorGeometry : MonoBehaviour
  {
    [Header("Точки СПАВНА (Progress = 0), верх лент")]
    [SerializeField] private Vector2 _leftSpawn  = new Vector2(-5f, 4f);
    [SerializeField] private Vector2 _rightSpawn = new Vector2( 5f, 4f);

    [Header("Точки ПОИМКИ/УСТЬЯ (Progress = 1), низ у центра")]
    [SerializeField] private Vector2 _leftCatch  = new Vector2(-1.2f, -3f);
    [SerializeField] private Vector2 _rightCatch = new Vector2( 1.2f, -3f);

    /// <summary>Где стоит повар на данной стороне (= точка поимки).</summary>
    public Vector3 CatchPoint(Side side)
      => side == Side.Left ? (Vector3)_leftCatch : (Vector3)_rightCatch;

    /// <summary>Позиция ингредиента вдоль наклонной ленты по прогрессу.</summary>
    public Vector3 PositionOf(Side side, float progress)
    {
      Vector2 from = side == Side.Left ? _leftSpawn : _rightSpawn;
      Vector2 to   = side == Side.Left ? _leftCatch : _rightCatch;
      return Vector2.Lerp(from, to, Mathf.Clamp01(progress));
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(_leftSpawn, _leftCatch);
      Gizmos.DrawLine(_rightSpawn, _rightCatch);

      Gizmos.color = Color.red; // устья = точки поимки/срыва
      Gizmos.DrawWireSphere(_leftCatch, 0.3f);
      Gizmos.DrawWireSphere(_rightCatch, 0.3f);
    }
  }
}