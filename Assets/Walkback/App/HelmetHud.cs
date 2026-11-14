// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Globalization;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The helmet HUD (guion §16.4, ficha G; §22.2): what the astronaut's visor shows while standing at
    /// a stop (inside its 360° photo). Few things and big: oxygen left (top left), minutes back to the
    /// habitat (top right), the next stop (bottom left), the 2D map (bottom right), and a red TURN BACK
    /// NOW in the middle only when the way back is no longer covered. Pinned to the view with a slight
    /// lag (§21.6), 1.2 m ahead, everything within about 25° of the center. Numbers come from
    /// HudEstimate and say so until session C's model lands.
    /// </summary>
    public class HelmetHud : MonoBehaviour
    {
        public const float Distance = 1.2f;
        public const float FollowSpeed = 6f;
        /// <summary>
        /// The one line that says what every number on the visor is. Short on purpose (36 characters):
        /// the visor is read at a glance, and the long version is on the verdict card.
        /// </summary>
        public const string EstimateLine = "ESTIMATE · not the walking model yet";

        public WalkbackApp app;
        /// <summary>Everything that shows; inactive while the HUD is off.</summary>
        public Transform content;
        public GlobeLabel oxygenCard;
        public GlobeLabel returnCard;
        public GlobeLabel nextCard;
        public GlobeLabel estimateTag;
        public GlobeLabel warningCard;
        public MiniMap miniMap;

        public bool Visible { get { return content != null && content.gameObject.activeSelf; } }
        public int AtStop { get; private set; } = -1;
        public HudEstimate.State Current { get; private set; }

        /// <summary>Shows the visor as on arrival at stop <paramref name="index"/>.</summary>
        public void ShowAt(int index)
        {
            SiteData site = app != null && app.terrain != null ? app.terrain.Site : null;
            if (site == null || site.Stops.Length == 0) { Hide(); return; }
            // The way back is read from the same return layer the map on the table shows.
            var s = HudEstimate.AtStop(site, index, app.terrain.ReturnMinutes);
            Current = s;
            AtStop = s.stop;

            oxygenCard.SetLines("O2  " + F(s.oxygenLeftPercent, "0") + " %", "oxygen left (estimate)");
            oxygenCard.lines[0].color = s.oxygenLeftPercent > 50f ? WalkbackTheme.Safe : (s.oxygenLeftPercent > 25f ? WalkbackTheme.Watch : WalkbackTheme.Danger);
            returnCard.SetLines(MinutesText(s.returnMinutes), "back to habitat (estimate)");
            returnCard.lines[0].color = s.turnBack ? WalkbackTheme.Danger : WalkbackTheme.Live;
            nextCard.SetLines("NEXT  " + s.nextName,
                MetersText(s.nextMeters) + "  ·  " + MinutesText(s.nextMinutes) + "  ·  O2 -" + F(s.nextOxygenPercent, "0.0") + " %",
                s.taskMinutes > 0f ? "Tasks here: " + F(s.taskMinutes, "0") + " min" : "");
            estimateTag.SetLines(site.IsMarsData ? EstimateLine : EstimateLine + "  ·  TEST TERRAIN, not Mars data");
            warningCard.SetLines("TURN BACK NOW");
            SetWarning(s.turnBack);

            content.gameObject.SetActive(true);
            miniMap.Show(app, s.here, s.nextPoint);
            // Inside a 360° photo the way the headset looks is not a direction on Mars: no heading arrow.
            miniMap.SetHeading(false, 0f);
            if (app.wristMap != null) app.wristMap.SetHeading(false, 0f);
            WalkbackLog.Info("HUD at " + s.stopName + ": O2 " + s.oxygenLeftPercent.ToString("F1") + " %, back " + s.returnMinutes.ToString("F1") + " min, next " + s.nextName + " " + s.nextMeters.ToString("F0") + " m");
        }

        /// <summary>
        /// Mission mode: the visor while walking, from the headset's position (MissionProgress). Same cards
        /// as ShowAt; the numbers come from the plan and the map's return layer, so they stay labelled ESTIMATE.
        /// </summary>
        public void ShowLive(MissionProgress.Snapshot s, SiteData site)
        {
            AtStop = -1;
            oxygenCard.SetLines("O2  " + F(s.oxygenLeftPercent, "0") + " %", "oxygen left (estimate)");
            oxygenCard.lines[0].color = s.oxygenLeftPercent > 50f ? WalkbackTheme.Safe : (s.oxygenLeftPercent > 25f ? WalkbackTheme.Watch : WalkbackTheme.Danger);
            returnCard.SetLines(s.noWayBack ? "NO WAY BACK" : MinutesText(s.returnMinutes), "back to habitat (estimate)");
            returnCard.lines[0].color = s.turnBack ? WalkbackTheme.Danger : WalkbackTheme.Live;
            nextCard.SetLines("NEXT  " + s.nextName,
                s.arrived ? "You are here" : MetersText(s.nextMeters) + "  ·  " + MinutesText(s.nextMinutes),
                s.taskMinutesAtNext > 0f ? "Work there: " + F(s.taskMinutesAtNext, "0") + " min" : "");
            estimateTag.SetLines(site != null && !site.IsMarsData ? EstimateLine + "  ·  TEST TERRAIN, not Mars data" : EstimateLine);
            warningCard.SetLines("TURN BACK NOW");
            SetWarning(s.turnBack);
            content.gameObject.SetActive(true);
            miniMap.Show(app, s.here, s.nextPoint);
        }

        public void Hide()
        {
            if (content != null) content.gameObject.SetActive(false);
            AtStop = -1;
            // Hidden is not "safe": the next time the visor shows with the warning on, it sounds again.
            warningOn = false;
        }

        /// <summary>How many times TURN BACK NOW has come on (each one sounds the alert once).</summary>
        public int TurnBackAlerts { get; private set; }

        public const int AlertRate = 44100;
        public const float AlertSeconds = 0.62f;
        public const float AlertVolume = 0.5f;

        bool warningOn;
        AudioSource alertAudio;

        /// <summary>
        /// Shows or hides TURN BACK NOW. The warning used to be only a red card, easy to miss while looking
        /// at the ground: the moment it comes on (not while it stays on) a two-beep alert sounds.
        /// </summary>
        void SetWarning(bool on)
        {
            warningCard.gameObject.SetActive(on);
            if (on && !warningOn)
            {
                TurnBackAlerts++;
                // Sound only while running: the editor test builds no clips and plays nothing.
                if (Application.isPlaying)
                {
                    if (alertAudio == null)
                    {
                        alertAudio = gameObject.AddComponent<AudioSource>();
                        alertAudio.playOnAwake = false;
                        alertAudio.spatialBlend = 0f;
                        alertAudio.volume = AlertVolume;
                        float[] tone = AlertTone(AlertRate);
                        alertAudio.clip = AudioClip.Create("TurnBackAlert", tone.Length, 1, AlertRate, false);
                        alertAudio.clip.SetData(tone, 0);
                    }
                    alertAudio.Play();
                }
                WalkbackLog.Info("HUD: TURN BACK NOW came on");
            }
            warningOn = on;
        }

        /// <summary>
        /// The alert, made in code like the pointer's click and the globe's chime (no audio file to credit):
        /// two 0.22 s beeps at 880 Hz with 0.12 s of silence between them, each with a soft start and end.
        /// </summary>
        public static float[] AlertTone(int rate)
        {
            int n = Mathf.RoundToInt(rate * AlertSeconds);
            var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                // Beeps at 0.00–0.22 s and 0.34–0.56 s.
                float inBeep = t < 0.22f ? t : (t >= 0.34f && t < 0.56f ? t - 0.34f : -1f);
                if (inBeep < 0f) continue;
                float envelope = Mathf.Clamp01(inBeep / 0.01f) * Mathf.Clamp01((0.22f - inBeep) / 0.03f);
                samples[i] = 0.6f * envelope * Mathf.Sin(2f * Mathf.PI * 880f * t);
            }
            return samples;
        }

        /// <summary>Jumps to its place in front of <paramref name="head"/> (on entering a stop).</summary>
        public void SnapTo(Transform head)
        {
            if (head == null) return;
            transform.SetPositionAndRotation(head.position + head.forward * Distance, head.rotation);
        }

        /// <summary>Follows the head with a slight lag, so the visor feels steady.</summary>
        public void Follow(Transform head, float deltaTime)
        {
            if (head == null) return;
            float k = 1f - Mathf.Exp(-FollowSpeed * deltaTime);
            transform.position = Vector3.Lerp(transform.position, head.position + head.forward * Distance, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, head.rotation, k);
        }

        public static string MinutesText(float minutes)
        {
            if (minutes < 1f) return "under 1 min";
            if (minutes < 60f) return F(minutes, "0") + " min";
            int h = Mathf.FloorToInt(minutes / 60f);
            return h + " h " + Mathf.RoundToInt(minutes - h * 60f) + " min";
        }

        public static string MetersText(float meters)
        {
            return meters >= 1000f ? F(meters / 1000f, "0.0") + " km" : F(meters, "0") + " m";
        }

        /// <summary>Numbers on the visor always with a decimal point (the UI is in English, whatever the device language).</summary>
        static string F(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
