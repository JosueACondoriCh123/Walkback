// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Walkback.EditorTools
{
    /// <summary>The pointer and the table controls, checked without a headset.</summary>
    public static partial class AutoTest
    {
        /// <summary>A plain C# target that counts what the pointer tells it.</summary>
        class PointerTestTarget : IPointerTarget
        {
            public int Enters, Exits, Clicks;
            public Vector3 LastPoint;
            public void OnPointerEnter() { Enters++; }
            public void OnPointerExit() { Exits++; }
            public void OnPointerClick(Vector3 worldPoint) { Clicks++; LastPoint = worldPoint; }
        }

        static partial void CheckPointer(WalkbackApp app)
        {
            var pointer = app.pointer;
            var controls = app.tableControls;
            Check("Pointer: built and wired to the app", pointer != null && controls != null && pointer.line != null && pointer.dot != null && controls.recenterButton != null && controls.pointer == pointer);
            if (pointer == null || controls == null) return;

            var rayMaterial = pointer.line.sharedMaterial;
            bool opaque = rayMaterial != null && rayMaterial.shader.name == "Universal Render Pipeline/Unlit" && (!rayMaterial.HasProperty("_Surface") || rayMaterial.GetFloat("_Surface") < 0.5f);
            Check("Pointer: ray and dot are opaque (no holes in passthrough)", opaque, rayMaterial != null ? rayMaterial.shader.name : "no material");
            Check("Pointer: the dot has no collider (the ray cannot hit itself)", pointer.dot.GetComponent<Collider>() == null);
            Check("Pointer: hand tracking declared next to the controllers", OVRProjectConfig.CachedProjectConfig.handTrackingSupport == OVRProjectConfig.HandTrackingSupport.ControllersAndHands);
            Check("Pointer: right hand reads the right hand, pose in tracking space", HandWired(pointer.rightHand, "HandRight"));
            Check("Pointer: left hand reads the left hand, pose in tracking space", HandWired(pointer.leftHand, "HandLeft"));
            var tableCollider = app.table.GetComponent<Collider>();
            Check("Pointer: the table collider stops at the table top", tableCollider is BoxCollider && Mathf.Abs(tableCollider.bounds.max.y - SceneBuilder.TableHeight) < 0.005f,
                tableCollider != null ? tableCollider.GetType().Name + ", top " + tableCollider.bounds.max.y.ToString("F3") + " m" : "none");

            CheckPointerDispatch(pointer);
            CheckPointerOnRealTargets(app, pointer);
            CheckPointerOnGround(app, pointer);
            CheckTableMath();
            CheckTableOnScene(app, controls);
        }

        static bool HandWired(OVRHand hand, string type)
        {
            if (hand == null) return false;
            var data = new SerializedObject(hand);
            var handType = data.FindProperty("HandType");
            var root = data.FindProperty("_pointerPoseRoot");
            return handType != null && handType.enumNames[handType.enumValueIndex] == type &&
                   root != null && root.objectReferenceValue != null && root.objectReferenceValue.name == "TrackingSpace";
        }

        static void CheckPointerDispatch(Pointer pointer)
        {
            // A box far from the scene, a plain C# target behind it.
            var box = new GameObject("PointerTestBox");
            box.transform.position = new Vector3(20f, 20f, 20f);
            var collider = box.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.5f;
            Physics.SyncTransforms();
            var target = new PointerTestTarget();
            var keep = pointer.Resolve;
            pointer.Resolve = c => c == collider ? target : Pointer.DefaultResolve(c);
            var at = new Ray(new Vector3(20f, 20f, 18f), Vector3.forward);
            var away = new Ray(new Vector3(20f, 20f, 18f), Vector3.back);
            try
            {
                pointer.Process(away, false);
                pointer.Process(at, false);
                Check("Pointer: a ray at a target enters it once, no click", target.Enters == 1 && target.Clicks == 0 && ReferenceEquals(pointer.Hovered, target),
                    "enters " + target.Enters + ", clicks " + target.Clicks);
                pointer.Process(at, true);
                Check("Pointer: a press clicks once, at the hit point", target.Clicks == 1 && Mathf.Abs(target.LastPoint.z - 19.75f) < 0.001f && pointer.PressBeganOnTarget,
                    "clicks " + target.Clicks + ", hit z " + target.LastPoint.z.ToString("F3"));
                pointer.Process(at, true);
                pointer.Process(at, true);
                Check("Pointer: holding the press does not click again", target.Clicks == 1);
                pointer.Process(at, false);
                pointer.Process(at, true);
                Check("Pointer: release and press again clicks again", target.Clicks == 2);
                pointer.Process(away, true);
                Check("Pointer: moving off the target exits it", target.Exits == 1 && pointer.Hovered == null);
                Check("Pointer: a press that began on a button never drags the map", pointer.PressBeganOnTarget && !TableControls.CanDrag(pointer));
                pointer.Process(away, false);
                pointer.Process(away, true);
                Check("Pointer: a press on nothing clicks nothing and may drag the map", target.Clicks == 2 && !pointer.PressBeganOnTarget && TableControls.CanDrag(pointer));
                pointer.Process(away, false);

                // A Unity target destroyed while hovered must not break the pointer.
                pointer.Process(at, false);
                Object.DestroyImmediate(box);
                Physics.SyncTransforms();
                bool survived = true;
                try { pointer.Process(at, false); pointer.Process(at, true); pointer.Process(at, false); }
                catch (System.Exception) { survived = false; }
                Check("Pointer: survives a target that disappears", survived && pointer.Hovered == null);
            }
            finally
            {
                pointer.Resolve = keep;
                if (box != null) Object.DestroyImmediate(box);
                pointer.Process(away, false, false);
            }
        }

        static void CheckPointerOnRealTargets(WalkbackApp app, Pointer pointer)
        {
            // Every IPointerTarget in the scene needs a collider on itself or below, or nothing can press it.
            var targets = new List<string>();
            var missing = new List<string>();
            foreach (GameObject root in app.gameObject.scene.GetRootGameObjects())
            {
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!(behaviour is IPointerTarget)) continue;
                    // The habitat pillar is drawn but deliberately not pressable (it is not a stop).
                    var pillarView = behaviour as PillarView;
                    if (pillarView != null && pillarView.IsHabitat) continue;
                    targets.Add(behaviour.name);
                    if (behaviour.GetComponentInChildren<Collider>(true) == null) missing.Add(behaviour.name);
                }
            }
            Check("Pointer: every pressable thing in the scene has a collider", missing.Count == 0,
                targets.Count + " targets" + (missing.Count > 0 ? "; without collider: " + string.Join(", ", missing) : ""));

            // The RECENTER sign through the real lookup.
            var recenter = app.tableControls.recenterButton;
            Physics.SyncTransforms();
            Vector3 center = recenter.transform.position;
            var ray = new Ray(center - recenter.transform.forward * 0.6f, recenter.transform.forward);
            pointer.Process(ray, false);
            Check("Pointer: the RECENTER sign is found through its collider", ReferenceEquals(pointer.Hovered, recenter) && recenter.Hovered && recenter.FrameColor == WalkbackTheme.Live);
            int before = recenter.Presses;
            pointer.Process(ray, true);
            Check("Pointer: pressing RECENTER registers and flashes amber", recenter.Presses == before + 1 && recenter.FrameColor == WalkbackTheme.Watch);
            pointer.Process(ray, false);
            pointer.Process(new Ray(ray.origin, -ray.direction), false);
            Check("Pointer: leaving RECENTER clears its hover", !recenter.Hovered);
            pointer.ShowNothing();
        }

        /// <summary>A ray straight down on the map center lands on the relief, not on the table under it.</summary>
        static void CheckPointerOnGround(WalkbackApp app, Pointer pointer)
        {
            var meshCollider = Pointer.AttachTerrainCollider(app.terrain);
            try
            {
                Check("Pointer: the terrain gets a ground collider at runtime", meshCollider != null && meshCollider.sharedMesh == app.terrain.GetComponent<MeshFilter>().sharedMesh);
                if (meshCollider == null) return;
                Physics.SyncTransforms();
                var site = app.terrain.Site;
                int n = site.GridSize, mid = n / 2;
                float expected = app.terrain.transform.TransformPoint(new Vector3(0f, site.Heights[mid * n + mid] - site.MinElevation, 0f)).y;
                Vector3 above = app.mapRoot.position + Vector3.up * 0.5f;
                pointer.Process(new Ray(above, Vector3.down), false);
                Check("Pointer: a ray at the map lands on the ground", pointer.HasHit && Mathf.Abs(pointer.HitPoint.y - expected) < 0.003f,
                    "hit at " + pointer.HitPoint.y.ToString("F3") + " m, ground " + expected.ToString("F3") + " m");
            }
            finally
            {
                pointer.Process(new Ray(Vector3.zero, Vector3.down), false, false);
                if (meshCollider != null) Object.DestroyImmediate(meshCollider);
            }
        }

        static void CheckTableMath()
        {
            Check("Table: zoom stays between 0.5x and 4x", Mathf.Approximately(TableControls.ClampZoom(1f, 10f), 4f) && Mathf.Approximately(TableControls.ClampZoom(1f, 0.1f), 0.5f));
            Check("Table: the map can slide 0.5 m at 1x and 2 m at 4x",
                Mathf.Abs(TableControls.ClampOffset(new Vector3(3f, 1f, 0f), 1f).magnitude - 0.5f) < 1e-4f && Mathf.Abs(TableControls.ClampOffset(new Vector3(0f, 0f, 5f), 4f).magnitude - 2f) < 1e-4f);
            TableControls.RecenterDelta(new Vector3(2f, 1.6f, 0f), new Vector3(1f, -0.3f, 0f), new Vector3(0f, 0.375f, 1f), 0f, 1f, out float yaw, out Vector3 center);
            Check("Table: recenter puts the table 1 m in front of the head, turned to it", Mathf.Abs(yaw - 90f) < 0.01f && (center - new Vector3(3f, 0.375f, 0f)).magnitude < 1e-4f,
                "turn " + yaw.ToString("F1") + "°, table at " + center.ToString("F2"));
            TableControls.TwoHandDelta(new Vector3(-0.2f, 1f, 0f), new Vector3(0.2f, 1f, 0f), new Vector3(-0.4f, 1.1f, 0f), new Vector3(0.4f, 1.1f, 0f), out float y1, out float r1, out Vector3 s1);
            Check("Table: hands moving apart zoom in", Mathf.Approximately(r1, 2f) && Mathf.Abs(y1) < 0.01f && s1.magnitude < 1e-4f);
            Vector3 a0 = new Vector3(-0.2f, 1f, 0f), b0 = new Vector3(0.2f, 1f, 0f), a1 = new Vector3(0f, 1f, -0.2f), b1 = new Vector3(0f, 1f, 0.2f);
            TableControls.TwoHandDelta(a0, b0, a1, b1, out float y2, out float r2, out Vector3 s2);
            Vector3 turned = Quaternion.Euler(0f, y2, 0f) * (b0 - a0).normalized;
            Check("Table: hands turning together turn the map the same way", (turned - (b1 - a1).normalized).magnitude < 1e-3f && Mathf.Approximately(r2, 1f), y2.ToString("F1") + "°");
        }

        static void CheckTableOnScene(WalkbackApp app, TableControls controls)
        {
            controls.Begin(app);
            var saved = new Dictionary<Transform, Pose>();
            foreach (Transform t in controls.StageObjects()) saved[t] = new Pose(t.position, t.rotation);
            Vector3 mapScale = app.mapRoot.localScale;
            Transform panel = app.panel.transform;
            Vector3 tableAt = app.table.position;
            Vector3 panelFromTable = panel.position - tableAt;
            Vector3 cameraAt = app.desktopCamera.transform.position;
            Vector3 rigAt = app.cameraRig.transform.position;
            bool wasLocked = app.MapControlsLocked;
            try
            {
                Check("Table: the table, map, panel and RECENTER move together",
                    saved.ContainsKey(app.table) && saved.ContainsKey(app.mapRoot) && saved.ContainsKey(panel) && saved.ContainsKey(controls.recenterButton.transform) && !saved.ContainsKey(app.cameraRig.transform),
                    saved.Count + " objects");

                Quaternion mapTurn = app.mapRoot.rotation;
                controls.RotateMap(30f);
                Check("Table: turning the map", Mathf.Abs(Mathf.DeltaAngle(app.mapRoot.eulerAngles.y, mapTurn.eulerAngles.y + 30f)) < 0.01f);
                controls.ZoomMap(10f);
                Check("Table: zoom in stops at 4x", Mathf.Abs(controls.Zoom - 4f) < 1e-3f, controls.Zoom.ToString("F2") + "x");
                controls.ZoomMap(0.01f);
                Check("Table: zoom out stops at 0.5x", Mathf.Abs(controls.Zoom - 0.5f) < 1e-3f);
                controls.MoveMap(new Vector3(5f, 0f, 0f));
                Vector3 offset = TableControls.Flat(app.mapRoot.position - app.table.position);
                Check("Table: the map cannot slide off the table", Mathf.Abs(offset.magnitude - TableControls.MaxOffset(0.5f)) < 1e-3f, offset.magnitude.ToString("F2") + " m");
                float mapY = app.mapRoot.position.y;
                controls.ResetMap();
                Check("Table: reset brings the map back to 1 m, centered, north up", (app.mapRoot.localScale - mapScale).magnitude < 1e-7f &&
                    TableControls.Flat(app.mapRoot.position - app.table.position).magnitude < 1e-4f && Mathf.Abs(Mathf.DeltaAngle(app.mapRoot.eulerAngles.y, 0f)) < 0.01f && Mathf.Approximately(app.mapRoot.position.y, mapY));

                app.MapControlsLocked = true;
                Vector3 lockedPos = app.mapRoot.position;
                Quaternion lockedRot = app.mapRoot.rotation;
                bool anyMoved = controls.RotateMap(45f) | controls.ZoomMap(2f) | controls.MoveMap(Vector3.right) | controls.ResetMap() | controls.Recenter(new Vector3(5f, 1.6f, 5f), Vector3.right);
                Check("Table: nothing moves while the map is locked (globe opening)", !anyMoved && app.mapRoot.position == lockedPos && app.mapRoot.rotation == lockedRot && (app.mapRoot.localScale - mapScale).magnitude < 1e-7f && app.table.position == tableAt);
                app.MapControlsLocked = false;

                controls.ZoomMap(2f);
                controls.Recenter(new Vector3(2f, 1.6f, 0f), new Vector3(1f, 0f, 0f));
                Vector3 expectedPanel = new Vector3(3f, tableAt.y, 0f) + Quaternion.Euler(0f, 90f, 0f) * panelFromTable;
                Check("Table: recenter brings the table in front of the head, panel behind it, facing the user",
                    (app.table.position - new Vector3(3f, tableAt.y, 0f)).magnitude < 1e-3f && (panel.position - expectedPanel).magnitude < 1e-3f && Vector3.Dot(panel.forward, Vector3.right) > 0.999f,
                    "table " + app.table.position.ToString("F2") + ", panel " + panel.position.ToString("F2"));
                Check("Table: recenter also resets the map and leaves the user's rig and camera alone",
                    (app.mapRoot.localScale - mapScale).magnitude < 1e-7f && TableControls.Flat(app.mapRoot.position - app.table.position).magnitude < 1e-3f &&
                    app.desktopCamera.transform.position == cameraAt && app.cameraRig.transform.position == rigAt);
                controls.Recenter(new Vector3(0f, 1.6f, 0f), Vector3.forward);
                Check("Table: recenter from the start pose puts everything back", (app.table.position - tableAt).magnitude < 1e-3f && (panel.position - (tableAt + panelFromTable)).magnitude < 1e-3f && Mathf.Abs(controls.StageYaw) < 0.01f);
            }
            finally
            {
                app.MapControlsLocked = wasLocked;
                foreach (var pair in saved) pair.Key.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
                app.mapRoot.localScale = mapScale;
            }
        }

        static partial void ShotsPointer(WalkbackApp app)
        {
            var pointer = app.pointer;
            if (pointer == null) return;
            var cam = app.desktopCamera;
            Vector3 homePosition = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            var meshCollider = Pointer.AttachTerrainCollider(app.terrain);
            Physics.SyncTransforms();
            Vector3 hand = new Vector3(0.20f, 1.10f, 0.05f);
            try
            {
                Vector3 sign = app.tableControls.recenterButton.transform.position;
                var toSign = new Ray(hand, (sign - hand).normalized);
                pointer.Process(toSign, false);
                pointer.ShowRay(toSign);
                Shot(cam, "pointer_01_hover_recenter.png", new Vector3(-0.30f, 1.32f, 0.10f), new Vector3(0.32f, 0.92f, 0.28f));

                Vector3 onMap = new Vector3(-0.15f, SceneBuilder.TableHeight + 0.05f, 1.05f);
                var toMap = new Ray(hand, (onMap - hand).normalized);
                pointer.Process(toMap, false);
                pointer.ShowRay(toMap);
                Shot(cam, "pointer_02_ray_on_map.png", new Vector3(0.35f, 1.55f, -0.30f), new Vector3(-0.05f, 0.85f, 0.90f));
            }
            finally
            {
                pointer.Process(new Ray(hand, Vector3.back), false, false);
                pointer.ShowNothing();
                if (meshCollider != null) Object.DestroyImmediate(meshCollider);
                cam.transform.SetPositionAndRotation(homePosition, homeRotation);
            }
        }
    }
}
