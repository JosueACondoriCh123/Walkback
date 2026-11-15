// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Walkback
{
    /// <summary>
    /// The charts of the mission board, so the plan is seen and not read: the verdict's icon, the
    /// oxygen tank as a bar (used / spare / reserve), the mission's time as a bar (walking and work in
    /// the order they happen) and the height along the route with the stops on it.
    /// They fill in step with the route drawing itself on the map (SetProgress), and a WHAT IF redraws
    /// them at once. Every number is read from the plan and the relief by PlanCharts; with no plan the
    /// charts are empty and show no number (rule 1).
    /// Built at edit time by MissionPanel.Build on the main board's canvas (1 pixel = 1 millimeter).
    /// The shapes themselves are not saved in the scene: ShowPlan or ShowNoPlan draws them after a load
    /// (WalkbackApp.StartPanelPiece does it at start).
    /// </summary>
    public class MissionCharts : MonoBehaviour
    {
        // Layout on the main board, in pixels from its top-left corner.
        public const float IconSize = 150f;
        public const float BarWidth = 645f;
        public const float BarHeight = 40f;
        public const float PlotWidth = 1360f;
        public const float PlotHeight = 150f;
        public const float PlotLeft = 70f;
        public const float PlotTop = 680f;
        public const float StopLabelWidth = 230f;
        public const int MaxStops = 4;

        public static readonly Color Track = new Color(0.20f, 0.22f, 0.26f);
        /// <summary>Oxygen already breathed: a neutral gray, so green and amber keep their meaning.</summary>
        public static readonly Color Used = new Color(0.62f, 0.65f, 0.70f);
        public static readonly Color Spare = WalkbackTheme.Safe;
        public static readonly Color Reserve = WalkbackTheme.Watch;
        public static readonly Color IntoReserve = WalkbackTheme.Danger;
        public static readonly Color Walk = WalkbackTheme.Watch;
        public static readonly Color Work = WalkbackTheme.Live;
        /// <summary>The area under the profile: the route's amber, dimmed, and opaque (nothing writes alpha).</summary>
        public static readonly Color ProfileFill = Color.Lerp(WalkbackTheme.CardBack, WalkbackTheme.Watch, 0.38f);

        public ChartGraphic icon;
        public ChartGraphic tankBar;
        public ChartGraphic timeBar;
        public ChartGraphic profilePlot;
        public Text tankCaption;
        public Text tankUsed;
        public Text tankSpare;
        public Text tankReserve;
        public Text timeCaption;
        public Text timeWalk;
        public Text timeWork;
        public Text timeTotal;
        public Text profileCaption;
        public Text profileNumbers;
        public Text[] stopLabels = new Text[0];

        public bool HasPlan { get { return plan != null; } }
        /// <summary>How much of the plan the charts show, 0..1 (the fraction of the route drawn on the map).</summary>
        public float Progress { get; private set; } = 1f;
        public VerdictState IconState { get; private set; } = VerdictState.NoRoute;
        /// <summary>What the charts show at the current progress (zero without a plan).</summary>
        public PlanCharts.Moment Shown { get; private set; }
        public PlanCharts.Profile CurrentProfile { get { return profile; } }
        /// <summary>How many stops are marked on the profile at the current progress.</summary>
        public int StopsShown { get; private set; }

        MissionFallback.Plan plan;
        PlanCharts.Profile profile;
        List<PlanCharts.Span> spans;
        float totalMinutes;

        // ---------------------------------------------------------------- what the board calls

        /// <summary>Draws the charts for a plan, whole. The texts under the bars are the plan's final numbers.</summary>
        public void ShowPlan(SiteData site, MissionFallback.Plan newPlan)
        {
            if (newPlan == null || !newPlan.HasRoute || newPlan.Legs.Count == 0)
            {
                ShowNoPlan();
                return;
            }
            plan = newPlan;
            profile = PlanCharts.MakeProfile(site, plan);
            spans = PlanCharts.Timeline(plan);
            totalMinutes = plan.TotalMinutes;

            bool whatIf = plan.TankPercent < 100f;
            tankCaption.text = whatIf ? "OXYGEN (estimate)  ·  tank at " + F(plan.TankPercent, "0") + " %" : "OXYGEN (estimate)";
            tankCaption.color = whatIf ? WalkbackTheme.Watch : WalkbackTheme.TextSoft;
            tankUsed.text = "USED " + F(plan.UsedLiters, "0") + " L";
            if (plan.SpareLiters >= 0f)
            {
                tankSpare.text = "SPARE " + F(plan.SpareLiters, "0") + " L";
                tankSpare.color = Spare;
            }
            else
            {
                tankSpare.text = "SHORT " + F(-plan.SpareLiters, "0") + " L";
                tankSpare.color = IntoReserve;
            }
            tankReserve.text = "RESERVE " + F(HudEstimate.ReserveLiters, "0") + " L";
            timeCaption.text = "TIME (estimate)";
            timeWalk.text = "WALK " + HelmetHud.MinutesText(plan.WalkMinutes);
            timeWork.text = "WORK " + HelmetHud.MinutesText(plan.WorkMinutes);
            timeTotal.text = "TOTAL " + HelmetHud.MinutesText(plan.TotalMinutes);
            profileCaption.text = "HEIGHT ALONG THE ROUTE";
            profileNumbers.text = HelmetHud.MetersText(profile.TotalMeters) + "  ·  highest " + Signed(profile.Highest) + " m  ·  lowest " + Signed(profile.Lowest) + " m  ·  habitat = 0";
            PlaceStopLabels();
            Progress = -1f;
            SetProgress(1f);
        }

        /// <summary>No plan: empty tracks, captions only, no number anywhere.</summary>
        public void ShowNoPlan()
        {
            plan = null;
            profile = null;
            spans = null;
            totalMinutes = 0f;
            Shown = new PlanCharts.Moment();
            StopsShown = 0;
            Progress = 1f;
            tankCaption.text = "OXYGEN";
            tankCaption.color = WalkbackTheme.TextSoft;
            timeCaption.text = "TIME";
            profileCaption.text = "HEIGHT ALONG THE ROUTE";
            foreach (Text t in new[] { tankUsed, tankSpare, tankReserve, timeWalk, timeWork, timeTotal, profileNumbers }) t.text = " ";
            foreach (Text label in stopLabels)
            {
                label.text = " ";
                label.gameObject.SetActive(false);
            }
            tankBar.Clear();
            tankBar.Box(0f, 0f, BarWidth, BarHeight, Track);
            tankBar.Apply();
            timeBar.Clear();
            timeBar.Box(0f, 0f, BarWidth, BarHeight, Track);
            timeBar.Apply();
            profilePlot.Clear();
            profilePlot.Box(0f, PlotHeight - 3f, PlotWidth, 3f, Track);
            profilePlot.Apply();
        }

        /// <summary>
        /// Shows the plan up to this fraction of the route (0..1): the profile up to there, the stops
        /// already reached, the time and the oxygen spent by then. The route drawing itself on the map
        /// drives it, so the three charts and the ribbon move together.
        /// </summary>
        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (plan == null || Mathf.Approximately(value, Progress)) return;
            Progress = value;
            Shown = PlanCharts.At(plan, value);
            DrawTank();
            DrawTime();
            DrawProfile();
        }

        /// <summary>The verdict as a shape: a check, a cross, or a bar while there is no route. The shape, not only the colour, tells them apart.</summary>
        public void ShowIcon(VerdictState state)
        {
            IconState = state;
            if (icon == null) return;
            Color color = state == VerdictState.Viable ? WalkbackTheme.Safe : state == VerdictState.NotViable ? WalkbackTheme.Danger : WalkbackTheme.Watch;
            Color cut = WalkbackTheme.CardBack;
            float s = IconSize;
            icon.Clear();
            icon.Disc(new Vector2(s / 2f, s / 2f), s * 0.47f, color);
            float thick = s * 0.12f;
            if (state == VerdictState.Viable)
            {
                Vector2 elbow = new Vector2(s * 0.44f, s * 0.68f);
                icon.Line(new Vector2(s * 0.27f, s * 0.51f), elbow, thick, cut);
                icon.Line(elbow, new Vector2(s * 0.74f, s * 0.33f), thick, cut);
                icon.Disc(elbow, thick / 2f, cut, 16);
            }
            else if (state == VerdictState.NotViable)
            {
                icon.Line(new Vector2(s * 0.31f, s * 0.31f), new Vector2(s * 0.69f, s * 0.69f), thick, cut);
                icon.Line(new Vector2(s * 0.69f, s * 0.31f), new Vector2(s * 0.31f, s * 0.69f), thick, cut);
            }
            else icon.Line(new Vector2(s * 0.28f, s * 0.5f), new Vector2(s * 0.72f, s * 0.5f), thick, cut);
            icon.Apply();
        }

        // ---------------------------------------------------------------- drawing

        void DrawTank()
        {
            PlanCharts.Tank tank = PlanCharts.TankAt(plan, Shown.usedLiters);
            tankBar.Clear();
            tankBar.Box(0f, 0f, BarWidth, BarHeight, Track);
            float x = 0f;
            x = Segment(tankBar, x, tank.used, Used);
            x = Segment(tankBar, x, tank.intoReserve, IntoReserve);
            x = Segment(tankBar, x, tank.spare, Spare);
            Segment(tankBar, x, tank.reserve, Reserve);
            // What was never in the tank (WHAT IF) stays as the dark track on the right.
            tankBar.Apply();
        }

        /// <summary>One part of a bar, with a 2 px dark gap after it. Returns where the next part starts.</summary>
        static float Segment(ChartGraphic bar, float x, float fraction, Color color)
        {
            float width = fraction * BarWidth;
            if (width <= 0f) return x;
            bar.Box(x, 0f, Mathf.Max(width - 2f, 1f), BarHeight, color);
            return x + width;
        }

        void DrawTime()
        {
            timeBar.Clear();
            timeBar.Box(0f, 0f, BarWidth, BarHeight, Track);
            float shown = totalMinutes > 0f ? Shown.TotalMinutes / totalMinutes : 0f;
            foreach (PlanCharts.Span span in spans)
            {
                if (span.start >= shown) break;
                float end = Mathf.Min(span.end, shown);
                float x = span.start * BarWidth, width = (end - span.start) * BarWidth;
                // A whole stretch ends with a 2 px gap; one still filling does not.
                if (end >= span.end) width = Mathf.Max(width - 2f, 1f);
                timeBar.Box(x, 0f, width, BarHeight, span.work ? Work : Walk);
            }
            timeBar.Apply();
        }

        /// <summary>Where a height sits in the plot (pixels from its top).</summary>
        public float PlotY(float height)
        {
            const float top = 14f, bottom = 8f;
            float range = profile.Highest - profile.Lowest;
            float t = range > 0.5f ? (height - profile.Lowest) / range : 0.5f;
            return PlotHeight - bottom - t * (PlotHeight - top - bottom);
        }

        /// <summary>Where a distance along the route sits in the plot (pixels from its left).</summary>
        public float PlotX(float meters)
        {
            return profile.TotalMeters > 0f ? meters / profile.TotalMeters * PlotWidth : 0f;
        }

        void DrawProfile()
        {
            profilePlot.Clear();
            profilePlot.Box(0f, PlotHeight - 3f, PlotWidth, 3f, Track);
            int count = profile.Meters.Length;
            float drawn = Progress * profile.TotalMeters;
            // The habitat's own height, as a thin line across: above it is a climb, below it a descent.
            profilePlot.Box(0f, PlotY(0f) - 1f, PlotWidth, 2f, Track);
            for (int i = 1; i < count; i++)
            {
                float m0 = profile.Meters[i - 1], m1 = profile.Meters[i];
                if (m0 >= drawn) break;
                float h1 = profile.Height[i];
                if (m1 > drawn)
                {
                    // The stretch being drawn now: cut it where the route's drawing is.
                    float t = (drawn - m0) / Mathf.Max(m1 - m0, 1e-4f);
                    h1 = Mathf.Lerp(profile.Height[i - 1], h1, t);
                    m1 = drawn;
                }
                var a = new Vector2(PlotX(m0), PlotY(profile.Height[i - 1]));
                var b = new Vector2(PlotX(m1), PlotY(h1));
                float floor = PlotHeight - 3f;
                profilePlot.Quad(a, b, new Vector2(b.x, floor), new Vector2(a.x, floor), ProfileFill);
                profilePlot.Quad(new Vector2(a.x, a.y - 3f), new Vector2(b.x, b.y - 3f), new Vector2(b.x, b.y + 3f), new Vector2(a.x, a.y + 3f), Walk);
            }

            // The stops reached so far: a white post from the ground line to the profile, a square on it, the name under it.
            StopsShown = 0;
            for (int i = 0; i < profile.StopMeters.Length; i++)
            {
                bool reached = profile.StopMeters[i] <= drawn + 0.5f;
                if (i < stopLabels.Length && stopLabels[i].gameObject.activeSelf != reached) stopLabels[i].gameObject.SetActive(reached);
                if (!reached) continue;
                StopsShown++;
                float x = PlotX(profile.StopMeters[i]), y = PlotY(profile.StopHeight[i]);
                profilePlot.Box(x - 1.5f, y, 3f, PlotHeight - y, WalkbackTheme.Text);
                profilePlot.Box(x - 8f, y - 8f, 16f, 16f, WalkbackTheme.Text);
            }
            for (int i = profile.StopMeters.Length; i < stopLabels.Length; i++)
                if (stopLabels[i].gameObject.activeSelf) stopLabels[i].gameObject.SetActive(false);
            profilePlot.Apply();
        }

        /// <summary>
        /// The stops' names under the plot, each centered under its post. A name that would run into the
        /// one before it goes on the second row.
        /// </summary>
        void PlaceStopLabels()
        {
            float lastRight = float.NegativeInfinity;
            bool lastLow = true;
            for (int i = 0; i < stopLabels.Length; i++)
            {
                Text label = stopLabels[i];
                if (i >= profile.StopNames.Length)
                {
                    label.text = " ";
                    continue;
                }
                label.text = profile.StopNames[i];
                float half = Mathf.Min(label.preferredWidth, StopLabelWidth) / 2f + 8f;
                float center = Mathf.Clamp(PlotLeft + PlotX(profile.StopMeters[i]), PlotLeft + half, PlotLeft + PlotWidth - half);
                bool low = center - half < lastRight && !lastLow;
                var rect = (RectTransform)label.transform;
                rect.anchoredPosition = new Vector2(center - StopLabelWidth / 2f, -(PlotTop + PlotHeight + 4f + (low ? 32f : 0f)));
                lastRight = center + half;
                lastLow = low;
            }
        }

        // ---------------------------------------------------------------- build (edit time)

        /// <summary>Adds the charts to the main board's canvas. Called once by MissionPanel.Build.</summary>
        public void Build(RectTransform board, Font font, Material ui)
        {
            Color soft = WalkbackTheme.TextSoft, text = WalkbackTheme.Text;
            icon = AddChart(board, "VerdictIcon", ui, 84f, MissionPanel.VerdictTop + 35f, IconSize, IconSize);

            // Three cards with air between them: oxygen and time side by side, the height along the route under them.
            const float cardsTop = 434f, smallHigh = 156f, gap = 30f;
            float cardWide = (MissionPanel.PixelsWide - 2f * MissionPanel.Margin - gap) / 2f;
            float rightCard = MissionPanel.Margin + cardWide + gap;
            float profileTop = cardsTop + smallHigh + gap;
            MissionPanel.AddBox(board, "TankCard", ui, WalkbackTheme.CardBack, MissionPanel.Margin, cardsTop, cardWide, smallHigh);
            MissionPanel.AddBox(board, "TimeCard", ui, WalkbackTheme.CardBack, rightCard, cardsTop, cardWide, smallHigh);
            MissionPanel.AddBox(board, "ProfileCard", ui, WalkbackTheme.CardBack, MissionPanel.Margin, profileTop, MissionPanel.PixelsWide - 2f * MissionPanel.Margin, 290f);

            // Oxygen, left card.
            float lx = MissionPanel.Margin + 20f, rx = rightCard + 20f;
            tankCaption = MissionPanel.AddText(board, "TankCaption", "OXYGEN", font, ui, 26, FontStyle.Bold, soft, lx, cardsTop + 12f, BarWidth, 34f);
            tankBar = AddChart(board, "TankBar", ui, lx, cardsTop + 52f, BarWidth, BarHeight);
            tankUsed = MissionPanel.AddText(board, "TankUsed", " ", font, ui, 26, FontStyle.Bold, Used, lx, cardsTop + 104f, 200f, 34f);
            tankSpare = MissionPanel.AddText(board, "TankSpare", " ", font, ui, 26, FontStyle.Bold, Spare, lx + 200f, cardsTop + 104f, 210f, 34f);
            tankReserve = MissionPanel.AddText(board, "TankReserve", " ", font, ui, 26, FontStyle.Bold, Reserve, lx + BarWidth - 235f, cardsTop + 104f, 235f, 34f);
            tankReserve.alignment = TextAnchor.MiddleRight;

            // Time, right card.
            timeCaption = MissionPanel.AddText(board, "TimeCaption", "TIME", font, ui, 26, FontStyle.Bold, soft, rx, cardsTop + 12f, BarWidth, 34f);
            timeBar = AddChart(board, "TimeBar", ui, rx, cardsTop + 52f, BarWidth, BarHeight);
            timeWalk = MissionPanel.AddText(board, "TimeWalk", " ", font, ui, 26, FontStyle.Bold, Walk, rx, cardsTop + 104f, 190f, 34f);
            timeWork = MissionPanel.AddText(board, "TimeWork", " ", font, ui, 26, FontStyle.Bold, Work, rx + 190f, cardsTop + 104f, 220f, 34f);
            timeTotal = MissionPanel.AddText(board, "TimeTotal", " ", font, ui, 26, FontStyle.Bold, text, rx + BarWidth - 235f, cardsTop + 104f, 235f, 34f);
            timeTotal.alignment = TextAnchor.MiddleRight;

            // Height along the route, full width.
            profileCaption = MissionPanel.AddText(board, "ProfileCaption", "HEIGHT ALONG THE ROUTE", font, ui, 26, FontStyle.Bold, soft, PlotLeft, profileTop + 14f, 420f, 34f);
            profileNumbers = MissionPanel.AddText(board, "ProfileNumbers", " ", font, ui, 26, FontStyle.Normal, text, PlotLeft + 440f, profileTop + 14f, PlotWidth - 440f, 34f);
            profileNumbers.alignment = TextAnchor.MiddleRight;
            profilePlot = AddChart(board, "ProfilePlot", ui, PlotLeft, PlotTop, PlotWidth, PlotHeight);
            stopLabels = new Text[MaxStops];
            for (int i = 0; i < MaxStops; i++)
            {
                stopLabels[i] = MissionPanel.AddText(board, "ProfileStop" + i, " ", font, ui, 26, FontStyle.Normal, text, PlotLeft, PlotTop + PlotHeight + 4f, StopLabelWidth, 32f);
                stopLabels[i].alignment = TextAnchor.MiddleCenter;
                stopLabels[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                stopLabels[i].gameObject.SetActive(false);
            }
            ShowIcon(VerdictState.NoRoute);
            ShowNoPlan();
        }

        static ChartGraphic AddChart(RectTransform board, string name, Material ui, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(ChartGraphic));
            MissionPanel.Place(board, (RectTransform)go.transform, x, y, w, h);
            var chart = go.GetComponent<ChartGraphic>();
            chart.raycastTarget = false;
            if (ui != null) chart.material = ui;
            return chart;
        }

        /// <summary>"+77" or "-3": heights relative to the habitat always carry their sign.</summary>
        public static string Signed(float meters)
        {
            int whole = Mathf.RoundToInt(meters);
            return (whole > 0 ? "+" : "") + whole.ToString(CultureInfo.InvariantCulture);
        }

        static string F(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
