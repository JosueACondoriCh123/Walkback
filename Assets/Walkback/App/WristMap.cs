// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The 3D map on the wrist (guion §22.2, §21.6 "pegado a la mano"): the same square kilometer as the
    /// table (same mesh, same paint as the table's layer), 14 cm wide, relief not exaggerated, held
    /// above the left hand or controller and tilted toward the face. Pins: cyan = you, green = habitat,
    /// amber = stops. It shows while the astronaut looks at the raised left hand, or stays on after
    /// pressing X (plan B of guion §22.5). Without a headset it floats low on the left of the view.
    /// </summary>
    public class WristMap : MonoBehaviour
    {
        public const float WidthMeters = 0.14f;
        public const float LookAngleDegrees = 40f;
        public const float TiltDegrees = 45f;

        /// <summary>Scaled holder: its children are in the terrain mesh's space (meters, origin at the map center).</summary>
        public Transform model;
        public MeshFilter terrainCopy;
        public MeshRenderer terrainRenderer;
        public Transform youPin;
        /// <summary>A nose on the you-pin pointing the way the astronaut looks (hidden while that is not known).</summary>
        public Transform youNose;
        public Transform habitatPin;
        public Material stopMaterial;
        public GlobeLabel label;

        public bool Visible { get { return gameObject.activeSelf; } }
        public bool Pinned;
        public Vector2 You { get; private set; }

        readonly List<Transform> stopPins = new List<Transform>();

        /// <summary>
        /// Which way the astronaut looks, in degrees clockwise from north: the you-pin turns and shows its
        /// nose. The model's +z is north and +x east, so the heading is a turn about its up axis.
        /// </summary>
        public void SetHeading(bool known, float degreesFromNorth)
        {
            if (youPin != null) youPin.localRotation = Quaternion.Euler(0f, known ? degreesFromNorth : 0f, 0f);
            if (youNose != null) youNose.gameObject.SetActive(known);
        }

        /// <summary>Copies the table's terrain and puts the pins for an astronaut standing at <paramref name="here"/>.</summary>
        public void Prepare(WalkbackApp app, Vector2 here, string hereName)
        {
            GaleTerrain terrain = app.terrain;
            SiteData site = terrain.Site;
            You = here;
            terrainCopy.sharedMesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            terrainRenderer.sharedMaterial = terrain.GetComponent<MeshRenderer>().sharedMaterial;
            var block = new MaterialPropertyBlock();
            Texture paint = terrain.LayerTexture(terrain.Layer);
            if (paint != null) block.SetTexture("_BaseMap", paint);
            block.SetColor("_BaseColor", terrain.Layer == MapLayer.Photo ? terrain.PhotoTint : Color.white);
            terrainRenderer.SetPropertyBlock(block);
            model.localScale = Vector3.one * (WidthMeters / site.SideMeters);

            Pin(youPin, site, here, 26f);
            Pin(habitatPin, site, site.Habitat, 22f);
            var stops = site.Stops;
            while (stopPins.Count < stops.Length) stopPins.Add(MakePin("StopPin" + stopPins.Count));
            for (int i = 0; i < stopPins.Count; i++)
            {
                bool used = i < stops.Length;
                stopPins[i].gameObject.SetActive(used);
                if (used) Pin(stopPins[i], site, new Vector2(stops[i].x, stops[i].y), 16f);
            }
            if (label != null) label.SetLines("YOU  ·  " + hereName);
        }

        void Pin(Transform pin, SiteData site, Vector2 meters, float half)
        {
            pin.localPosition = RouteGeometry.ToMap(site, meters, half);
        }

        Transform MakePin(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (Application.isPlaying) Destroy(go.GetComponent<Collider>()); else DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(model, false);
            go.transform.localScale = new Vector3(12f, 16f, 12f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = stopMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public void SetVisible(bool on)
        {
            if (gameObject.activeSelf != on) gameObject.SetActive(on);
        }

        /// <summary>Above the hand or controller, north away from the viewer, tilted toward the face.</summary>
        public void HoldAt(Transform hand, Transform head)
        {
            Vector3 at = hand.position + Vector3.up * 0.07f;
            Pose(at, head);
        }

        /// <summary>No headset: low on the left of the view, as if the wrist were raised.</summary>
        public void HoldInFrontOf(Transform head)
        {
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, flat);
            Pose(head.position + flat * 0.45f - right * 0.24f + Vector3.down * 0.20f, head);
        }

        void Pose(Vector3 at, Transform head)
        {
            Vector3 away = Vector3.ProjectOnPlane(at - head.position, Vector3.up);
            if (away.sqrMagnitude < 1e-6f) away = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            transform.SetPositionAndRotation(at, Quaternion.LookRotation(away.normalized, Vector3.up) * Quaternion.Euler(-TiltDegrees, 0f, 0f));
            if (label != null) label.transform.rotation = Quaternion.LookRotation(label.transform.position - head.position, Vector3.up);
        }

        /// <summary>The wrist "gesture": the left hand is raised into the view.</summary>
        public static bool LookingAt(Transform hand, Transform head)
        {
            return hand != null && head != null && Vector3.Angle(head.forward, hand.position - head.position) < LookAngleDegrees;
        }
    }
}
