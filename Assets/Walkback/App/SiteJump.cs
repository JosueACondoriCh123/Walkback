// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The jump to the site (guion §3, piece 10): the panel's JUMP TO SITE button raises
    /// app.JumpRequested, and the user is surrounded by that stop's 360° photo
    /// (StreamingAssets/Site/panorama_[id].jpg, session B), seen from inside an opaque sphere,
    /// not mirrored (trap 12), at full resolution up to 4096 px wide (trap 13), with the stop's
    /// name and photo credit in front. Table, map and panel hide while inside and come back after.
    /// No photo for a stop → a one-line message, never a stand-in picture.
    /// </summary>
    public class SiteJump : MonoBehaviour, IPointerTarget
    {
        public const int MaxPanoramaWidth = 4096;
        public const float SphereRadius = 6f;
        public const float NoPanoramaSeconds = 4f;
        public const string ReturnHint = "Press anywhere to return to the table";
        public const string NoPanoramaText = "No panorama for this stop yet";

        public WalkbackApp app;
        /// <summary>The inside-out photo sphere (inactive until a jump). Has the collider: press anywhere to return.</summary>
        public GameObject sphere;
        public GlobeLabel card;
        public GlobeLabel noPanorama;

        public bool Inside { get; private set; }
        public Texture CurrentTexture { get { return current; } }

        Mesh mesh;
        Texture current;
        bool ownsTexture;
        bool tableWas, mapWas, panelWas, lockWas;
        float noPanoramaUntil = -1f;

        void Awake()
        {
            EnsureBuilt();
        }

        void OnDestroy()
        {
            ReleaseTexture();
            DestroySafe(mesh);
        }

        public void EnsureBuilt()
        {
            if (app == null) app = FindAnyObjectByType<WalkbackApp>();
            if (mesh != null) return;
            mesh = GlobeMath.BuildPanoramaMesh(SphereRadius, 48, 96);
            sphere.GetComponent<MeshFilter>().sharedMesh = mesh;
            var collider = sphere.GetComponent<MeshCollider>();
            if (collider != null) collider.sharedMesh = mesh;
        }

        public static string PanoramaFile(StopInfo stop)
        {
            return "Site/panorama_" + stop.panorama + ".jpg";
        }

        /// <summary>Runtime: jump to stop <paramref name="index"/> of the site, or say there is no photo.</summary>
        public void Jump(int index)
        {
            EnsureBuilt();
            var stops = app != null && app.terrain != null && app.terrain.Site != null ? app.terrain.Site.Stops : null;
            if (stops == null || index < 0 || index >= stops.Length)
            {
                WalkbackLog.Warn("Jump: no stop " + index);
                return;
            }
            var stop = stops[index];
            byte[] bytes = null;
            if (!string.IsNullOrEmpty(stop.panorama))
            {
                try { bytes = SiteLoader.ReadStreamingAsset(PanoramaFile(stop)); }
                catch (Exception e) { WalkbackLog.Warn("Panorama could not be read: " + e.Message); }
            }
            if (bytes == null)
            {
                ShowNoPanorama(stop.name);
                WalkbackLog.Info("Jump: no panorama for " + stop.name);
                return;
            }
            var loaded = new Texture2D(2, 2, TextureFormat.RGB24, true) { name = "Panorama_" + stop.id, hideFlags = HideFlags.DontSave };
            if (!loaded.LoadImage(bytes, false))
            {
                DestroySafe(loaded);
                ShowNoPanorama(stop.name);
                WalkbackLog.Warn("Jump: " + PanoramaFile(stop) + " is not a readable image");
                return;
            }
            Texture shown = CapWidth(loaded, MaxPanoramaWidth);
            if (shown != loaded) DestroySafe(loaded);
            WalkbackLog.Info("Jump: " + stop.name + ", panorama " + shown.width + " x " + shown.height);
            EnterJump(shown, stop.name, stop.photoCredit, true);
        }

        /// <summary>Surrounds the viewer with <paramref name="texture"/>; hides table, map and panel.</summary>
        public void EnterJump(Texture texture, string stopName, string photoCredit, bool takeOwnership)
        {
            EnsureBuilt();
            HideNoPanorama();
            if (!Inside)
            {
                tableWas = app.table != null && app.table.gameObject.activeSelf;
                mapWas = app.mapRoot.gameObject.activeSelf;
                panelWas = app.panel != null && app.panel.gameObject.activeSelf;
                lockWas = app.MapControlsLocked;
            }
            ReleaseTexture();
            current = texture;
            ownsTexture = takeOwnership;

            Transform head = ViewerTransform();
            Vector3 eye = head != null ? head.position : new Vector3(0f, 1.6f, 0f);
            Vector3 forward = head != null ? Vector3.ProjectOnPlane(head.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();

            // Center column of the photo straight ahead of the viewer.
            sphere.transform.position = eye;
            sphere.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            var block = new MaterialPropertyBlock();
            block.SetTexture("_BaseMap", texture);
            sphere.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            sphere.SetActive(true);

            card.SetLines(string.IsNullOrEmpty(stopName) ? "Site" : stopName,
                string.IsNullOrEmpty(photoCredit) ? "Photo credit: not given in stops.json" : photoCredit,
                ReturnHint);
            card.transform.position = eye + forward * 1.3f + Vector3.down * 0.42f;
            card.transform.rotation = Quaternion.LookRotation(card.transform.position - eye, Vector3.up);
            card.gameObject.SetActive(true);

            if (app.table != null) app.table.gameObject.SetActive(false);
            app.mapRoot.gameObject.SetActive(false);
            if (app.panel != null) app.panel.gameObject.SetActive(false);
            app.MapControlsLocked = true;
            Inside = true;
        }

        /// <summary>Back to the table: everything as it was before the jump.</summary>
        public void ExitJump()
        {
            if (!Inside) return;
            sphere.SetActive(false);
            card.gameObject.SetActive(false);
            if (app.table != null) app.table.gameObject.SetActive(tableWas);
            app.mapRoot.gameObject.SetActive(mapWas);
            if (app.panel != null) app.panel.gameObject.SetActive(panelWas);
            app.MapControlsLocked = lockWas;
            ReleaseTexture();
            Inside = false;
            WalkbackLog.Info("Jump: back to the table");
        }

        public void ShowNoPanorama(string stopName)
        {
            Transform head = ViewerTransform();
            Vector3 eye = head != null ? head.position : new Vector3(0f, 1.6f, 0f);
            Vector3 forward = head != null ? Vector3.ProjectOnPlane(head.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            noPanorama.SetLines(NoPanoramaText, string.IsNullOrEmpty(stopName) ? "" : stopName);
            noPanorama.transform.position = eye + forward * 1.1f + Vector3.down * 0.2f;
            noPanorama.transform.rotation = Quaternion.LookRotation(noPanorama.transform.position - eye, Vector3.up);
            noPanorama.gameObject.SetActive(true);
            noPanoramaUntil = Application.isPlaying ? Time.time + NoPanoramaSeconds : -1f;
        }

        public void HideNoPanorama()
        {
            noPanorama.gameObject.SetActive(false);
            noPanoramaUntil = -1f;
        }

        void Update()
        {
            if (noPanoramaUntil > 0f && Time.time > noPanoramaUntil) HideNoPanorama();
            if (!Inside) return;
            // Ways back besides pressing: B on the right controller, Escape or Backspace on the keyboard.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool key = keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame);
            if (key || OVRInput.GetDown(OVRInput.Button.Two)) ExitJump();
        }

        /// <summary>The photo as is, or a copy scaled down to <paramref name="maxWidth"/> (keeps the 2:1 shape).</summary>
        public static Texture CapWidth(Texture2D source, int maxWidth)
        {
            if (source.width <= maxWidth) return source;
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * (maxWidth / (float)source.width)));
            var target = new RenderTexture(maxWidth, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = source.name + "_capped",
                useMipMap = true,
                autoGenerateMips = true,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontSave,
            };
            target.Create();
            Graphics.Blit(source, target);
            return target;
        }

        Transform ViewerTransform()
        {
            if (app == null) return null;
            if (UnityEngine.XR.XRSettings.isDeviceActive && app.cameraRig != null && app.cameraRig.activeInHierarchy)
            {
                var rig = app.cameraRig.GetComponent<OVRCameraRig>();
                if (rig != null && rig.centerEyeAnchor != null) return rig.centerEyeAnchor;
            }
            return app.desktopCamera != null ? app.desktopCamera.transform : null;
        }

        void ReleaseTexture()
        {
            if (current != null && ownsTexture)
            {
                var rt = current as RenderTexture;
                if (rt != null) rt.Release();
                DestroySafe(current);
            }
            current = null;
            ownsTexture = false;
        }

        static void DestroySafe(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        public void OnPointerEnter() { }
        public void OnPointerExit() { }
        public void OnPointerClick(Vector3 worldPoint) { if (Inside) ExitJump(); }
    }
}
