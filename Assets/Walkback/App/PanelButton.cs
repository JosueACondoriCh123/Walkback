// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Walkback
{
    /// <summary>
    /// A button on a world-space board: a colored box, a label and a BoxCollider that matches it,
    /// so the pointer (hand, controller or mouse) can hover and press it through IPointerTarget.
    /// It lights up on hover, flashes and clicks when pressed (guion §18.1: every button answers),
    /// and shows amber while it is the selected option (the current layer, the chosen stop).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PanelButton : MonoBehaviour, IPointerTarget
    {
        public static readonly Color NormalBack = new Color(0.20f, 0.22f, 0.26f);
        public static readonly Color HoverBack = new Color(0.34f, 0.37f, 0.43f);
        public static readonly Color SelectedBack = WalkbackTheme.Watch;
        public static readonly Color DisabledBack = new Color(0.13f, 0.14f, 0.16f);
        public static readonly Color SelectedText = new Color(0.06f, 0.06f, 0.07f);
        public static readonly Color DisabledText = new Color(0.50f, 0.52f, 0.55f);

        public Image background;
        public Text label;

        /// <summary>What a press does. Set at runtime (MissionPanel.Bind): delegates are not saved in the scene.</summary>
        public Action onClick;

        public bool Selected { get; private set; }
        public bool Hovered { get; private set; }
        public bool Interactable { get; private set; } = true;
        /// <summary>Presses received (the automatic test reads it).</summary>
        public int Presses { get; private set; }

        float flash;

        public void SetSelected(bool selected)
        {
            Selected = selected;
            Refresh();
        }

        public void SetInteractable(bool interactable)
        {
            Interactable = interactable;
            if (!interactable) Hovered = false;
            Refresh();
        }

        public void OnPointerEnter()
        {
            if (!Interactable) return;
            Hovered = true;
            Refresh();
        }

        public void OnPointerExit()
        {
            Hovered = false;
            Refresh();
        }

        public void OnPointerClick(Vector3 worldPoint)
        {
            if (!Interactable) return;
            Presses++;
            // The flash fades in Update, which only runs in play mode: in the editor it would stay lit.
            if (Application.isPlaying) flash = 0.18f;
            Refresh();
            PanelSound.Click();
            if (onClick != null) onClick();
        }

        void Update()
        {
            if (flash <= 0f) return;
            flash -= Time.unscaledDeltaTime;
            Refresh();
        }

        public void Refresh()
        {
            Color back = !Interactable ? DisabledBack : Selected ? SelectedBack : Hovered ? HoverBack : NormalBack;
            if (flash > 0f) back = Color.Lerp(back, Color.white, 0.55f);
            if (background != null) background.color = back;
            if (label != null) label.color = !Interactable ? DisabledText : (Selected || flash > 0f) ? SelectedText : WalkbackTheme.Text;
        }

        /// <summary>
        /// Builds a button inside a board canvas (1 canvas pixel = 1 mm). x, y from the top-left
        /// corner of the board, in pixels. The collider is 20 mm deep and matches the box.
        /// </summary>
        public static PanelButton Create(RectTransform parent, string name, string text, Font font, Material material, int fontSize, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(BoxCollider), typeof(PanelButton));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (material != null) image.material = material;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
            var label = labelObject.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            if (material != null) label.material = material;

            // The rect spans x 0..w and y -h..0 around its top-left pivot.
            var box = go.GetComponent<BoxCollider>();
            box.size = new Vector3(w, h, 20f);
            box.center = new Vector3(w / 2f, -h / 2f, 0f);

            var button = go.GetComponent<PanelButton>();
            button.background = image;
            button.label = label;
            button.Refresh();
            return button;
        }
    }

    /// <summary>A short click made in code (no audio file): the press is heard as well as seen.</summary>
    public static class PanelSound
    {
        static AudioSource source;
        static AudioClip clip;

        public static void Click()
        {
            if (!Application.isPlaying) return;
            try
            {
                if (source == null)
                {
                    var go = new GameObject("PanelSound");
                    source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    clip = MakeClick();
                }
                source.PlayOneShot(clip, 0.5f);
            }
            catch (Exception e)
            {
                WalkbackLog.Warn("Panel click sound failed: " + e.Message);
            }
        }

        static AudioClip MakeClick()
        {
            const int rate = 44100;
            int samples = rate * 60 / 1000;
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 1500f * t) * Mathf.Exp(-t * 70f) * 0.8f;
            }
            var made = AudioClip.Create("PanelClick", samples, 1, rate, false);
            made.SetData(data, 0);
            return made;
        }
    }
}
