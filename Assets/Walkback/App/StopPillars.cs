// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Walkback
{
    /// <summary>
    /// Pillars of light on the map: the habitat (green) and every stop of the mission (amber), from
    /// site.json and stops.json. They grow when the map appears ("brotan"), carry a name label that
    /// reads at 1.5 m, and a stop pillar selects its stop when pressed (app.SelectStop). Lives under
    /// MapRoot with an identity transform, in map meters.
    /// </summary>
    public class StopPillars : MonoBehaviour
    {
        public Material lightMaterial;
        public Material labelMaterial;

        // Map meters (1 m of map = 1 mm on the table).
        public const float PillarHeightMeters = 110f;
        public const float StopRadiusMeters = 5f;
        public const float HabitatRadiusMeters = 8f;
        /// <summary>Stops closer than this get their labels stacked, each this much higher (map meters; 1 m on the table = 1000 m).</summary>
        public const float LabelSpacingMeters = 60f;
        public const float LabelStackMeters = 38f;
        public const float HitRadiusMeters = 18f;
        public const int LabelFontSize = 30;
        public const float GrowSeconds = 0.7f;
        public const float GrowStagger = 0.18f;

        public static readonly Color StopColor = WalkbackTheme.Watch;
        public static readonly Color HabitatColor = WalkbackTheme.Safe;

        readonly List<PillarView> stops = new List<PillarView>();
        public PillarView Habitat { get; private set; }
        public IList<PillarView> Stops { get { return stops; } }

        WalkbackApp app;
        float growStarted = -1f;

        /// <summary>
        /// (Re)builds the pillars from app.terrain.Site. With <paramref name="grow"/> (Play only),
        /// they grow one after the other; otherwise they appear whole.
        /// </summary>
        public void Build(WalkbackApp owner, bool grow)
        {
            Unsubscribe();
            app = owner;
            for (int i = transform.childCount - 1; i >= 0; i--) DestroySafe(transform.GetChild(i).gameObject);
            stops.Clear();
            Habitat = null;
            if (app == null || app.terrain == null || app.terrain.Site == null) return;

            SiteData site = app.terrain.Site;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Habitat = MakePillar(site, site.Habitat, "HABITAT", -1, HabitatColor, HabitatRadiusMeters, font);
            for (int i = 0; i < site.Stops.Length; i++)
            {
                StopInfo stop = site.Stops[i];
                string name = string.IsNullOrEmpty(stop.name) ? "Stop " + (i + 1) : stop.name;
                var pillar = MakePillar(site, new Vector2(stop.x, stop.y), name, i, StopColor, StopRadiusMeters, font);
                // Stops a few meters apart (Valle de la Luna and Nevado Sajama are 10 m apart): stack the labels.
                int near = 0;
                for (int j = 0; j < i; j++)
                    if (Vector2.Distance(new Vector2(site.Stops[j].x, site.Stops[j].y), new Vector2(stop.x, stop.y)) < LabelSpacingMeters) near++;
                pillar.labelLift = near * LabelStackMeters;
                pillar.Refresh();
                stops.Add(pillar);
            }

            app.StopSelected += OnStopSelected;
            OnStopSelected(app.SelectedStop);

            bool animate = grow && Application.isPlaying;
            growStarted = animate ? Time.time : -1f;
            SetGrowth(animate ? 0f : 1f);
            WalkbackLog.Info("Pillars built: habitat + " + stops.Count + " stops" + (site.StopsAreReal ? "" : " (TEST STOPs)"));
        }

        /// <summary>Every pillar at the same growth, 0..1 (the test and the end of the animation).</summary>
        public void SetGrowth(float value)
        {
            if (Habitat != null) Habitat.SetGrowth(value);
            foreach (var p in stops) p.SetGrowth(value);
        }

        /// <summary>Labels turn to this camera. Called every frame in Play; the test calls it before a screenshot.</summary>
        public void FaceCamera(Camera cam)
        {
            if (Habitat != null) Habitat.FaceCamera(cam);
            foreach (var p in stops) p.FaceCamera(cam);
        }

        void Update()
        {
            if (growStarted < 0f) return;
            float t = Time.time - growStarted;
            if (Habitat != null) Habitat.SetGrowth(t / GrowSeconds);
            for (int i = 0; i < stops.Count; i++) stops[i].SetGrowth((t - GrowStagger * (i + 1)) / GrowSeconds);
            if (t > GrowSeconds + GrowStagger * (stops.Count + 1)) growStarted = -1f;
        }

        void LateUpdate()
        {
            FaceCamera(ViewerCamera());
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        void OnStopSelected(int index)
        {
            for (int i = 0; i < stops.Count; i++) stops[i].SetSelected(i == index);
        }

        void Unsubscribe()
        {
            if (app != null) app.StopSelected -= OnStopSelected;
        }

        /// <summary>The camera the user sees through: the enabled one drawn last (desktop camera or the headset eye).</summary>
        public static Camera ViewerCamera()
        {
            Camera best = null;
            foreach (var cam in Camera.allCameras)
                if (cam.isActiveAndEnabled && (best == null || cam.depth > best.depth)) best = cam;
            return best;
        }

        PillarView MakePillar(SiteData site, Vector2 meters, string name, int index, Color color, float radius, Font font)
        {
            var root = new GameObject(index < 0 ? "Pillar_Habitat" : "Pillar_" + index);
            root.transform.SetParent(transform, false);
            Vector3 basePoint = RouteGeometry.ToMap(site, meters, 0f);
            root.transform.localPosition = basePoint;
            var pillar = root.AddComponent<PillarView>();
            pillar.StopIndex = index;
            pillar.app = app;
            pillar.baseColor = color;
            pillar.radiusMeters = radius;
            pillar.heightMeters = PillarHeightMeters;

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Beam";
            DestroySafe(beam.GetComponent<Collider>());
            beam.transform.SetParent(root.transform, false);
            var beamRenderer = beam.GetComponent<MeshRenderer>();
            beamRenderer.sharedMaterial = lightMaterial;
            beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beamRenderer.receiveShadows = false;
            pillar.beam = beamRenderer;

            if (index >= 0)
            {
                // A fat, invisible capsule: easy to hit with a ray from 1.5 m.
                var hitObject = new GameObject("Hit");
                hitObject.transform.SetParent(root.transform, false);
                var capsule = hitObject.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.radius = HitRadiusMeters;
                capsule.height = PillarHeightMeters + 2f * HitRadiusMeters;
                capsule.center = new Vector3(0f, PillarHeightMeters / 2f, 0f);
                pillar.hit = capsule;
            }

            MakeLabel(pillar, name, font);
            pillar.Refresh();
            return pillar;
        }

        void MakeLabel(PillarView pillar, string text, Font font)
        {
            var canvasObject = new GameObject("Label", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var rect = (RectTransform)canvasObject.transform;
            rect.SetParent(pillar.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;

            var boxObject = new GameObject("Box", typeof(RectTransform), typeof(Image));
            var boxRect = (RectTransform)boxObject.transform;
            boxRect.SetParent(rect, false);
            var box = boxObject.GetComponent<Image>();
            box.color = WalkbackTheme.PanelBack;
            box.raycastTarget = false;
            if (labelMaterial != null) box.material = labelMaterial;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(rect, false);
            var label = textObject.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = LabelFontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.color = WalkbackTheme.Text;
            if (labelMaterial != null) label.material = labelMaterial;

            float width = Mathf.Max(label.preferredWidth, text.Length * LabelFontSize * 0.55f) + 28f;
            float height = LabelFontSize + 18f;
            rect.sizeDelta = new Vector2(width, height);
            // Pivot at the bottom center: the label sits on top of the pillar.
            rect.pivot = new Vector2(0.5f, 0f);
            Stretch(boxRect);
            Stretch(textRect);

            pillar.labelRoot = rect;
            pillar.label = label;
            pillar.labelBox = box;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
