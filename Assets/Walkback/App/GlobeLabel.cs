// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;
using UnityEngine.UI;

namespace Walkback
{
    /// <summary>
    /// A small world-space card: an opaque dark back plate and lines of text on it, 1 canvas
    /// pixel = 1 mm, readable on a projector (text always on a dark box, guion §18.1).
    /// Built at edit time by the scene builder, so it is saved in the scene.
    /// </summary>
    public class GlobeLabel : MonoBehaviour
    {
        public Text[] lines;
        public Transform backPlate;
        public RectTransform canvasRect;

        /// <summary>Creates a card of width x height millimeters with one Text per entry of sizes (font px = mm).</summary>
        public static GlobeLabel Create(Transform parent, string name, float widthMm, float heightMm, int[] sizes, Color[] colors,
            Material backPlateMaterial, Material uiMaterial, TextAnchor alignment)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var label = root.AddComponent<GlobeLabel>();

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "BackPlate";
            Object.DestroyImmediate(back.GetComponent<Collider>());
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, 0.003f);
            back.transform.localScale = new Vector3(widthMm / 1000f, heightMm / 1000f, 1f);
            back.GetComponent<MeshRenderer>().sharedMaterial = backPlateMaterial;
            back.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            label.backPlate = back.transform;

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(widthMm, heightMm);
            rect.localScale = Vector3.one * 0.001f;
            label.canvasRect = rect;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float margin = 24f;
            float total = 0f;
            foreach (int s in sizes) total += s * 1.25f;
            float y = (heightMm - total) / 2f;
            label.lines = new Text[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                float h = sizes[i] * 1.25f;
                var go = new GameObject("Line" + i, typeof(RectTransform), typeof(Text));
                var r = (RectTransform)go.transform;
                r.SetParent(rect, false);
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(margin, -y);
                r.sizeDelta = new Vector2(widthMm - 2f * margin, h);
                var text = go.GetComponent<Text>();
                text.font = font;
                text.text = " ";
                text.fontSize = sizes[i];
                text.fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal;
                text.color = colors[i];
                text.alignment = alignment;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;
                if (uiMaterial != null) text.material = uiMaterial;
                label.lines[i] = text;
                y += h;
            }
            return label;
        }

        /// <summary>Sets every line; an empty or null entry leaves the line blank.</summary>
        public void SetLines(params string[] texts)
        {
            for (int i = 0; i < lines.Length; i++)
                lines[i].text = i < texts.Length && !string.IsNullOrEmpty(texts[i]) ? texts[i] : " ";
        }

        /// <summary>Turns the card so its text reads from <paramref name="eye"/> (canvas +z points away from the viewer).</summary>
        public void Face(Vector3 eye)
        {
            Vector3 away = transform.position - eye;
            away.y = 0f;
            if (away.sqrMagnitude < 1e-6f) return;
            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}
