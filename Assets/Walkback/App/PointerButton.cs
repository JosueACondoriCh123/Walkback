// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Walkback
{
    /// <summary>
    /// A small pressable sign that stands on its own in the room (not on the panel), e.g. RECENTER
    /// next to the table. Opaque plates (no holes in passthrough), white text, a frame that turns
    /// cyan when the pointer is on it and amber for a moment when pressed.
    /// </summary>
    public class PointerButton : MonoBehaviour, IPointerTarget
    {
        public static readonly Color FrameNormal = new Color(0.42f, 0.44f, 0.48f);
        const float FlashSeconds = 0.18f;

        public Renderer frame;
        public Renderer face;
        public Text label;

        /// <summary>Wired at runtime by whoever owns the action (not saved in the scene).</summary>
        public Action Clicked;

        public bool Hovered { get; private set; }
        public int Presses { get; private set; }

        float flashUntil = -1f;
        MaterialPropertyBlock block;

        public void OnPointerEnter()
        {
            Hovered = true;
            Paint();
        }

        public void OnPointerExit()
        {
            Hovered = false;
            Paint();
        }

        public void OnPointerClick(Vector3 worldPoint)
        {
            Presses++;
            flashUntil = Time.realtimeSinceStartup + FlashSeconds;
            Paint();
            WalkbackLog.Info("Button pressed: " + name);
            if (Clicked != null) Clicked();
        }

        void Start()
        {
            // Property blocks are not saved in the scene: color the plates again at runtime.
            TintFace();
            Paint();
        }

        void TintFace()
        {
            if (face == null) return;
            var faceBlock = new MaterialPropertyBlock();
            face.GetPropertyBlock(faceBlock);
            faceBlock.SetColor("_BaseColor", WalkbackTheme.CardBack);
            face.SetPropertyBlock(faceBlock);
        }

        void Update()
        {
            if (flashUntil > 0f && Time.realtimeSinceStartup >= flashUntil)
            {
                flashUntil = -1f;
                Paint();
            }
        }

        /// <summary>The frame color for the current state.</summary>
        public Color FrameColor
        {
            get
            {
                if (flashUntil > 0f && Time.realtimeSinceStartup < flashUntil) return WalkbackTheme.Watch;
                return Hovered ? WalkbackTheme.Live : FrameNormal;
            }
        }

        void Paint()
        {
            if (frame == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            frame.GetPropertyBlock(block);
            block.SetColor("_BaseColor", FrameColor);
            frame.SetPropertyBlock(block);
        }

        /// <summary>
        /// Builds the sign at edit time: frame and face (opaque quads), a world-space canvas with the
        /// label (1 canvas pixel = 1 mm) and a box collider the pointer can hit. Faces -z like the panel.
        /// </summary>
        public static PointerButton Create(string name, string text, Vector2 sizeMeters, Material plateMaterial, Material uiMaterial)
        {
            var root = new GameObject(name);
            var button = root.AddComponent<PointerButton>();

            var frameQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frameQuad.name = "Frame";
            UnityEngine.Object.DestroyImmediate(frameQuad.GetComponent<Collider>());
            frameQuad.transform.SetParent(root.transform, false);
            frameQuad.transform.localPosition = new Vector3(0f, 0f, 0.003f);
            frameQuad.transform.localScale = new Vector3(sizeMeters.x, sizeMeters.y, 1f);
            var frameRenderer = frameQuad.GetComponent<MeshRenderer>();
            frameRenderer.sharedMaterial = plateMaterial;
            frameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            const float border = 0.008f;
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face";
            UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, 0f, 0.0015f);
            face.transform.localScale = new Vector3(sizeMeters.x - 2f * border, sizeMeters.y - 2f * border, 1f);
            var faceRenderer = face.GetComponent<MeshRenderer>();
            faceRenderer.sharedMaterial = plateMaterial;
            faceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(sizeMeters.x * 1000f, sizeMeters.y * 1000f);
            rect.localScale = Vector3.one * 0.001f;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = Mathf.RoundToInt(sizeMeters.y * 1000f * 0.48f);
            label.fontStyle = FontStyle.Bold;
            label.color = WalkbackTheme.Text;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            if (uiMaterial != null) label.material = uiMaterial;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeMeters.x, sizeMeters.y, 0.02f);
            collider.center = new Vector3(0f, 0f, 0.002f);

            button.frame = frameRenderer;
            button.face = faceRenderer;
            button.label = label;
            // The face keeps one plate color; only the frame changes with the state.
            button.TintFace();
            button.Paint();
            return button;
        }
    }
}
