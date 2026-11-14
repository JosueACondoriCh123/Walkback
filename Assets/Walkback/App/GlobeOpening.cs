// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Walkback
{
    /// <summary>
    /// The opening (guion §3, piece 9): Mars floats over the table with one point of light on the
    /// site and the WALKBACK title above it. Pressing the globe turns the light toward you, shrinks
    /// the planet into it and grows the square kilometer onto the table. While that happens the
    /// table controls are locked (trap 17), and the map is hidden only one frame after start so
    /// the controls keep its real size.
    /// The globe image is session B's mars_global.jpg when it exists; otherwise a plain sphere,
    /// labelled as such. The image, the mesh and the light share GlobeMath (trap 11).
    /// </summary>
    public class GlobeOpening : MonoBehaviour, IPointerTarget
    {
        public enum State { Off, Opening, Transition, Done }

        public const string GlobeImageFile = "Site/mars_global.jpg";
        public const float TransitionSeconds = 2.6f;

        public WalkbackApp app;
        /// <summary>Everything that shows during the opening (inactive in the built scene).</summary>
        public GameObject visual;
        /// <summary>The planet: rotates and scales around its center. Has the collider.</summary>
        public Transform pivot;
        public MeshRenderer globeRenderer;
        public Transform beacon;
        public Transform beaconCore;
        public GlobeLabel title;
        public GlobeLabel hint;
        public GlobeLabel siteLabel;

        public float radius = 0.22f;
        public Vector3 center = new Vector3(0f, 1.22f, 1.0f);

        public State Current { get; private set; }
        public float SiteLatitude { get; private set; }
        public float SiteLongitude { get; private set; }
        public string SiteName { get; private set; }
        public bool HasMarsImage { get; private set; }
        public Texture GlobeTexture { get { return texture; } }
        /// <summary>Raised when the map has landed on the table and the controls are free again.</summary>
        public event Action Landed;

        Mesh mesh;
        Texture2D texture;
        AudioSource audioSource;
        bool built, captured, hovered;
        Vector3 mapScale, mapPosition;
        bool mapWasActive, panelWasActive;
        Quaternion startRotation, faceRotation;
        Vector3 lightPoint;
        Vector3 beaconCoreScale = Vector3.one;

        void Awake()
        {
            EnsureBuilt();
        }

        void OnDestroy()
        {
            DestroySafe(mesh);
            DestroySafe(texture);
        }

        /// <summary>Mesh, image, site and texts. Safe to call more than once (the test calls it in edit mode).</summary>
        public void EnsureBuilt()
        {
            if (built && mesh != null) return;
            built = true;
            if (app == null) app = FindAnyObjectByType<WalkbackApp>();

            DestroySafe(mesh);
            mesh = GlobeMath.BuildGlobeMesh(radius, 64, 128, GlobeMath.GlobeImageWestEdge);
            pivot.GetComponent<MeshFilter>().sharedMesh = mesh;
            var sphere = pivot.GetComponent<SphereCollider>();
            if (sphere != null) sphere.radius = radius;

            DestroySafe(texture);
            texture = null;
            HasMarsImage = false;
            try
            {
                byte[] bytes = SiteLoader.ReadStreamingAsset(GlobeImageFile);
                if (bytes != null)
                {
                    var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = "GlobeMars", hideFlags = HideFlags.DontSave };
                    if (loaded.LoadImage(bytes, false))
                    {
                        loaded.wrapMode = TextureWrapMode.Repeat;
                        loaded.filterMode = FilterMode.Trilinear;
                        loaded.anisoLevel = 4;
                        texture = loaded;
                        HasMarsImage = true;
                    }
                    else DestroySafe(loaded);
                }
            }
            catch (Exception e)
            {
                WalkbackLog.Warn("Globe image could not be read: " + e.Message);
            }
            if (texture == null) texture = GlobeMath.PlainSphereTexture(1024, 512);
            var block = new MaterialPropertyBlock();
            block.SetTexture("_BaseMap", texture);
            globeRenderer.SetPropertyBlock(block);

            // The site: the real package's center, or Gale crater's published center while the table is a test.
            var site = app != null && app.terrain != null ? app.terrain.Site : null;
            bool real = site != null && site.IsMarsData && site.Info != null;
            SiteLatitude = real ? (float)site.Info.centerLat : GlobeMath.GaleLatitude;
            SiteLongitude = real ? (float)site.Info.centerLon : GlobeMath.GaleLongitude;
            SiteName = real && !string.IsNullOrEmpty(site.Info.name) ? site.Info.name.ToUpperInvariant() : "GALE CRATER";

            Vector3 local = GlobeMath.LatLonToLocal(SiteLatitude, SiteLongitude, radius);
            beacon.localPosition = local;
            beacon.localRotation = Quaternion.FromToRotation(Vector3.up, local.normalized);
            if (beaconCore != null) beaconCoreScale = beaconCore.localScale;

            title.SetLines("WALKBACK", "The safest way to go far.");
            string where = SiteName + "  ·  " + LatLonText(SiteLatitude, SiteLongitude);
            string terrainNote = real ? "" : "The table shows a TEST TERRAIN, not Mars data";
            string imageNote = HasMarsImage ? GlobeCredit(site) : "Plain sphere: no global Mars image loaded yet";
            hint.SetLines("Point at the light and press to land", where, terrainNote, imageNote);
            siteLabel.SetLines(SiteName);
        }

        /// <summary>Runtime start: lock the controls now, hide the map one frame later (trap 17).</summary>
        public void Begin()
        {
            EnsureBuilt();
            if (app != null) app.MapControlsLocked = true;
            StartCoroutine(OpenAfterAFrame());
        }

        IEnumerator OpenAfterAFrame()
        {
            yield return null;
            SetOpeningState();
            WalkbackLog.Info("Globe opening: " + SiteName + " at " + LatLonText(SiteLatitude, SiteLongitude) + (HasMarsImage ? ", Mars image" : ", plain sphere"));
        }

        /// <summary>Remembers the map and panel as they are now, to put them back exactly.</summary>
        void Capture()
        {
            if (captured || app == null) return;
            captured = true;
            mapScale = app.mapRoot.localScale;
            mapPosition = app.mapRoot.position;
            mapWasActive = app.mapRoot.gameObject.activeSelf;
            panelWasActive = app.panel != null && app.panel.gameObject.activeSelf;
        }

        /// <summary>The planet over the table, the map and the panel hidden, the controls locked.</summary>
        public void SetOpeningState()
        {
            EnsureBuilt();
            Capture();
            visual.SetActive(true);
            pivot.position = center;
            pivot.localScale = Vector3.one;
            faceRotation = GlobeMath.FaceRotation(beacon.localPosition, Eye() - center);
            pivot.rotation = faceRotation;
            pivot.GetComponent<Collider>().enabled = true;
            title.gameObject.SetActive(true);
            hint.gameObject.SetActive(true);
            siteLabel.gameObject.SetActive(true);
            title.Face(Eye());
            hint.Face(Eye());
            PlaceSiteLabel();
            app.mapRoot.gameObject.SetActive(false);
            if (app.panel != null) app.panel.gameObject.SetActive(false);
            app.MapControlsLocked = true;
            Current = State.Opening;
        }

        /// <summary>Pressing the globe (pointer, the A button, or G on the keyboard) starts the landing.</summary>
        public void Press()
        {
            if (Current != State.Opening) return;
            Current = State.Transition;
            BeginTransitionPose();
            PlayPress();
            WalkbackLog.Info("Globe pressed: landing on " + SiteName);
            if (Application.isPlaying) StartCoroutine(RunTransition());
        }

        void BeginTransitionPose()
        {
            startRotation = pivot.rotation;
            faceRotation = GlobeMath.FaceRotation(beacon.localPosition, Eye() - center);
            lightPoint = center + faceRotation * beacon.localPosition;
            pivot.GetComponent<Collider>().enabled = false;
            title.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            siteLabel.gameObject.SetActive(false);
            pivot.localScale = Vector3.one;
            hovered = false;
        }

        IEnumerator RunTransition()
        {
            float start = Time.time;
            while (Time.time - start < TransitionSeconds)
            {
                ApplyTransition((Time.time - start) / TransitionSeconds);
                yield return null;
            }
            ApplyTransition(1f);
            FinishTransition();
        }

        /// <summary>
        /// The landing at time t (0..1): first the light turns toward the viewer, then the planet
        /// shrinks into the light while the map grows from it down onto the table.
        /// </summary>
        public void ApplyTransition(float t)
        {
            if (Current == State.Opening) { Current = State.Transition; BeginTransitionPose(); }
            float turn = GlobeMath.Ease(t / 0.35f);
            float grow = GlobeMath.Ease((t - 0.35f) / 0.65f);

            float shrink = Mathf.Max(1f - grow, 0.0001f);
            pivot.rotation = Quaternion.Slerp(startRotation, faceRotation, turn);
            pivot.localScale = Vector3.one * shrink;
            // While it shrinks the planet is already facing the viewer: keep the light where it is.
            pivot.position = grow > 0f ? lightPoint - faceRotation * beacon.localPosition * shrink : center;

            bool mapOn = grow > 0f;
            if (app.mapRoot.gameObject.activeSelf != mapOn) app.mapRoot.gameObject.SetActive(mapOn);
            if (mapOn)
            {
                app.mapRoot.localScale = mapScale * Mathf.Lerp(0.02f, 1f, grow);
                app.mapRoot.position = Vector3.Lerp(lightPoint, mapPosition, grow);
            }
        }

        /// <summary>The end of the landing: map exactly as it was, panel back, globe hidden, controls free.</summary>
        public void FinishTransition()
        {
            Capture();
            visual.SetActive(false);
            pivot.localScale = Vector3.one;
            pivot.position = center;
            app.mapRoot.localScale = mapScale;
            app.mapRoot.position = mapPosition;
            app.mapRoot.gameObject.SetActive(mapWasActive);
            if (app.panel != null) app.panel.gameObject.SetActive(panelWasActive);
            Current = State.Done;
            if (Application.isPlaying) StartCoroutine(UnlockAfterAFrame());
            else Unlock();
        }

        IEnumerator UnlockAfterAFrame()
        {
            yield return null;
            Unlock();
            WalkbackLog.Info("Map landed on the table");
        }

        void Unlock()
        {
            app.MapControlsLocked = false;
            if (Landed != null) Landed();
            app.RaiseMapLanded();
        }

        /// <summary>Back to the built scene's state, for the test: globe hidden, map and panel as captured.</summary>
        public void ResetForTest()
        {
            if (captured)
            {
                app.mapRoot.localScale = mapScale;
                app.mapRoot.position = mapPosition;
                app.mapRoot.gameObject.SetActive(mapWasActive);
                if (app.panel != null) app.panel.gameObject.SetActive(panelWasActive);
            }
            visual.SetActive(false);
            pivot.localScale = Vector3.one;
            pivot.position = center;
            app.MapControlsLocked = false;
            Current = State.Off;
            captured = false;
        }

        void Update()
        {
            if (Current != State.Opening) return;
            Vector3 eye = Eye();
            // A gentle sway around "light facing you", so the light never hides behind the planet.
            faceRotation = GlobeMath.FaceRotation(beacon.localPosition, eye - center);
            pivot.rotation = Quaternion.AngleAxis(18f * Mathf.Sin(Time.time * 0.45f), Vector3.up) * faceRotation;
            pivot.localScale = Vector3.one * (hovered ? 1.04f : 1f);
            if (beaconCore != null)
                beaconCore.localScale = beaconCoreScale * (1f + 0.35f * Mathf.Sin(Time.time * 4.5f) + (hovered ? 0.4f : 0f));
            title.Face(eye);
            hint.Face(eye);
            PlaceSiteLabel();
        }

        /// <summary>The site's name next to the light, turned toward the viewer.</summary>
        void PlaceSiteLabel()
        {
            Vector3 eye = Eye();
            Vector3 lightWorld = beacon.position;
            Vector3 toEye = (eye - lightWorld).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, toEye).normalized;
            // Canvas +z points away from the viewer, so "right" on screen is -right here.
            siteLabel.transform.position = lightWorld - right * 0.22f + Vector3.up * 0.05f + toEye * 0.08f;
            siteLabel.Face(eye);
        }

        /// <summary>Where the viewer's eyes are: the headset's center eye, or the desktop camera.</summary>
        public Vector3 Eye()
        {
            if (app != null)
            {
                if (XRSettings.isDeviceActive && app.cameraRig != null && app.cameraRig.activeInHierarchy)
                {
                    var rig = app.cameraRig.GetComponent<OVRCameraRig>();
                    if (rig != null && rig.centerEyeAnchor != null) return rig.centerEyeAnchor.position;
                }
                if (app.desktopCamera != null) return app.desktopCamera.transform.position;
            }
            return new Vector3(0f, 1.6f, 0f);
        }

        public void OnPointerEnter() { hovered = true; }
        public void OnPointerExit() { hovered = false; }
        public void OnPointerClick(Vector3 worldPoint) { Press(); }

        void PlayPress()
        {
            if (!Application.isPlaying) return;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.6f;
                audioSource.clip = Chime();
            }
            audioSource.transform.position = center;
            audioSource.Play();
        }

        /// <summary>A short rising chime made in code (no audio file to credit).</summary>
        static AudioClip Chime()
        {
            const int rate = 44100;
            int n = rate * 6 / 10;
            var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float f = Mathf.Lerp(520f, 880f, Mathf.Clamp01(t / 0.25f));
                float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 6f);
                samples[i] = 0.35f * envelope * Mathf.Sin(2f * Mathf.PI * f * t);
            }
            var clip = AudioClip.Create("GlobePress", n, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static string LatLonText(float lat, float lon)
        {
            return Mathf.Abs(lat).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "° " + (lat < 0f ? "S" : "N") + ", " + Mathf.Abs(lon).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "° " + (lon < 0f ? "W" : "E");
        }

        static string GlobeCredit(SiteData site)
        {
            var s = site != null ? site.Source("globe") : null;
            if (s == null) return "Globe image: mars_global.jpg (source not listed in site.json)";
            string text = "Globe: " + (s.mission + " " + s.instrument).Trim();
            if (!string.IsNullOrEmpty(s.credit)) text += " · " + s.credit;
            return text;
        }

        static void DestroySafe(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
