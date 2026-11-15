// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Walkback.EditorTools
{
    /// <summary>Mission mode: Mars at real scale, the route on the ground, the pillar, the stop card, the floor border and the fade curtain. All hidden at start.</summary>
    public static partial class SceneBuilder
    {
        static partial void BuildMission(WalkbackApp app)
        {
            try
            {
                Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Terrain.mat");
                Material light = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/RouteLight.mat");
                Material backPlate = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelBack.mat");
                Material ui = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelUI.mat");
                Material borderMaterial = MakeMaterial("MissionBorder", "Universal Render Pipeline/Unlit", WalkbackTheme.Watch, 0f);
                // Without the fade shader the mode still works: the changes are immediate instead of faded.
                Material fade = Shader.Find("Walkback/Fade") != null ? MakeMaterial("MissionFade", "Walkback/Fade", new Color(0.02f, 0.02f, 0.025f, 1f), 0f) : null;
                Color text = WalkbackTheme.Text, soft = WalkbackTheme.TextSoft;

                var root = new GameObject("MissionMode");
                var mission = root.AddComponent<MissionMode>();
                mission.app = app;

                // Everything at real scale lives under Mars, in the terrain mesh's meters.
                var mars = new GameObject("Mars").transform;
                mars.SetParent(root.transform, false);
                mission.mars = mars;

                var terrainObject = new GameObject("TerrainRealScale", typeof(MeshFilter), typeof(MeshRenderer));
                terrainObject.transform.SetParent(mars, false);
                mission.terrainCopy = terrainObject.GetComponent<MeshFilter>();
                mission.terrainRenderer = terrainObject.GetComponent<MeshRenderer>();
                mission.terrainRenderer.sharedMaterial = terrainMaterial;
                mission.terrainRenderer.shadowCastingMode = ShadowCastingMode.Off;

                var routeObject = new GameObject("RouteOnTheGround", typeof(MeshFilter), typeof(MeshRenderer));
                routeObject.transform.SetParent(mars, false);
                mission.routeFilter = routeObject.GetComponent<MeshFilter>();
                mission.routeRenderer = routeObject.GetComponent<MeshRenderer>();
                mission.routeRenderer.sharedMaterial = light;
                mission.routeRenderer.shadowCastingMode = ShadowCastingMode.Off;
                mission.routeRenderer.receiveShadows = false;

                Transform pillar = GlobePrimitive(PrimitiveType.Cylinder, "StopPillar", mars, light);
                pillar.localScale = new Vector3(MissionMode.PillarWidthMeters, MissionMode.PillarHeightMeters / 2f, MissionMode.PillarWidthMeters);
                mission.pillar = pillar;
                mission.pillarRenderer = pillar.GetComponent<MeshRenderer>();

                // Read from 5 m: 90 mm letters are about 1° tall there (17 mm at 1.2 m is 0.8°).
                mission.pillarLabel = Card(mars, "PillarLabel", 1500f, 230f, new[] { 90, 50 }, new[] { WalkbackTheme.Watch, text }, backPlate, ui, Vector3.zero);
                foreach (var line in mission.pillarLabel.lines) line.alignment = TextAnchor.MiddleCenter;
                mission.pillarLabel.SetLines("Stop", " ");

                // The stop's card on arrival, read from 1.5 m or less: 24 mm letters and up. 860 mm wide (its longest
                // real line is 650 mm): from 1.5 m it fits between the visor's NEXT card and its mini map.
                mission.stopCard = Card(root.transform, "StopCard", 860f, 250f, new[] { 46, 24, 28, 28, 24 },
                    new[] { WalkbackTheme.Watch, soft, text, text, soft }, backPlate, ui, Vector3.zero);
                foreach (var line in mission.stopCard.lines) line.alignment = TextAnchor.MiddleCenter;
                mission.stopCard.SetLines("Stop", " ", " ", " ", " ");
                mission.stopCard.gameObject.SetActive(false);

                // The free space on the floor: a square of four thin opaque strips, 3 x 3 m.
                var border = new GameObject("FreeSpaceBorder").transform;
                border.SetParent(root.transform, false);
                float side = MissionMode.FreeSpaceMeters, half = side / 2f, width = 0.04f, lift = 0.01f;
                Transform north = HudQuad("North", border, borderMaterial, new Vector3(0f, lift, half), new Vector3(side, width, 1f));
                Transform south = HudQuad("South", border, borderMaterial, new Vector3(0f, lift, -half), new Vector3(side, width, 1f));
                Transform east = HudQuad("East", border, borderMaterial, new Vector3(half, lift, 0f), new Vector3(side, width, 1f));
                Transform west = HudQuad("West", border, borderMaterial, new Vector3(-half, lift, 0f), new Vector3(side, width, 1f));
                north.localRotation = south.localRotation = Quaternion.Euler(90f, 0f, 0f);
                east.localRotation = west.localRotation = Quaternion.Euler(90f, 90f, 0f);
                border.gameObject.SetActive(false);
                mission.border = border;

                // The way on and the way out with the pointer (hands or controllers): two signs low in front of the user.
                var controls = new GameObject("MissionControls").transform;
                controls.SetParent(root.transform, false);
                var signSize = new Vector2(0.38f, 0.08f);
                mission.nextStopButton = PointerButton.Create("NextStopButton", MissionMode.NextStopText, signSize, backPlate, ui);
                mission.nextStopButton.transform.SetParent(controls, false);
                mission.nextStopButton.transform.localPosition = new Vector3(-0.205f, 0f, 0f);
                mission.nextViewButton = PointerButton.Create("NextViewButton", MissionMode.RoomViewText, signSize, backPlate, ui);
                mission.nextViewButton.transform.SetParent(controls, false);
                mission.nextViewButton.transform.localPosition = new Vector3(0.205f, 0f, 0f);
                // RECENTER under them, smaller: the least used of the three, and the only way to recenter with hands.
                mission.recenterButton = PointerButton.Create("MissionRecenterButton", MissionMode.RecenterText, new Vector2(0.30f, 0.07f), backPlate, ui);
                mission.recenterButton.transform.SetParent(controls, false);
                mission.recenterButton.transform.localPosition = new Vector3(0f, -0.095f, 0f);
                controls.gameObject.SetActive(false);
                mission.controls = controls;

                if (fade != null)
                {
                    // A shell around the head, bigger than the near plane and drawn from inside (the shader does not cull).
                    Transform curtain = GlobePrimitive(PrimitiveType.Sphere, "FadeCurtain", root.transform, fade);
                    curtain.localScale = Vector3.one * 0.5f;
                    curtain.gameObject.SetActive(false);
                    mission.curtain = curtain.GetComponent<MeshRenderer>();
                }

                mars.gameObject.SetActive(false);
                app.mission = mission;
            }
            catch (System.Exception e)
            {
                // A failure here must not stop the rest of the scene from being built.
                Debug.LogError("[Walkback] BuildMission failed: " + e);
            }
        }
    }
}
