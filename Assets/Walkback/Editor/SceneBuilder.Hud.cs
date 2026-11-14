// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Walkback.EditorTools
{
    /// <summary>The helmet HUD with its 2D map, and the 3D map on the wrist. Both start hidden.</summary>
    public static partial class SceneBuilder
    {
        static partial void BuildHud(WalkbackApp app)
        {
            Material backPlate = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelBack.mat");
            Material ui = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelUI.mat");
            Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Terrain.mat");
            Material mapMaterial = MakeMaterial("HudMap", "Universal Render Pipeline/Unlit", Color.white, 0f);
            Material safe = MakeMaterial("HudSafe", "Universal Render Pipeline/Unlit", WalkbackTheme.Safe, 0f);
            Material watch = MakeMaterial("HudWatch", "Universal Render Pipeline/Unlit", WalkbackTheme.Watch, 0f);
            Material live = MakeMaterial("HudLive", "Universal Render Pipeline/Unlit", WalkbackTheme.Live, 0f);
            Material mark = MakeMaterial("HudMark", "Universal Render Pipeline/Unlit", Color.white, 0f);
            Color text = WalkbackTheme.Text, soft = WalkbackTheme.TextSoft;

            // ---- Helmet HUD ----
            var root = new GameObject("HelmetHud");
            var hud = root.AddComponent<HelmetHud>();
            hud.app = app;
            var content = new GameObject("HudContent").transform;
            content.SetParent(root.transform, false);
            hud.content = content;

            hud.oxygenCard = Card(content, "Oxygen", 320f, 130f, new[] { 60, 24 }, new[] { WalkbackTheme.Safe, soft }, backPlate, ui, new Vector3(-0.33f, 0.22f, 0f));
            hud.returnCard = Card(content, "Return", 360f, 130f, new[] { 60, 24 }, new[] { WalkbackTheme.Live, soft }, backPlate, ui, new Vector3(0.33f, 0.22f, 0f));
            hud.nextCard = Card(content, "Next", 440f, 130f, new[] { 30, 26, 24 }, new[] { WalkbackTheme.Watch, text, soft }, backPlate, ui, new Vector3(-0.32f, -0.17f, 0f));
            hud.estimateTag = Card(content, "Estimate", 760f, 42f, new[] { 20 }, new[] { WalkbackTheme.Watch }, backPlate, ui, new Vector3(0f, 0.33f, 0f));
            hud.warningCard = Card(content, "TurnBack", 560f, 96f, new[] { 56 }, new[] { WalkbackTheme.Danger }, backPlate, ui, new Vector3(0f, 0.10f, 0f));
            foreach (var card in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard, hud.estimateTag, hud.warningCard })
                foreach (var line in card.lines) line.alignment = TextAnchor.MiddleCenter;
            hud.warningCard.gameObject.SetActive(false);

            // The 2D map in the bottom-right corner.
            var mapObject = new GameObject("MiniMap");
            mapObject.transform.SetParent(content, false);
            mapObject.transform.localPosition = new Vector3(0.34f, -0.09f, 0f);
            var map = mapObject.AddComponent<MiniMap>();
            float s = MiniMap.SizeMeters;
            HudQuad("Frame", mapObject.transform, backPlate, new Vector3(0f, 0f, 0.002f), new Vector3(s + 0.012f, s + 0.012f, 1f));
            map.photoQuad = HudQuad("Photo", mapObject.transform, mapMaterial, Vector3.zero, new Vector3(s, s, 1f)).GetComponent<MeshRenderer>();
            map.line = HudQuad("LineToNext", mapObject.transform, mark, new Vector3(0f, 0f, -0.002f), new Vector3(0.05f, 0.0025f, 1f));
            map.nextRing = HudQuad("NextRing", mapObject.transform, mark, new Vector3(0f, 0f, -0.0025f), new Vector3(0.016f, 0.016f, 1f));
            map.habitat = HudQuad("Habitat", mapObject.transform, safe, new Vector3(0f, 0f, -0.003f), new Vector3(0.012f, 0.012f, 1f));
            map.you = HudQuad("You", mapObject.transform, live, new Vector3(0f, 0f, -0.005f), new Vector3(0.013f, 0.013f, 1f));
            map.scaleBar = HudQuad("ScaleBar", mapObject.transform, mark, new Vector3(0f, 0f, -0.003f), new Vector3(0.05f, 0.003f, 1f));
            map.stopMaterial = watch;
            map.caption = Card(mapObject.transform, "MapCaption", 340f, 34f, new[] { 17 }, new[] { soft }, backPlate, ui, new Vector3(0f, -s / 2f - 0.026f, 0f));
            map.caption.lines[0].alignment = TextAnchor.MiddleCenter;
            map.caption.SetLines("North up");
            hud.miniMap = map;
            content.gameObject.SetActive(false);
            app.hud = hud;

            // ---- 3D map on the wrist ----
            var wristObject = new GameObject("WristMap");
            var wrist = wristObject.AddComponent<WristMap>();
            var model = new GameObject("Model").transform;
            model.SetParent(wristObject.transform, false);
            wrist.model = model;
            var copy = new GameObject("TerrainCopy", typeof(MeshFilter), typeof(MeshRenderer));
            copy.transform.SetParent(model, false);
            wrist.terrainCopy = copy.GetComponent<MeshFilter>();
            wrist.terrainRenderer = copy.GetComponent<MeshRenderer>();
            wrist.terrainRenderer.sharedMaterial = terrainMaterial;
            wrist.terrainRenderer.shadowCastingMode = ShadowCastingMode.Off;
            wrist.youPin = HudPrimitive(PrimitiveType.Sphere, "YouPin", model, live, new Vector3(30f, 30f, 30f));
            wrist.habitatPin = HudPrimitive(PrimitiveType.Cylinder, "HabitatPin", model, safe, new Vector3(18f, 22f, 18f));
            wrist.stopMaterial = watch;
            wrist.label = Card(wristObject.transform, "WristLabel", 200f, 26f, new[] { 13 }, new[] { text }, backPlate, ui, new Vector3(0f, 0.03f, 0.085f));
            wrist.label.lines[0].alignment = TextAnchor.MiddleCenter;
            wrist.label.SetLines("YOU");
            wristObject.SetActive(false);
            app.wristMap = wrist;
        }

        static GlobeLabel Card(Transform parent, string name, float widthMm, float heightMm, int[] sizes, Color[] colors, Material backPlate, Material ui, Vector3 localPosition)
        {
            var card = GlobeLabel.Create(parent, name, widthMm, heightMm, sizes, colors, backPlate, ui, TextAnchor.MiddleLeft);
            card.transform.localPosition = localPosition;
            return card;
        }

        static Transform HudQuad(string name, Transform parent, Material material, Vector3 localPosition, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        static Transform HudPrimitive(PrimitiveType type, string name, Transform parent, Material material, Vector3 localScale)
        {
            var t = GlobePrimitive(type, name, parent, material);
            t.localScale = localScale;
            return t;
        }
    }
}
