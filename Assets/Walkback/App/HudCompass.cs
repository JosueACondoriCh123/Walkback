// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Globalization;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The compass tape between the two top cards of the helmet HUD, for mission mode only: there Mars is anchored in
    /// the room, so the way the headset looks is a direction on Mars. (Inside a 360° photo it is not,
    /// and the tape hides.) The tape shows 40° to each side of where the astronaut looks: letters every
    /// 45° (N, NE, E...), a tick every 15°, a cyan notch in the middle (you) and an amber bar at the
    /// next stop's bearing. When the stop is outside the window the bar waits at that end of the tape.
    /// Only transforms move: no text is rewritten while turning.
    /// </summary>
    public class HudCompass : MonoBehaviour
    {
        public const float WidthMeters = 0.40f;
        public const float HeightMeters = 0.05f;
        public const float HalfWindowDegrees = 40f;
        public const float MetersPerDegree = WidthMeters / (2f * HalfWindowDegrees);
        public const float TickStepDegrees = 15f;
        public const float LetterStepDegrees = 45f;
        /// <summary>Half the width of a letter's card: a letter shows only while it is whole inside the tape.</summary>
        public const float LetterHalfMeters = 0.03f;
        public static readonly string[] Letters = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        /// <summary>One per entry of tickAngles.</summary>
        public Transform[] ticks = new Transform[0];
        public float[] tickAngles = new float[0];
        /// <summary>Letter i stands at i * 45° (Letters[i]).</summary>
        public GlobeLabel[] letters = new GlobeLabel[0];
        public Transform stopMarker;

        public float Heading { get; private set; }
        public bool HasTarget { get; private set; }
        /// <summary>Degrees from where the astronaut looks to the next stop: + to the right, - to the left.</summary>
        public float TargetOffset { get; private set; }
        /// <summary>True when the next stop is outside the tape's window (the amber bar waits at that end).</summary>
        public bool TargetOffTape { get; private set; }

        /// <summary>Slides the tape to this heading (degrees clockwise from north) and puts the next stop's bar.</summary>
        public void Show(float headingDegrees, bool hasTarget, float bearingDegrees)
        {
            Heading = Mathf.Repeat(headingDegrees, 360f);
            float half = WidthMeters / 2f;
            for (int i = 0; i < ticks.Length && i < tickAngles.Length; i++) Slide(ticks[i], tickAngles[i], half - 0.003f);
            for (int i = 0; i < letters.Length; i++)
                if (letters[i] != null) Slide(letters[i].transform, i * LetterStepDegrees, half - LetterHalfMeters);

            HasTarget = hasTarget;
            TargetOffset = hasTarget ? Offset(bearingDegrees, Heading) : 0f;
            TargetOffTape = hasTarget && Mathf.Abs(TargetOffset) > HalfWindowDegrees;
            if (stopMarker == null) return;
            if (stopMarker.gameObject.activeSelf != hasTarget) stopMarker.gameObject.SetActive(hasTarget);
            if (!hasTarget) return;
            Vector3 p = stopMarker.localPosition;
            float limit = half - stopMarker.localScale.x / 2f;
            stopMarker.localPosition = new Vector3(Mathf.Clamp(TargetOffset * MetersPerDegree, -limit, limit), p.y, p.z);
        }

        void Slide(Transform mark, float angle, float limit)
        {
            if (mark == null) return;
            float x = Offset(angle, Heading) * MetersPerDegree;
            bool inside = Mathf.Abs(x) <= limit;
            if (mark.gameObject.activeSelf != inside) mark.gameObject.SetActive(inside);
            if (!inside) return;
            Vector3 p = mark.localPosition;
            mark.localPosition = new Vector3(x, p.y, p.z);
        }

        // ---------------------------------------------------------------- pure functions (the test calls them directly)

        /// <summary>Degrees from the heading to an angle, both clockwise from north: + = to the right, in -180..180.</summary>
        public static float Offset(float angleDegrees, float headingDegrees)
        {
            return Mathf.DeltaAngle(headingDegrees, angleDegrees);
        }

        /// <summary>Bearing from one point of the site to another (local meters, x east, y north): degrees clockwise from north, 0..360.</summary>
        public static float Bearing(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            if (d.sqrMagnitude < 1e-8f) return 0f;
            return Mathf.Repeat(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 360f);
        }

        /// <summary>The nearest of the eight compass points.</summary>
        public static string Cardinal(float degrees)
        {
            int index = Mathf.RoundToInt(Mathf.Repeat(degrees, 360f) / LetterStepDegrees) % Letters.Length;
            return Letters[index];
        }

        /// <summary>"045° NE": three digits, as bearings are read, and the nearest compass point.</summary>
        public static string BearingText(float degrees)
        {
            int whole = Mathf.RoundToInt(Mathf.Repeat(degrees, 360f)) % 360;
            return whole.ToString("000", CultureInfo.InvariantCulture) + "° " + Cardinal(whole);
        }

        /// <summary>The height difference to the next stop, from the relief: "climb 12 m", "descent 8 m" or "level" (under half a meter).</summary>
        public static string ClimbText(float meters)
        {
            if (Mathf.Abs(meters) < 0.5f) return "level";
            string amount = Mathf.Abs(meters).ToString("0", CultureInfo.InvariantCulture) + " m";
            return meters > 0f ? "climb " + amount : "descent " + amount;
        }
    }
}
