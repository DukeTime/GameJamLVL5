using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class NavGrid2D : MonoBehaviour
{
    public Tilemap tilemap;
    [Tooltip("Слой стен (TilemapCollider/Composite)")]
    public LayerMask obstacleMask;

    [Header("Probe")]
    [Tooltip("Размер бокса для проверки клетки: cellSize * этот множитель")]
    [Range(0.5f, 1.1f)] public float cellProbeScale = 0.95f;   // ключевой фикс
    [Tooltip("Радиус агента: используется для LOS-упрощения и дополнительной проверки")]
    [Range(0.1f, 0.6f)] public float agentRadius = 0.25f;

    [Header("Graph")]
    public bool eightDirections = true;
    public bool drawGizmos;

    void Reset() { tilemap = GetComponent<Tilemap>(); }

    public Vector3Int WorldToCell(Vector2 world) => tilemap.WorldToCell(world);
    public Vector2 CellCenter(Vector3Int cell) => (Vector2)tilemap.GetCellCenterWorld(cell);
    public Vector2 CellSize() => (Vector2)tilemap.cellSize;
    public bool InBounds(Vector3Int cell) => tilemap.cellBounds.Contains(cell);

    public bool Walkable(Vector3Int cell)
    {
        // Блокируем клетку, если в её области есть стены (Outlines/Polygons — оба варианта ловятся)
        Vector2 size = CellSize() * cellProbeScale;
        return !Physics2D.OverlapBox(CellCenter(cell), size, 0f, obstacleMask);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || tilemap == null) return;
        var b = tilemap.cellBounds;
        Vector2 size = CellSize() * cellProbeScale;
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                var c = new Vector3Int(x, y, 0);
                var p = CellCenter(c);
                Gizmos.color = Walkable(c) ? new Color(0, 1, 0, .15f) : new Color(1, 0, 0, .25f);
                Gizmos.DrawCube(p, size);
            }
    }
#endif

    public IEnumerable<Vector3Int> Neighbors(Vector3Int c)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!eightDirections && Mathf.Abs(dx) + Mathf.Abs(dy) > 1) continue;

                var n = new Vector3Int(c.x + dx, c.y + dy, 0);
                if (!InBounds(n) || !Walkable(n)) continue;

                // запрет среза угла
                if (dx != 0 && dy != 0)
                {
                    if (!Walkable(new Vector3Int(c.x + dx, c.y, 0))) continue;
                    if (!Walkable(new Vector3Int(c.x, c.y + dy, 0))) continue;
                }
                yield return n;
            }
    }
}
