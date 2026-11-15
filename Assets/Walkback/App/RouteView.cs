// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// Draws on the map: the safe route as an amber ribbon of light that draws itself, and the
    /// straight "as the crow flies" line as white dashes (guion §22.4, slide 2). Both are draped
    /// on the relief. Lives under MapRoot with an identity transform, so its meshes are in the
    /// terrain mesh's meters. It only draws: the route itself comes from session C's RoutePlanner.
    /// </summary>
    public class RouteView : MonoBehaviour
    {
        public Material lightMaterial;

        // Sizes in map meters (1 m of map = 1 mm on the table).
        public const float RibbonWidthMeters = 12f;
        public const float RibbonLiftMeters = 1.5f;
        public const float LineWidthMeters = 6f;
        public const float LineLiftMeters = 2.8f;
        public const float DashPeriodMeters = 20f;
        public const float SampleStepMeters = 2f;
        public const float DrawSeconds = 2.5f;

        public static readonly Color RouteColor = WalkbackTheme.Watch;
        public static readonly Color StraightColor = new Color(0.96f, 0.96f, 0.96f);

        /// <summary>For the panel's legend.</summary>
        public const string StraightLineLegend = "Dotted white: straight line between stops, not a walking route";

        SiteData site;
        MeshRenderer ribbon, line;
        Mesh ribbonMesh, lineMesh;
        MaterialPropertyBlock block;
        float progress = 1f;
        bool drawing;

        public bool HasRoute { get { return ribbon != null && ribbon.gameObject.activeSelf; } }
        public bool HasStraightLine { get { return line != null && line.gameObject.activeSelf; } }
        public float RouteLengthMeters { get; private set; }
        public float StraightLengthMeters { get; private set; }
        /// <summary>How much of the route is drawn, 0..1.</summary>
        public float Progress { get { return progress; } }
        public MeshRenderer RibbonRenderer { get { return ribbon; } }
        public MeshRenderer LineRenderer { get { return line; } }

        public void Bind(SiteData siteData)
        {
            site = siteData;
        }

        /// <summary>
        /// The safe route, in local meters (x east, y north). With <paramref name="animate"/>, it draws
        /// itself from the habitat in <see cref="DrawSeconds"/> (in Play only; at edit time it shows whole).
        /// </summary>
        public void ShowRoute(IList<Vector2> pointsMeters, bool animate = true)
        {
            if (site == null || pointsMeters == null || pointsMeters.Count < 2) { ClearRoute(); return; }
            ribbon = EnsurePart("SafeRoute", ribbon);
            ribbonMesh = RouteGeometry.BuildStrip(site, pointsMeters, RibbonWidthMeters, RibbonLiftMeters, SampleStepMeters, ribbonMesh);
            ribbon.GetComponent<MeshFilter>().sharedMesh = ribbonMesh;
            RouteLengthMeters = RouteGeometry.Length(pointsMeters);
            ribbon.gameObject.SetActive(true);
            drawing = animate && Application.isPlaying;
            SetProgress(drawing ? 0f : 1f);
            WalkbackLog.Info("Route shown: " + pointsMeters.Count + " points, " + RouteLengthMeters.ToString("F0") + " m");
        }

        /// <summary>The straight line through the waypoints (habitat, stops, habitat), white dashes.</summary>
        public void ShowStraightLine(IList<Vector2> waypointsMeters)
        {
            if (site == null || waypointsMeters == null || waypointsMeters.Count < 2) { ClearStraightLine(); return; }
            line = EnsurePart("StraightLine", line);
            lineMesh = RouteGeometry.BuildStrip(site, waypointsMeters, LineWidthMeters, LineLiftMeters, SampleStepMeters, lineMesh);
            line.GetComponent<MeshFilter>().sharedMesh = lineMesh;
            StraightLengthMeters = RouteGeometry.Length(waypointsMeters);
            line.gameObject.SetActive(true);
            Paint(line, StraightColor, 1f, Mathf.Max(1f, Mathf.Round(StraightLengthMeters / DashPeriodMeters)), 0.28f, 0f);
        }

        public void ClearRoute()
        {
            if (ribbon != null) ribbon.gameObject.SetActive(false);
            RouteLengthMeters = 0f;
            drawing = false;
        }

        public void ClearStraightLine()
        {
            if (line != null) line.gameObject.SetActive(false);
            StraightLengthMeters = 0f;
        }

        public void Clear()
        {
            ClearRoute();
            ClearStraightLine();
        }

        /// <summary>Shows the route up to this fraction (0..1) of its length.</summary>
        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            if (ribbon != null) Paint(ribbon, RouteColor, progress, 0f, 0.26f, 0.25f);
        }

        void Update()
        {
            if (!drawing) return;
            SetProgress(progress + Time.deltaTime / DrawSeconds);
            if (progress >= 1f) drawing = false;
        }

        void OnDestroy()
        {
            DestroySafe(ribbonMesh);
            DestroySafe(lineMesh);
        }

        MeshRenderer EnsurePart(string name, MeshRenderer existing)
        {
            if (existing != null) return existing;
            Transform child = transform.Find(name);
            GameObject go = child != null ? child.gameObject : new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = lightMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        void Paint(MeshRenderer target, Color color, float cut, float dashes, float border, float pulse)
        {
            if (block == null) block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetFloat("_Progress", cut);
            block.SetFloat("_DashCount", dashes);
            block.SetFloat("_Border", border);
            block.SetFloat("_Pulse", pulse);
            block.SetFloat("_Vertical", 0f);
            target.SetPropertyBlock(block);
        }

        static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
