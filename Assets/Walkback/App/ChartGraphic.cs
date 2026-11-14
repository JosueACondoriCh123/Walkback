// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Walkback
{
    /// <summary>
    /// Flat shapes on a board's canvas: bars, an area, lines, a disc. One graphic holds one chart, so
    /// a chart is one mesh and one draw call. Coordinates are pixels (= millimeters) from the top-left
    /// corner of its own rectangle, y down, like the rest of the board. It is drawn with the board's
    /// Walkback/UI material, which never writes alpha (no holes to the room in passthrough).
    /// The shapes are not saved in the scene: whoever owns the chart draws it again after a load.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ChartGraphic : MaskableGraphic
    {
        readonly List<Vector2> points = new List<Vector2>();
        readonly List<Color32> colors = new List<Color32>();

        /// <summary>How many triangles the chart has now.</summary>
        public int TriangleCount { get { return points.Count / 3; } }

        /// <summary>Removes every shape (call Apply to show the change).</summary>
        public void Clear()
        {
            points.Clear();
            colors.Clear();
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            Color32 c32 = color;
            points.Add(a); points.Add(b); points.Add(c);
            colors.Add(c32); colors.Add(c32); colors.Add(c32);
        }

        /// <summary>Any four corners, in order around the shape.</summary>
        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            Triangle(a, b, c, color);
            Triangle(a, c, d, color);
        }

        public void Box(float x, float y, float width, float height, Color color)
        {
            if (width <= 0f || height <= 0f) return;
            Quad(new Vector2(x, y), new Vector2(x + width, y), new Vector2(x + width, y + height), new Vector2(x, y + height), color);
        }

        /// <summary>A straight stroke from a to b, this thick.</summary>
        public void Line(Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 along = b - a;
            if (along.sqrMagnitude < 1e-8f) return;
            Vector2 side = new Vector2(-along.y, along.x).normalized * (thickness / 2f);
            Quad(a + side, b + side, b - side, a - side, color);
        }

        public void Disc(Vector2 center, float radius, Color color, int segments = 40)
        {
            Vector2 previous = center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Triangle(center, previous, next, color);
                previous = next;
            }
        }

        /// <summary>Shows the shapes added since Clear.</summary>
        public void Apply()
        {
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            // The rectangle's pivot is its top-left corner (the board places everything that way): x right, y down.
            Rect r = rectTransform.rect;
            float left = r.xMin, top = r.yMax;
            UIVertex vertex = UIVertex.simpleVert;
            for (int i = 0; i < points.Count; i++)
            {
                vertex.position = new Vector3(left + points[i].x, top - points[i].y, 0f);
                vertex.color = colors[i];
                vertex.uv0 = Vector2.zero;
                vh.AddVert(vertex);
            }
            for (int i = 0; i + 2 < points.Count; i += 3) vh.AddTriangle(i, i + 1, i + 2);
        }
    }
}
