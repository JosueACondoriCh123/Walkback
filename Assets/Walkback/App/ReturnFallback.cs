// Written with Claude (AI), directed and tested by team MARSXR
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// PLAN B for the return layer, used only until session C's ReturnMap (Assets/Walkback/Model/) lands.
    /// Minutes to walk back to the habitat from every node of the grid, along the fastest way over the
    /// real relief, instead of ReturnStandIn's straight line on flat ground:
    ///   - Dijkstra from the habitat over the grid, 8 neighbors per node (own binary heap: Unity's C#
    ///     has no PriorityQueue).
    ///   - Flat ground costs the walking model's own test pace (ReturnStandIn.FlatMinutesPerKilometer).
    ///   - Slope changes the pace by Tobler's hiking function, normalized to 1 on flat ground:
    ///     factor(S) = exp(-3.5 * |S + 0.05|) / exp(-3.5 * 0.05), S = rise / run in the walking direction
    ///     (Tobler, W., 1993, "Three presentations on geographical analysis and modeling", NCGIA
    ///     Technical Report 93-1). It was measured on Earth without a suit: it is NOT the team's walking
    ///     model, and the legend says so.
    ///   - Nodes steeper than TerrainLayers.SlopeLimitDegrees (the hatched no-go band of the slope
    ///     layer) cannot be walked, and neither can a step between two nodes steeper than that.
    ///     Nodes with no way back are float.PositiveInfinity: TerrainLayers.PaintReturn hatches them gray.
    /// Known limits: paths follow the 8 grid directions, so a straight walk in another direction reads
    /// up to 8 % longer than it is; sand is ignored; the grid (3.9 m at Gale) hides smaller steps.
    /// Pure function: no scene needed, the automatic test calls it directly.
    /// </summary>
    public static class ReturnFallback
    {
        public const string Label = "PLACEHOLDER: fastest way over the relief, not the walking model yet";

        const float ToblerK = 3.5f;
        const float ToblerOffset = 0.05f;

        /// <summary>Pace multiplier for a slope S = rise / run in the walking direction (1 on flat ground, under 1 = slower).</summary>
        public static float SlopeFactor(float s)
        {
            return Mathf.Exp(-ToblerK * Mathf.Abs(s + ToblerOffset) + ToblerK * ToblerOffset);
        }

        /// <summary>
        /// Minutes back to the habitat from every node, row 0 = north (same layout as SiteData.Heights).
        /// <paramref name="slopeDegrees"/> is TerrainLayers.SlopeDegrees of the same site.
        /// </summary>
        public static float[] Minutes(SiteData site, float[] slopeDegrees)
        {
            int n = site.GridSize;
            float spacing = site.NodeSpacing;
            float[] heights = site.Heights;
            float limit = TerrainLayers.SlopeLimitDegrees;
            float maxGrade = Mathf.Tan(limit * Mathf.Deg2Rad);
            float pace = ReturnStandIn.FlatMinutesPerKilometer / 1000f;   // minutes per meter on flat ground

            var minutes = new float[n * n];
            for (int i = 0; i < minutes.Length; i++) minutes[i] = float.PositiveInfinity;

            // Start at the node nearest to the habitat; the few meters between them count as flat ground.
            Vector2 habitat = site.Habitat;
            int startColumn = Mathf.Clamp(Mathf.RoundToInt(habitat.x / spacing), 0, n - 1);
            int startRow = Mathf.Clamp(Mathf.RoundToInt((site.SideMeters - habitat.y) / spacing), 0, n - 1);
            int start = startRow * n + startColumn;
            minutes[start] = Vector2.Distance(site.NodeToMeters(startColumn, startRow), habitat) * pace;

            var heap = new MinHeap(n * n);
            heap.Push(start, minutes[start]);
            float diagonal = spacing * Mathf.Sqrt(2f);

            while (heap.Count > 0)
            {
                int u;
                float t;
                heap.Pop(out u, out t);
                if (t > minutes[u]) continue;   // an older, slower entry of the same node
                int ur = u / n, uc = u - ur * n;
                for (int dr = -1; dr <= 1; dr++)
                {
                    int vr = ur + dr;
                    if (vr < 0 || vr >= n) continue;
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int vc = uc + dc;
                        if (vc < 0 || vc >= n) continue;
                        int v = vr * n + vc;
                        if (slopeDegrees[v] > limit) continue;   // no-go ground
                        float run = dr != 0 && dc != 0 ? diagonal : spacing;
                        // The walk goes from v toward the habitat, so it steps from v onto u.
                        float grade = (heights[u] - heights[v]) / run;
                        if (Mathf.Abs(grade) > maxGrade) continue;
                        float arrival = t + run * pace / SlopeFactor(grade);
                        if (arrival < minutes[v])
                        {
                            minutes[v] = arrival;
                            heap.Push(v, arrival);
                        }
                    }
                }
            }
            return minutes;
        }

        /// <summary>A binary min-heap of (node, minutes). A node may be pushed more than once; stale entries are skipped on pop.</summary>
        sealed class MinHeap
        {
            int[] nodes;
            float[] keys;
            public int Count { get; private set; }

            public MinHeap(int capacity)
            {
                nodes = new int[Mathf.Max(capacity, 16)];
                keys = new float[nodes.Length];
            }

            public void Push(int node, float key)
            {
                if (Count == nodes.Length)
                {
                    System.Array.Resize(ref nodes, nodes.Length * 2);
                    System.Array.Resize(ref keys, keys.Length * 2);
                }
                int i = Count++;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (keys[parent] <= key) break;
                    nodes[i] = nodes[parent];
                    keys[i] = keys[parent];
                    i = parent;
                }
                nodes[i] = node;
                keys[i] = key;
            }

            public void Pop(out int node, out float key)
            {
                node = nodes[0];
                key = keys[0];
                Count--;
                if (Count == 0) return;
                int lastNode = nodes[Count];
                float lastKey = keys[Count];
                int i = 0;
                while (true)
                {
                    int child = 2 * i + 1;
                    if (child >= Count) break;
                    if (child + 1 < Count && keys[child + 1] < keys[child]) child++;
                    if (keys[child] >= lastKey) break;
                    nodes[i] = nodes[child];
                    keys[i] = keys[child];
                    i = child;
                }
                nodes[i] = lastNode;
                keys[i] = lastKey;
            }
        }
    }
}
