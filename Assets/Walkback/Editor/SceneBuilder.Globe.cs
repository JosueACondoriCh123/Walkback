// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Walkback.EditorTools
{
    /// <summary>The globe opening and the site jump. Both start hidden: they only show at runtime.</summary>
    public static partial class SceneBuilder
    {
        static partial void BuildGlobe(WalkbackApp app)
        {
            Material globeMaterial = MakeMaterial("Globe", "Universal Render Pipeline/Lit", Color.white, 0.05f);
            Material coreMaterial = MakeMaterial("GlobeSiteLight", "Universal Render Pipeline/Unlit", new Color(1f, 0.96f, 0.82f), 0f);
            Material pillarMaterial = MakeMaterial("GlobeSitePillar", "Universal Render Pipeline/Unlit", WalkbackTheme.Watch, 0f);
            Material panoramaMaterial = MakeMaterial("Panorama", "Universal Render Pipeline/Unlit", Color.white, 0f);
            Material backPlate = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelBack.mat");
            Material ui = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelUI.mat");

            // ---- Globe opening ----
            var root = new GameObject("GlobeOpening");
            var opening = root.AddComponent<GlobeOpening>();
            opening.app = app;

            var visual = new GameObject("GlobeVisual");
            visual.transform.SetParent(root.transform, false);
            opening.visual = visual;

            var pivot = new GameObject("Globe", typeof(MeshFilter), typeof(MeshRenderer), typeof(SphereCollider));
            pivot.transform.SetParent(visual.transform, false);
            pivot.transform.position = opening.center;
            var globeRenderer = pivot.GetComponent<MeshRenderer>();
            globeRenderer.sharedMaterial = globeMaterial;
            globeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            pivot.GetComponent<SphereCollider>().radius = opening.radius;
            opening.pivot = pivot.transform;
            opening.globeRenderer = globeRenderer;

            // The site light: a bright core on the surface and a short amber pillar standing out of it.
            var beacon = new GameObject("SiteLight");
            beacon.transform.SetParent(pivot.transform, false);
            opening.beacon = beacon.transform;
            var core = GlobePrimitive(PrimitiveType.Sphere, "Core", beacon.transform, coreMaterial);
            core.localScale = Vector3.one * 0.024f;
            opening.beaconCore = core;
            var pillar = GlobePrimitive(PrimitiveType.Cylinder, "Pillar", beacon.transform, pillarMaterial);
            pillar.localScale = new Vector3(0.006f, 0.035f, 0.006f);
            pillar.localPosition = new Vector3(0f, 0.035f, 0f);

            Color text = WalkbackTheme.Text, soft = WalkbackTheme.TextSoft, watch = WalkbackTheme.Watch;
            opening.title = GlobeLabel.Create(visual.transform, "Title", 820f, 230f, new[] { 110, 40 }, new[] { text, soft }, backPlate, ui, TextAnchor.MiddleCenter);
            opening.title.transform.position = new Vector3(0f, 1.63f, 1.0f);
            opening.hint = GlobeLabel.Create(visual.transform, "Hint", 860f, 230f, new[] { 34, 30, 26, 24 }, new[] { text, text, watch, soft }, backPlate, ui, TextAnchor.MiddleCenter);
            opening.hint.transform.position = new Vector3(0f, 0.93f, 0.62f);
            opening.siteLabel = GlobeLabel.Create(visual.transform, "SiteLabel", 330f, 64f, new[] { 30 }, new[] { text }, backPlate, ui, TextAnchor.MiddleCenter);
            opening.title.SetLines("WALKBACK", "The safest way to go far.");
            opening.hint.SetLines("Point at the light and press to land", "GALE CRATER", "", "");
            opening.siteLabel.SetLines("GALE CRATER");

            visual.SetActive(false);
            app.globe = opening;

            // ---- Site jump ----
            var jumpRoot = new GameObject("SiteJump");
            var jump = jumpRoot.AddComponent<SiteJump>();
            jump.app = app;
            var sphere = new GameObject("PanoramaSphere", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            sphere.transform.SetParent(jumpRoot.transform, false);
            var sphereRenderer = sphere.GetComponent<MeshRenderer>();
            sphereRenderer.sharedMaterial = panoramaMaterial;
            sphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
            sphere.SetActive(false);
            jump.sphere = sphere;

            jump.card = GlobeLabel.Create(jumpRoot.transform, "JumpCard", 1000f, 230f, new[] { 46, 26, 30 }, new[] { text, soft, watch }, backPlate, ui, TextAnchor.MiddleCenter);
            jump.card.SetLines("Site", "Photo credit", SiteJump.ReturnHint);
            jump.card.gameObject.SetActive(false);
            jump.noPanorama = GlobeLabel.Create(jumpRoot.transform, "NoPanoramaCard", 900f, 170f, new[] { 42, 30 }, new[] { watch, text }, backPlate, ui, TextAnchor.MiddleCenter);
            jump.noPanorama.SetLines(SiteJump.NoPanoramaText, "");
            jump.noPanorama.gameObject.SetActive(false);
            app.siteJump = jump;
        }

        static Transform GlobePrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
