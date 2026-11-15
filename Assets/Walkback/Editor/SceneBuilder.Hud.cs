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

            // The four corners sit out at the edges (0.50 m to each side at 1.2 m, about 23°; they were at 0.37 m
            // and the visor read as one block around the pillar): the middle of the view stays open for Mars,
            // the pillar and the route. Every card's center is within 26° of the middle.
            hud.oxygenCard = Card(content, "Oxygen", 320f, 150f, new[] { 60, 24 }, new[] { WalkbackTheme.Safe, soft }, backPlate, ui, new Vector3(-0.50f, 0.24f, 0f));
            hud.returnCard = Card(content, "Return", 340f, 150f, new[] { 56, 22, 22 }, new[] { WalkbackTheme.Safe, soft, text }, backPlate, ui, new Vector3(0.51f, 0.24f, 0f));
            hud.nextCard = Card(content, "Next", 440f, 140f, new[] { 30, 24, 22 }, new[] { WalkbackTheme.Watch, text, soft }, backPlate, ui, new Vector3(-0.47f, -0.19f, 0f));
            hud.estimateTag = Card(content, "Estimate", 760f, 42f, new[] { 20 }, new[] { WalkbackTheme.Watch }, backPlate, ui, HelmetHud.EstimateAbove);
            hud.warningCard = Card(content, "TurnBack", 560f, 96f, new[] { 56 }, new[] { WalkbackTheme.Danger }, backPlate, ui, new Vector3(0f, 0.10f, 0f));
            foreach (var card in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard, hud.estimateTag, hud.warningCard })
                foreach (var line in card.lines) line.alignment = TextAnchor.MiddleCenter;
            hud.warningCard.gameObject.SetActive(false);

            // The oxygen bar under the number: a dark track, the fill (its colour is set with the number)
            // and a white tick where the reserve starts. The two text lines move up to make room.
            Material track = MakeMaterial("HudTrack", "Universal Render Pipeline/Unlit", new Color(0.26f, 0.28f, 0.31f), 0f);
            foreach (var line in hud.oxygenCard.lines) ((RectTransform)line.transform).anchoredPosition += new Vector2(0f, 14f);
            const float barY = -0.051f;
            float barWide = HelmetHud.OxygenBarMeters, barTall = HelmetHud.OxygenBarHeightMeters;
            HudQuad("OxygenTrack", hud.oxygenCard.transform, track, new Vector3(0f, barY, -0.001f), new Vector3(barWide, barTall, 1f));
            hud.oxygenFill = HudQuad("OxygenFill", hud.oxygenCard.transform, mark, new Vector3(0f, barY, -0.002f), new Vector3(barWide, barTall, 1f));
            hud.oxygenFillRenderer = hud.oxygenFill.GetComponent<MeshRenderer>();
            float reserveAt = -barWide / 2f + barWide * HudEstimate.ReserveLiters / HudEstimate.TankLiters;
            HudQuad("OxygenReserve", hud.oxygenCard.transform, mark, new Vector3(reserveAt, barY, -0.003f), new Vector3(0.003f, barTall + 0.008f, 1f));

            // The compass tape between the two top cards, and the heading over it. Mission mode only: both
            // start hidden. Not above the cards: at 5 m the next stop's name (3.35 m up on its pillar) is
            // seen 14° to 19° above the horizon, just over the cards, and a tape there covered it.
            var compassObject = new GameObject("Compass");
            compassObject.transform.SetParent(content, false);
            compassObject.transform.localPosition = new Vector3(0f, 0.195f, 0f);
            var compass = compassObject.AddComponent<HudCompass>();
            float tapeWide = HudCompass.WidthMeters, tapeTall = HudCompass.HeightMeters;
            HudQuad("Tape", compassObject.transform, backPlate, new Vector3(0f, 0f, 0.002f), new Vector3(tapeWide, tapeTall, 1f));
            var tickList = new System.Collections.Generic.List<Transform>();
            var tickAngles = new System.Collections.Generic.List<float>();
            for (float angle = 0f; angle < 359.9f; angle += HudCompass.TickStepDegrees)
            {
                // Where a letter stands there is no tick.
                if (Mathf.Abs(Mathf.Repeat(angle, HudCompass.LetterStepDegrees)) < 0.01f) continue;
                tickList.Add(HudQuad("Tick" + angle.ToString("000"), compassObject.transform, mark, new Vector3(0f, -0.010f, -0.001f), new Vector3(0.003f, 0.016f, 1f)));
                tickAngles.Add(angle);
            }
            compass.ticks = tickList.ToArray();
            compass.tickAngles = tickAngles.ToArray();
            compass.letters = new GlobeLabel[HudCompass.Letters.Length];
            for (int i = 0; i < compass.letters.Length; i++)
            {
                float wide = HudCompass.LetterHalfMeters * 2000f;
                var letter = Card(compassObject.transform, "Letter" + HudCompass.Letters[i], wide, 34f, new[] { 22 },
                    new[] { i % 2 == 0 ? text : soft }, backPlate, ui, new Vector3(0f, 0.004f, -0.001f));
                // A card this narrow has no room for the label's side margins: the letter takes the whole width.
                var letterRect = (RectTransform)letter.lines[0].transform;
                letterRect.anchoredPosition = new Vector2(0f, letterRect.anchoredPosition.y);
                letterRect.sizeDelta = new Vector2(wide, letterRect.sizeDelta.y);
                letter.lines[0].alignment = TextAnchor.MiddleCenter;
                letter.lines[0].horizontalOverflow = HorizontalWrapMode.Overflow;
                letter.SetLines(HudCompass.Letters[i]);
                compass.letters[i] = letter;
            }
            // The notch in the middle is you (cyan); the bar is the next stop (amber), in front of everything else.
            HudQuad("NotchTop", compassObject.transform, live, new Vector3(0f, tapeTall / 2f - 0.004f, -0.003f), new Vector3(0.005f, 0.008f, 1f));
            HudQuad("NotchBottom", compassObject.transform, live, new Vector3(0f, -tapeTall / 2f + 0.004f, -0.003f), new Vector3(0.005f, 0.008f, 1f));
            compass.stopMarker = HudQuad("NextStopBar", compassObject.transform, watch, new Vector3(0f, 0f, -0.004f), new Vector3(0.010f, tapeTall, 1f));
            compass.Show(0f, false, 0f);
            compassObject.SetActive(false);
            hud.compass = compass;

            hud.headingCard = Card(content, "Heading", 170f, 36f, new[] { 20 }, new[] { text }, backPlate, ui, new Vector3(0f, 0.250f, 0f));
            hud.headingCard.lines[0].alignment = TextAnchor.MiddleCenter;
            hud.headingCard.SetLines(HudCompass.BearingText(0f));
            hud.headingCard.gameObject.SetActive(false);

            // The arrow to the next stop, on its own plate right of the NEXT card: one solid shape (a wide
            // head on a shaft), turning about the plate's center. It was three loose bars before.
            var arrowPlate = new GameObject("NextArrow").transform;
            arrowPlate.SetParent(content, false);
            arrowPlate.localPosition = new Vector3(-0.18f, -0.19f, 0f);   // right against the NEXT card
            HudQuad("Plate", arrowPlate, backPlate, new Vector3(0f, 0f, 0.002f), new Vector3(0.10f, 0.10f, 1f));
            var arrow = new GameObject("Arrow").transform;
            arrow.SetParent(arrowPlate, false);
            arrow.localPosition = new Vector3(0f, 0f, -0.002f);
            HudArrow("Shape", arrow, watch, 0.078f, 0.036f, 0.062f, 0.020f);
            arrowPlate.gameObject.SetActive(false);
            hud.arrowPlate = arrowPlate;
            hud.arrow = arrow;

            // The 2D map in the bottom-right corner.
            var mapObject = new GameObject("MiniMap");
            mapObject.transform.SetParent(content, false);
            mapObject.transform.localPosition = new Vector3(0.49f, -0.13f, 0f);
            var map = mapObject.AddComponent<MiniMap>();
            float s = MiniMap.SizeMeters;
            HudQuad("Frame", mapObject.transform, backPlate, new Vector3(0f, 0f, 0.002f), new Vector3(s + 0.012f, s + 0.012f, 1f));
            map.photoQuad = HudQuad("Photo", mapObject.transform, mapMaterial, Vector3.zero, new Vector3(s, s, 1f)).GetComponent<MeshRenderer>();
            map.line = HudQuad("LineToNext", mapObject.transform, mark, new Vector3(0f, 0f, -0.002f), new Vector3(0.05f, 0.0025f, 1f));
            map.nextRing = HudQuad("NextRing", mapObject.transform, mark, new Vector3(0f, 0f, -0.0025f), new Vector3(0.016f, 0.016f, 1f));
            map.habitat = HudQuad("Habitat", mapObject.transform, safe, new Vector3(0f, 0f, -0.003f), new Vector3(0.012f, 0.012f, 1f));
            map.you = HudQuad("You", mapObject.transform, live, new Vector3(0f, 0f, -0.005f), new Vector3(0.013f, 0.013f, 1f));
            // The heading point rides on the "you" dot: a triangle in front of it, toward where the astronaut looks
            // (dot + point read as one pointer; it was a thin bar before).
            map.youHeading = HudTriangle("Heading", map.you, live, new Vector3(0f, 0.45f, 0f), 1.15f, 0.95f);
            // A dark rim behind the mark, 2 mm all around: cyan alone fades on the bright sand of the photo.
            // The point's rim is its child, so it hides with it inside a 360° photo (there "you" is a dot).
            HudQuad("Outline", map.you, backPlate, new Vector3(0f, 0f, 0.0008f), new Vector3(1.3f, 1.3f, 1f));
            HudTriangle("Outline", map.youHeading, backPlate, new Vector3(0f, -0.13f, 0.0008f), 1.61f, 1.33f);
            map.youHeading.gameObject.SetActive(false);
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
            wrist.youNose = HudPrimitive(PrimitiveType.Cube, "YouNose", wrist.youPin, live, new Vector3(0.35f, 0.35f, 1.6f));
            wrist.youNose.localPosition = new Vector3(0f, 0f, 1.1f);
            wrist.youNose.gameObject.SetActive(false);
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

        /// <summary>
        /// A flat arrow pointing up (+y) in its parent's plane, centered on the parent's origin and seen from -z
        /// like a quad: shaft and head as one mesh, saved in the scene with it.
        /// </summary>
        static Transform HudArrow(string name, Transform parent, Material material, float length, float headLength, float headWidth, float shaftWidth)
        {
            float top = length / 2f, bottom = -length / 2f, neck = top - headLength, s = shaftWidth / 2f, h = headWidth / 2f;
            var vertices = new[]
            {
                new Vector3(-s, bottom, 0f), new Vector3(-s, neck, 0f), new Vector3(s, neck, 0f), new Vector3(s, bottom, 0f),
                new Vector3(-h, neck, 0f), new Vector3(0f, top, 0f), new Vector3(h, neck, 0f),
            };
            // Clockwise seen from -z (the side the HUD is looked at from), as Unity's quad.
            var triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6 };
            return HudMesh(name, parent, material, vertices, triangles, Vector3.zero);
        }

        /// <summary>A flat triangle pointing up (+y), its base centered on localPosition, seen from -z.</summary>
        static Transform HudTriangle(string name, Transform parent, Material material, Vector3 localPosition, float width, float height)
        {
            var vertices = new[] { new Vector3(-width / 2f, 0f, 0f), new Vector3(0f, height, 0f), new Vector3(width / 2f, 0f, 0f) };
            return HudMesh(name, parent, material, vertices, new[] { 0, 1, 2 }, localPosition);
        }

        static Transform HudMesh(string name, Transform parent, Material material, Vector3[] vertices, int[] triangles, Vector3 localPosition)
        {
            var mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
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
