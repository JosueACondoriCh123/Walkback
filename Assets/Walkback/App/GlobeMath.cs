// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;
using UnityEngine.Rendering;

namespace Walkback
{
    /// <summary>
    /// One place for the planet's geometry, so the globe mesh, its image and the site light
    /// all use the same latitude/longitude math (guion trap 11), and one place for the
    /// inside-out panorama sphere, seen from inside and not mirrored (trap 12).
    /// Globe frame: +y = north pole; latitude 0, longitude 0 faces -z (toward a viewer in
    /// front); east longitude grows to the viewer's right, as on a map seen from outside.
    /// </summary>
    public static class GlobeMath
    {
        /// <summary>Gale crater's published center (IAU planetary nomenclature): 5.4° S, 137.8° E.</summary>
        public const float GaleLatitude = -5.4f;
        public const float GaleLongitude = 137.8f;

        /// <summary>
        /// Longitude of the LEFT edge of an equirectangular global image (east positive).
        /// -180 = image spans 180° W to 180° E. If session B's mars_global.jpg spans 0° to 360° E, set 0.
        /// </summary>
        public const float GlobeImageWestEdge = -180f;

        /// <summary>A point on a sphere of the given radius, in the globe's local frame.</summary>
        public static Vector3 LatLonToLocal(float latDeg, float lonDeg, float radius)
        {
            float lat = latDeg * Mathf.Deg2Rad, lon = lonDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), -Mathf.Cos(lat) * Mathf.Cos(lon)) * radius;
        }

        /// <summary>The inverse: latitude and longitude (degrees, longitude in -180..180) of a local direction.</summary>
        public static Vector2 LocalToLatLon(Vector3 local)
        {
            Vector3 d = local.normalized;
            float lat = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
            float lon = Mathf.Atan2(d.x, -d.z) * Mathf.Rad2Deg;
            return new Vector2(lat, lon);
        }

        /// <summary>Texture coordinate of a latitude/longitude on an equirectangular image (v = 1 is the north edge).</summary>
        public static Vector2 LatLonToUV(float latDeg, float lonDeg, float westEdge)
        {
            float u = Mathf.Repeat(lonDeg - westEdge, 360f) / 360f;
            return new Vector2(u, (latDeg + 90f) / 180f);
        }

        /// <summary>
        /// A UV sphere seen from outside, its image mapped with <see cref="LatLonToUV"/>. The seam
        /// sits at the image's west edge so u runs 0..1 without a jump.
        /// </summary>
        public static Mesh BuildGlobeMesh(float radius, int latSegments, int lonSegments, float westEdge)
        {
            int rows = latSegments + 1, cols = lonSegments + 1;
            var vertices = new Vector3[rows * cols];
            var normals = new Vector3[rows * cols];
            var uv = new Vector2[rows * cols];
            for (int i = 0; i < rows; i++)
            {
                float lat = -90f + 180f * i / latSegments;
                for (int j = 0; j < cols; j++)
                {
                    float lon = westEdge + 360f * j / lonSegments;
                    int k = i * cols + j;
                    vertices[k] = LatLonToLocal(lat, lon, radius);
                    normals[k] = vertices[k] / radius;
                    uv[k] = new Vector2(j / (float)lonSegments, (lat + 90f) / 180f);
                }
            }
            var triangles = new int[latSegments * lonSegments * 6];
            int t = 0;
            for (int i = 0; i < latSegments; i++)
            {
                for (int j = 0; j < lonSegments; j++)
                {
                    // a = south-west, b = south-east, c = north-west, d = north-east (seen from outside: east is right).
                    int a = i * cols + j, b = a + 1, c = a + cols, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = d;
                    triangles[t++] = a; triangles[t++] = d; triangles[t++] = b;
                }
            }
            var mesh = new Mesh { name = "Globe", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Direction for a panorama pixel column/row: azimuth 0 = straight ahead (+z), 90 = to the
        /// right (+x); elevation 0 = horizon. An equirectangular panorama reads left to right while
        /// turning right, and its center column faces forward when you enter.
        /// </summary>
        public static Vector3 PanoramaDirection(float azimuthDeg, float elevationDeg)
        {
            float az = azimuthDeg * Mathf.Deg2Rad, el = elevationDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
        }

        /// <summary>The panorama's u (0..1) for an azimuth: u = 0.5 straight ahead, 0.75 to the right.</summary>
        public static float PanoramaU(float azimuthDeg)
        {
            return Mathf.Repeat(azimuthDeg / 360f + 0.5f, 1f);
        }

        /// <summary>
        /// A sphere seen from INSIDE: triangles face the center, and the image is not mirrored
        /// (trap 12: the right half of the photo is on the viewer's right).
        /// </summary>
        public static Mesh BuildPanoramaMesh(float radius, int latSegments, int lonSegments)
        {
            int rows = latSegments + 1, cols = lonSegments + 1;
            var vertices = new Vector3[rows * cols];
            var normals = new Vector3[rows * cols];
            var uv = new Vector2[rows * cols];
            for (int i = 0; i < rows; i++)
            {
                float el = -90f + 180f * i / latSegments;
                for (int j = 0; j < cols; j++)
                {
                    // u from 0 to 1 = azimuth from -180 (behind, on the left) to +180 (behind, on the right).
                    float u = j / (float)lonSegments;
                    float az = (u - 0.5f) * 360f;
                    int k = i * cols + j;
                    vertices[k] = PanoramaDirection(az, el) * radius;
                    normals[k] = -vertices[k] / radius;
                    uv[k] = new Vector2(u, (el + 90f) / 180f);
                }
            }
            var triangles = new int[latSegments * lonSegments * 6];
            int t = 0;
            for (int i = 0; i < latSegments; i++)
            {
                for (int j = 0; j < lonSegments; j++)
                {
                    // Seen from inside, increasing azimuth runs to the right: same corner order as the
                    // globe seen from outside, so the triangles face the center.
                    int a = i * cols + j, b = a + 1, c = a + cols, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = d;
                    triangles[t++] = a; triangles[t++] = d; triangles[t++] = b;
                }
            }
            var mesh = new Mesh { name = "PanoramaSphere", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The globe while no global Mars image is loaded: one plain rust color with a faint
        /// latitude/longitude grid every 30°. It is not a picture of Mars and the label says so.
        /// </summary>
        public static Texture2D PlainSphereTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "GlobePlain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontSave,
            };
            Color32 baseColor = new Color32(156, 82, 50, 255);
            Color32 grid = new Color32(196, 128, 92, 255);
            Color32 equator = new Color32(222, 160, 120, 255);
            var pixels = new Color32[width * height];
            float pxPerDegX = width / 360f, pxPerDegY = height / 180f;
            for (int y = 0; y < height; y++)
            {
                float lat = (y + 0.5f) / height * 180f - 90f;
                bool latLine = Mathf.Abs(Mathf.Repeat(lat + 15f, 30f) - 15f) * pxPerDegY < 0.9f;
                bool equatorLine = Mathf.Abs(lat) * pxPerDegY < 1.4f;
                for (int x = 0; x < width; x++)
                {
                    float lon = (x + 0.5f) / width * 360f;
                    bool lonLine = Mathf.Abs(Mathf.Repeat(lon + 15f, 30f) - 15f) * pxPerDegX < 0.9f;
                    pixels[y * width + x] = equatorLine ? equator : (latLine || lonLine ? grid : baseColor);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }

        /// <summary>Rotation that turns the globe so <paramref name="localPoint"/> faces <paramref name="towardWorld"/>, north kept up.</summary>
        public static Quaternion FaceRotation(Vector3 localPoint, Vector3 towardWorld)
        {
            Vector3 fl = localPoint.normalized;
            Vector3 ul = Vector3.ProjectOnPlane(Vector3.up, fl);
            if (ul.sqrMagnitude < 1e-6f) ul = Vector3.forward;
            Vector3 fw = towardWorld.normalized;
            Vector3 uw = Vector3.ProjectOnPlane(Vector3.up, fw);
            if (uw.sqrMagnitude < 1e-6f) uw = Vector3.forward;
            return Quaternion.LookRotation(fw, uw) * Quaternion.Inverse(Quaternion.LookRotation(fl, ul));
        }

        /// <summary>Smooth 0..1 ease (cubic). Not Mathf.SmoothStep, which blends two values (trap 15).</summary>
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
