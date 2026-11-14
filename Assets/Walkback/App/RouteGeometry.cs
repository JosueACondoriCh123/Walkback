// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Walkback
{
    /// <summary>
    /// Lines draped on the map. Input in local meters (x east, y north, from the south-west corner);
    /// output in the terrain mesh's space (meters, origin at the map center, y up = height above the
    /// lowest point, z north), so a mesh built here sits on the terrain under the same parent.
    /// </summary>
    public static class RouteGeometry
    {
        /// <summary>Terrain height (above the site's lowest point) at local meters, bilinear between nodes.</summary>
        public static float HeightAt(SiteData site, float x, float y)
        {
            int n = site.GridSize;
            float spacing = site.NodeSpacing;
            float gc = Mathf.Clamp(x / spacing, 0f, n - 1);
            float gr = Mathf.Clamp((site.SideMeters - y) / spacing, 0f, n - 1);
            int c0 = Mathf.Min((int)gc, n - 2), r0 = Mathf.Min((int)gr, n - 2);
            float fc = gc - c0, fr = gr - r0;
            float[] h = site.Heights;
            float top = Mathf.Lerp(h[r0 * n + c0], h[r0 * n + c0 + 1], fc);
            float bottom = Mathf.Lerp(h[(r0 + 1) * n + c0], h[(r0 + 1) * n + c0 + 1], fc);
            return Mathf.Lerp(top, bottom, fr) - site.MinElevation;
        }

        /// <summary>Highest of the four nodes around a point: never below the drawn terrain triangles.</summary>
        public static float CellTopAt(SiteData site, float x, float y)
        {
            int n = site.GridSize;
            float spacing = site.NodeSpacing;
            float gc = Mathf.Clamp(x / spacing, 0f, n - 1);
            float gr = Mathf.Clamp((site.SideMeters - y) / spacing, 0f, n - 1);
            int c0 = Mathf.Min((int)gc, n - 2), r0 = Mathf.Min((int)gr, n - 2);
            float[] h = site.Heights;
            float top = Mathf.Max(Mathf.Max(h[r0 * n + c0], h[r0 * n + c0 + 1]), Mathf.Max(h[(r0 + 1) * n + c0], h[(r0 + 1) * n + c0 + 1]));
            return top - site.MinElevation;
        }

        /// <summary>Local meters to the terrain mesh's space, lifted above the ground.</summary>
        public static Vector3 ToMap(SiteData site, Vector2 meters, float lift)
        {
            float half = site.SideMeters / 2f;
            return new Vector3(meters.x - half, HeightAt(site, meters.x, meters.y) + lift, meters.y - half);
        }

        public static float Length(IList<Vector2> points)
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++) total += Vector2.Distance(points[i - 1], points[i]);
            return total;
        }

        /// <summary>A point every <paramref name="step"/> meters along the polyline, keeping every corner.</summary>
        public static List<Vector2> Densify(IList<Vector2> points, float step)
        {
            var result = new List<Vector2>();
            if (points == null || points.Count == 0) return result;
            result.Add(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 a = points[i - 1], b = points[i];
                float d = Vector2.Distance(a, b);
                if (d < 0.01f) continue;
                int parts = Mathf.Max(1, Mathf.CeilToInt(d / step));
                for (int k = 1; k <= parts; k++) result.Add(Vector2.Lerp(a, b, k / (float)parts));
            }
            return result;
        }

        /// <summary>
        /// A flat strip <paramref name="width"/> meters wide along the polyline; each vertex sits on the
        /// terrain under it plus <paramref name="lift"/>, so the strip follows the relief across its width too.
        /// uv.x = distance along / total length, uv.y = 0 left edge, 1 right edge.
        /// </summary>
        public static Mesh BuildStrip(SiteData site, IList<Vector2> points, float width, float lift, float step, Mesh reuse)
        {
            List<Vector2> d = Densify(points, step);
            var mesh = reuse != null ? reuse : new Mesh { name = "RouteStrip", hideFlags = HideFlags.DontSave };
            mesh.Clear();
            if (d.Count < 2) return mesh;

            int count = d.Count;
            var vertices = new Vector3[count * 2];
            var uv = new Vector2[count * 2];
            var normals = new Vector3[count * 2];
            float total = 0f;
            for (int i = 1; i < count; i++) total += Vector2.Distance(d[i - 1], d[i]);
            if (total <= 0f) total = 1f;

            const int window = 3;   // tangent averaged over about 12 m: 8-neighbor zigzags stay smooth
            float along = 0f;
            float half = width / 2f;
            for (int i = 0; i < count; i++)
            {
                if (i > 0) along += Vector2.Distance(d[i - 1], d[i]);
                Vector2 t = d[Mathf.Min(i + window, count - 1)] - d[Mathf.Max(i - window, 0)];
                if (t.sqrMagnitude < 1e-6f) t = Vector2.right;
                t.Normalize();
                Vector2 left = new Vector2(-t.y, t.x);
                Vector2 pl = d[i] + left * half, pr = d[i] - left * half;
                vertices[2 * i] = Lifted(site, pl, lift);
                vertices[2 * i + 1] = Lifted(site, pr, lift);
                float u = along / total;
                uv[2 * i] = new Vector2(u, 0f);
                uv[2 * i + 1] = new Vector2(u, 1f);
                normals[2 * i] = normals[2 * i + 1] = Vector3.up;
            }

            var triangles = new int[(count - 1) * 6];
            for (int i = 0; i < count - 1; i++)
            {
                int a = 2 * i, b = a + 1, c = a + 2, e = a + 3;
                triangles[6 * i] = a; triangles[6 * i + 1] = c; triangles[6 * i + 2] = b;
                triangles[6 * i + 3] = b; triangles[6 * i + 4] = c; triangles[6 * i + 5] = e;
            }

            mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>On the ground under the point, never under the terrain's own triangles, plus the lift.</summary>
        static Vector3 Lifted(SiteData site, Vector2 meters, float lift)
        {
            float half = site.SideMeters / 2f;
            float x = Mathf.Clamp(meters.x, 0f, site.SideMeters), y = Mathf.Clamp(meters.y, 0f, site.SideMeters);
            // Bilinear can dip below a triangle on steep cells: blend toward the cell top.
            float ground = Mathf.Lerp(HeightAt(site, x, y), CellTopAt(site, x, y), 0.5f);
            return new Vector3(x - half, ground + lift, y - half);
        }
    }
}
