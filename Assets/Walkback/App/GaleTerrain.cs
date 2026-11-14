// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Walkback
{
    /// <summary>
    /// The square kilometer on the table: a 257 x 257 mesh from session B's site package
    /// (or the synthetic test terrain until it lands), relief not exaggerated, and three paints
    /// computed from the heights: height, slope and return on foot; plus the HiRISE photo (Site/ortho.jpg)
    /// draped on the relief, the first thing the user sees when there is one.
    /// Mesh units are meters; the parent scales the kilometer down to the table. North is +z.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GaleTerrain : MonoBehaviour
    {
        public SiteData Site { get; private set; }
        /// <summary>Slope in degrees at every node, row 0 = north.</summary>
        public float[] Slope { get; private set; }
        /// <summary>Minutes back to the habitat from every node, row 0 = north.</summary>
        public float[] ReturnMinutes { get; private set; }
        /// <summary>True while the return layer comes from the stand-in, not session C's model.</summary>
        public bool ReturnIsPlaceholder { get; private set; }
        /// <summary>What the legend says under the return layer while it is a placeholder (which stand-in made it).</summary>
        public string ReturnPlaceholderLabel { get; private set; } = ReturnStandIn.Label;
        /// <summary>
        /// Minutes of walking the oxygen left covers (infinity = no limit shown). The return layer paints
        /// in red the ground farther from the habitat than this: from there you no longer make it back.
        /// </summary>
        public float ReturnReachMinutes { get; private set; } = float.PositiveInfinity;

        /// <summary>Sets ReturnReachMinutes and repaints the return layer (shown at once if it is the layer on the map).</summary>
        public void SetReturnReach(float minutes)
        {
            if (float.IsNaN(minutes) || minutes < 0f) minutes = 0f;
            bool same = float.IsInfinity(minutes) ? float.IsInfinity(ReturnReachMinutes) : Mathf.Approximately(minutes, ReturnReachMinutes);
            if (same) return;
            ReturnReachMinutes = minutes;
            int i = (int)MapLayer.Return;
            DestroySafe(paints[i]);
            paints[i] = null;
            PaintMilliseconds[i] = 0;
            if (Layer == MapLayer.Return) ShowLayer(MapLayer.Return);
        }
        public MapLayer Layer { get; private set; } = MapLayer.Photo;
        /// <summary>Width of the photo on the table (memory on the Quest: 2048 px is 0.5 mm per pixel on a 1 m map).</summary>
        public const int PhotoSize = 4096;
        /// <summary>False when the site package has no ortho.jpg (the synthetic terrain): PHOTO shows HEIGHT instead.</summary>
        public bool HasPhoto { get; private set; }
        /// <summary>Milliseconds each layer took to paint (0 = not painted yet).</summary>
        public readonly long[] PaintMilliseconds = new long[4];

        public event Action<MapLayer> LayerChanged;

        Mesh mesh;
        readonly Texture2D[] paints = new Texture2D[4];
        MaterialPropertyBlock block;

        void Awake()
        {
            Build();
        }

        void OnDestroy()
        {
            DestroySafe(mesh);
            for (int i = 0; i < paints.Length; i++) DestroySafe(paints[i]);
        }

        /// <summary>Loads the site package (or the synthetic stand-in) and builds everything.</summary>
        public void Build()
        {
            Build(SiteLoader.Load());
        }

        public void Build(SiteData site)
        {
            var watch = Stopwatch.StartNew();
            Site = site;
            BuildMesh();
            Slope = TerrainLayers.SlopeDegrees(site.Heights, site.GridSize, site.NodeSpacing);
            // PLAN B until session C's ReturnMap lands: the fastest way back over the real relief
            // (ReturnFallback). If it fails for any reason, the old straight-line stand-in.
            try
            {
                ReturnMinutes = ReturnFallback.Minutes(site, Slope);
                ReturnPlaceholderLabel = ReturnFallback.Label;
            }
            catch (Exception e)
            {
                WalkbackLog.Warn("Return fallback failed, using the straight-line stand-in: " + e.Message);
                ReturnMinutes = ReturnStandIn.Minutes(site);
                ReturnPlaceholderLabel = ReturnStandIn.Label;
            }
            ReturnIsPlaceholder = true;
            HasPhoto = site.IsMarsData && SiteLoader.ReadStreamingAsset(SiteLoader.SiteFolder + "/ortho.jpg") != null;
            for (int i = 0; i < paints.Length; i++)
            {
                DestroySafe(paints[i]);
                paints[i] = null;
                PaintMilliseconds[i] = 0;
            }
            ShowLayer(Layer);
            WalkbackLog.Info("Terrain built in " + watch.ElapsedMilliseconds + " ms: " + (site.IsMarsData ? site.Info.name : "synthetic test terrain") +
                ", relief " + (site.MaxElevation - site.MinElevation).ToString("F1") + " m");
        }

        /// <summary>True when site.json gives a colour for the black-and-white photo.</summary>
        public bool PhotoTinted
        {
            get { var t = Site != null && Site.Info != null ? Site.Info.photoTint : null; return t != null && t.r + t.g + t.b > 0f; }
        }

        /// <summary>
        /// The colour multiplied onto the HiRISE photo (a single red band, black and white): site.json photoTint,
        /// the ground colour Curiosity's Mastcam saw, scaled so its brightest channel is 1. White without it.
        /// </summary>
        public Color PhotoTint
        {
            get
            {
                if (!PhotoTinted) return Color.white;
                var t = Site.Info.photoTint;
                float top = Mathf.Max(t.r, Mathf.Max(t.g, t.b));
                return new Color(t.r / top, t.g / top, t.b / top, 1f);
            }
        }

        public void ShowLayer(MapLayer layer)
        {
            if (layer == MapLayer.Photo && !HasPhoto) layer = MapLayer.Height;
            Layer = layer;
            if (block == null) block = new MaterialPropertyBlock();
            block.SetTexture("_BaseMap", LayerTexture(layer));
            // Only the photo is tinted: the other layers' colours are their legend.
            block.SetColor("_BaseColor", layer == MapLayer.Photo ? PhotoTint : Color.white);
            GetComponent<MeshRenderer>().SetPropertyBlock(block);
            if (LayerChanged != null) LayerChanged(layer);
        }

        /// <summary>The paint of one layer; painted the first time it is asked for.</summary>
        public Texture2D LayerTexture(MapLayer layer)
        {
            int i = (int)layer;
            if (paints[i] != null) return paints[i];

            var watch = Stopwatch.StartNew();
            if (layer == MapLayer.Photo)
            {
                paints[i] = HasPhoto ? LoadPhoto(PhotoSize) : null;
                if (paints[i] == null) return LayerTexture(MapLayer.Height);
                PaintMilliseconds[i] = Math.Max(1, watch.ElapsedMilliseconds);
                WalkbackLog.Info("Layer Photo loaded in " + PaintMilliseconds[i] + " ms");
                return paints[i];
            }
            int size = TerrainLayers.PaintSize;
            int n = Site.GridSize;
            Color32[] pixels;
            switch (layer)
            {
                case MapLayer.Slope:
                    pixels = TerrainLayers.PaintSlope(Slope, n, size);
                    break;
                case MapLayer.Return:
                    Vector2 h = Site.Habitat;
                    pixels = TerrainLayers.PaintReturn(ReturnMinutes, n, size, new Vector2(h.x / Site.SideMeters, h.y / Site.SideMeters), ReturnReachMinutes);
                    break;
                default:
                    pixels = TerrainLayers.PaintHeight(Site.Heights, n, Site.MinElevation, Site.MaxElevation, size);
                    break;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Paint_" + layer,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            // In the headset the CPU copy is dropped (12 MB saved); the editor test keeps it to read pixels.
            texture.Apply(true, Application.isPlaying);
            paints[i] = texture;
            PaintMilliseconds[i] = Math.Max(1, watch.ElapsedMilliseconds);
            WalkbackLog.Info("Layer " + layer + " painted in " + PaintMilliseconds[i] + " ms");
            return texture;
        }

        /// <summary>Anisotropic filtering of the photo: the ground is seen at a grazing angle (above all at real scale, in mission mode).</summary>
        public const int PhotoAniso = 16;

        /// <summary>
        /// Site/ortho.jpg with mipmaps; null if it cannot be read. At its own size (4096 px over 1 km = 24 cm per
        /// pixel, the resolution of the HiRISE mosaic) it is decoded straight into the texture, with no resampling;
        /// a larger file is scaled down to size x size.
        /// </summary>
        static Texture2D LoadPhoto(int size)
        {
            byte[] bytes = SiteLoader.ReadStreamingAsset(SiteLoader.SiteFolder + "/ortho.jpg");
            if (bytes == null) return null;
            var direct = new Texture2D(2, 2, TextureFormat.RGB24, true)
            {
                name = "Paint_Photo",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = PhotoAniso,
                hideFlags = HideFlags.DontSave,
            };
            if (!direct.LoadImage(bytes, false)) { DestroySafe(direct); return null; }
            if (direct.width <= size && direct.height <= size)
            {
                direct.wrapMode = TextureWrapMode.Clamp;
                direct.filterMode = FilterMode.Trilinear;
                direct.anisoLevel = PhotoAniso;
                // In the headset the CPU copy is dropped once the mipmaps are on the GPU.
                direct.Apply(true, Application.isPlaying);
                return direct;
            }
            var full = direct;
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(full, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var photo = new Texture2D(size, size, TextureFormat.RGB24, true)
            {
                name = "Paint_Photo",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = PhotoAniso,
                hideFlags = HideFlags.DontSave,
            };
            photo.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            photo.Apply(true, Application.isPlaying);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            DestroySafe(full);
            return photo;
        }

        /// <summary>One line under the verdict: what this terrain is and where it comes from.</summary>
        public string SourceLine()
        {
            if (!Site.IsMarsData)
                return "TEST TERRAIN · synthetic shape for development · not Mars data";
            var info = Site.Info;
            string place = (string.IsNullOrEmpty(info.name) ? "Mars" : info.name.ToUpperInvariant()) + " · " + (Site.SideMeters / 1000f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " km²";
            place += " · " + Math.Abs(info.centerLat).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "° " + (info.centerLat < 0 ? "S" : "N") +
                     ", " + Math.Abs(info.centerLon).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "° " + (info.centerLon < 0 ? "W" : "E");
            var source = Site.ElevationSource;
            if (source != null)
                place += " · heights: " + Join(" ", source.mission, source.instrument) + (string.IsNullOrEmpty(source.resolution) ? "" : ", " + source.resolution);
            return place;
        }

        /// <summary>What the colors of a layer mean, in the panel's rich text.</summary>
        public string LegendText(MapLayer layer)
        {
            switch (layer)
            {
                case MapLayer.Slope:
                    return "SLOPE   " +
                        Colored(TerrainLayers.SlopeGentle, "under " + TerrainLayers.GentleSlopeDegrees.ToString("0") + "°") + "  ·  " +
                        Colored(TerrainLayers.SlopeCareful, TerrainLayers.GentleSlopeDegrees.ToString("0") + "–" + TerrainLayers.SlopeLimitDegrees.ToString("0") + "° with care") + "  ·  " +
                        Colored(TerrainLayers.SlopeNoGo, "over " + TerrainLayers.SlopeLimitDegrees.ToString("0") + "° no-go (hatched)");
                case MapLayer.Return:
                    float worst = 0f;
                    foreach (float m in ReturnMinutes) if (!float.IsInfinity(m) && m > worst) worst = m;
                    string text = "RETURN ON FOOT   ring every " + TerrainLayers.ReturnRingMinutes.ToString("0") + " min  ·  farthest " + worst.ToString("0") + " min";
                    if (!float.IsInfinity(ReturnReachMinutes))
                        text = "RETURN ON FOOT   ring every " + TerrainLayers.ReturnRingMinutes.ToString("0") + " min  ·  " +
                            Colored(TerrainLayers.TooFar, "red: beyond the " + ReturnReachMinutes.ToString("0") + " min the oxygen left covers");
                    if (ReturnIsPlaceholder)
                        text += "\n" + Colored(WalkbackTheme.Watch, ReturnPlaceholderLabel);
                    return text;
                case MapLayer.Photo:
                    var photo = Site.Source("ortho");
                    return "PHOTO   " + (photo != null ? Join(" ", photo.mission, photo.instrument) + ", " + photo.resolution : "orbital photo") +
                        "  ·  on the real relief (" + (Site.MaxElevation - Site.MinElevation).ToString("F0") + " m, not exaggerated)" +
                        (PhotoTinted ? "\nColour: the ground as Curiosity's Mastcam saw it at Nevado Sajama (HiRISE is black and white)" : "");
                default:
                    return "HEIGHT   dark " + Site.MinElevation.ToString("F0") + " m  ·  light " + Site.MaxElevation.ToString("F0") + " m  ·  " +
                        "contour every " + TerrainLayers.ContourMeters.ToString("0") + " m  ·  relief " + (Site.MaxElevation - Site.MinElevation).ToString("F0") + " m, not exaggerated";
            }
        }

        void BuildMesh()
        {
            int n = Site.GridSize;
            float side = Site.SideMeters;
            float step = Site.NodeSpacing;
            float min = Site.MinElevation;
            float[] heights = Site.Heights;

            var vertices = new Vector3[n * n];
            var uv = new Vector2[n * n];
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    int i = r * n + c;
                    vertices[i] = new Vector3(c * step - side / 2f, heights[i] - min, side / 2f - r * step);
                    uv[i] = new Vector2(c / (float)(n - 1), 1f - r / (float)(n - 1));
                }
            }

            var triangles = new int[(n - 1) * (n - 1) * 6];
            int t = 0;
            for (int r = 0; r < n - 1; r++)
            {
                for (int c = 0; c < n - 1; c++)
                {
                    int a = r * n + c, b = a + 1, d = a + n, e = d + 1;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = e;
                    triangles[t++] = a; triangles[t++] = e; triangles[t++] = d;
                }
            }

            DestroySafe(mesh);
            mesh = new Mesh { name = "GaleTerrain", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        static string Colored(Color color, string text)
        {
            return "<color=" + TerrainLayers.Hex((Color32)color) + ">" + text + "</color>";
        }

        static string Join(string separator, params string[] parts)
        {
            string result = "";
            foreach (string p in parts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                result += (result.Length > 0 ? separator : "") + p;
            }
            return result;
        }

        static void DestroySafe(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
