using System.Collections.Generic;
using UnityEngine;

public static class PathFinder2D
{
    struct Rec { public int g, f; public Vector3Int parent; public byte state; }
    const int COST_STRAIGHT = 10, COST_DIAG = 14;

    static int Heur(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        int diag = Mathf.Min(dx, dy), straight = dx + dy - 2 * diag;
        return diag * COST_DIAG + straight * COST_STRAIGHT;
    }

    static bool AdjustToNearestWalkable(NavGrid2D grid, ref Vector3Int cell, int maxRadius = 8)
    {
        if (grid.InBounds(cell) && grid.Walkable(cell)) return true;
        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    var c = new Vector3Int(cell.x + dx, cell.y + dy, 0);
                    if (grid.InBounds(c) && grid.Walkable(c)) { cell = c; return true; }
                }
        }
        return false;
    }

    public static bool FindPath(Vector2 worldStart, Vector2 worldGoal, NavGrid2D grid, List<Vector2> result, int maxNodes = 20000)
    {
        result.Clear();
        if (grid == null || grid.tilemap == null) return false;

        Vector3Int s = grid.WorldToCell(worldStart);
        Vector3Int t = grid.WorldToCell(worldGoal);

        if (!AdjustToNearestWalkable(grid, ref s)) return false;
        if (!AdjustToNearestWalkable(grid, ref t)) return false;

        var open = new OpenHeap(256);
        var rec = new Dictionary<Vector3Int, Rec>(1024);

        void SetRec(Vector3Int n, Rec r) => rec[n] = r;
        bool TryGet(Vector3Int n, out Rec r) => rec.TryGetValue(n, out r);

        // старт
        var r0 = new Rec { g = 0, f = Heur(s, t), parent = new Vector3Int(int.MinValue, 0, 0), state = 1 };
        SetRec(s, r0); open.Push(s, r0.f);

        int iter = 0;
        while (open.Count > 0 && iter++ < maxNodes)
        {
            var cur = open.PopMin();
            if (!TryGet(cur, out var rc) || rc.state == 2) continue;
            rc.state = 2; SetRec(cur, rc);

            if (cur == t)
            {
                var list = new List<Vector2>(64);
                var c = cur;
                while (true)
                {
                    list.Add(grid.CellCenter(c));
                    var p = rec[c].parent;
                    if (p.x == int.MinValue) break;
                    c = p;
                }
                list.Reverse();
                SimplifyWithLOS(list, grid.obstacleMask, grid.agentRadius);
                result.AddRange(list);
                return true;
            }

            foreach (var nb in grid.Neighbors(cur))
            {
                int add = (nb.x != cur.x && nb.y != cur.y) ? COST_DIAG : COST_STRAIGHT;
                int ng = rc.g + add;

                if (!TryGet(nb, out var rn))
                {
                    rn.g = ng; rn.f = ng + Heur(nb, t); rn.parent = cur; rn.state = 1;
                    SetRec(nb, rn); open.Push(nb, rn.f);
                }
                else if (rn.state == 1 && ng < rn.g)
                {
                    rn.g = ng; rn.f = ng + Heur(nb, t); rn.parent = cur;
                    SetRec(nb, rn); open.Update(nb, rn.f);
                }
            }
        }
        return false;
    }

    static void SimplifyWithLOS(List<Vector2> path, LayerMask mask, float r)
    {
        if (path.Count <= 2) return;
        var outp = new List<Vector2>(path.Count);
        int anchor = 0; outp.Add(path[anchor]);
        for (int i = 2; i < path.Count; i++)
        {
            if (Blocked(path[anchor], path[i], mask, r))
            { outp.Add(path[i - 1]); anchor = i - 1; }
        }
        outp.Add(path[^1]); path.Clear(); path.AddRange(outp);
    }

    static bool Blocked(Vector2 a, Vector2 b, LayerMask mask, float r)
    {
        if (Physics2D.Linecast(a, b, mask)) return true;
        var dir = (b - a).normalized; var n = new Vector2(-dir.y, dir.x); float rr = r * 0.9f;
        return Physics2D.Linecast(a + n * rr, b + n * rr, mask) || Physics2D.Linecast(a - n * rr, b - n * rr, mask);
    }

    struct HeapItem { public Vector3Int node; public int f; }
    class OpenHeap
    {
        readonly List<HeapItem> data; readonly Dictionary<Vector3Int, int> idx;
        public int Count => data.Count;
        public OpenHeap(int cap) { data = new(cap); idx = new(cap); }
        public void Push(Vector3Int n, int f) { data.Add(new HeapItem { node = n, f = f }); idx[n] = data.Count - 1; Up(data.Count - 1); }
        public Vector3Int PopMin() { var root = data[0]; var last = data[^1]; data[0] = last; if (data.Count > 1) idx[last.node] = 0; data.RemoveAt(data.Count - 1); idx.Remove(root.node); if (data.Count > 0) Down(0); return root.node; }
        public void Update(Vector3Int n, int f) { if (!idx.TryGetValue(n, out int i)) return; if (data[i].f == f) return; data[i] = new HeapItem { node = n, f = f }; Up(i); Down(i); }
        void Up(int i) { while (i > 0) { int p = (i - 1) >> 1; if (data[p].f <= data[i].f) break; Swap(p, i); i = p; } }
        void Down(int i) { while (true) { int l = i * 2 + 1, r = l + 1, s = i; if (l < data.Count && data[l].f < data[s].f) s = l; if (r < data.Count && data[r].f < data[s].f) s = r; if (s == i) break; Swap(s, i); i = s; } }
        void Swap(int a, int b) { var t = data[a]; data[a] = data[b]; data[b] = t; idx[data[a].node] = a; idx[data[b].node] = b; }
    }
}
