using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class NavGrid2D : MonoBehaviour
{
    public Tilemap tilemap;
    [Tooltip("Слой стен (Tilemap/CompositeCollider2D).")]
    public LayerMask obstacleMask;

    [Header("Проверка клетки")]
    [Range(0.5f, 1.1f)] public float cellProbeScale = 0.85f;   // размер бокса относительно cellSize
    [Range(0.05f, 0.8f)] public float agentRadius = 0.25f;     // радиус агента (для LOS/упрощения)

    [Header("Clearance")]
    [Tooltip("Доп. зазор от стен (м). Увеличивает 'толщину' препятствий при построении графа.")]
    [Range(0f, 0.8f)] public float extraClearance = 0.25f;

    [Header("Граф")]
    public bool eightDirections = true;
    public bool drawWalkable = false;

    void Reset() { tilemap = GetComponent<Tilemap>(); }

    public Vector3Int WorldToCell(Vector2 world) => tilemap.WorldToCell(world);
    public Vector2 CellCenter(Vector3Int c) => (Vector2)tilemap.GetCellCenterWorld(c);
    public Vector2 CellSize() => (Vector2)tilemap.cellSize;
    public bool InBounds(Vector3Int c) => tilemap != null && tilemap.cellBounds.Contains(c);

    public bool Walkable(Vector3Int c)
    {
        // клетка непроходима, если увеличенный бокс пересекает стены
        Vector2 baseSize = CellSize() * cellProbeScale;
        // раздуваем препятствия на extraClearance с каждой стороны
        Vector2 inflated = baseSize + new Vector2(extraClearance * 2f, extraClearance * 2f);

        return !Physics2D.OverlapBox(CellCenter(c), inflated, 0f, obstacleMask);
    }

    public IEnumerable<Vector3Int> Neighbors(Vector3Int c)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!eightDirections && Mathf.Abs(dx) + Mathf.Abs(dy) > 1) continue;

                var n = new Vector3Int(c.x + dx, c.y + dy, 0);
                if (!InBounds(n) || !Walkable(n)) continue;

                // не разрешаем диагональный "срез угла" через два смежных блока
                if (dx != 0 && dy != 0)
                {
                    if (!Walkable(new Vector3Int(c.x + dx, c.y, 0))) continue;
                    if (!Walkable(new Vector3Int(c.x, c.y + dy, 0))) continue;
                }
                yield return n;
            }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawWalkable || tilemap == null) return;
        var b = tilemap.cellBounds;
        Vector2 baseSize = CellSize() * cellProbeScale;
        Vector2 inflated = baseSize + new Vector2(extraClearance * 2f, extraClearance * 2f);
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var c = new Vector3Int(x, y, 0); var p = CellCenter(c);
                Gizmos.color = Walkable(c) ? new Color(0, 1, 0, 0.12f) : new Color(1, 0, 0, 0.18f);
                Gizmos.DrawCube(p, inflated);
            }
    }
#endif
}
