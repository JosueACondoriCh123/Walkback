// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Walkback.EditorTools
{
    /// <summary>Panel piece checks: verdict card, layer buttons, stop card, conditions, side board.</summary>
    public static partial class AutoTest
    {
        const string ViableTestLine = "O2 used 23 % · margin to return 5 h 40 min · reserve 30 min untouched";

        static partial void CheckPanelPiece(WalkbackApp app)
        {
            var panel = app.panel;
            panel.Bind(app);
            CheckPanelButtons(app, panel);
            CheckPanelVerdict(panel);
            CheckPanelSideBoard(app, panel);
            CheckPanelConditions(panel);
            CheckPanelTextFits(panel);
        }

        static void CheckPanelButtons(WalkbackApp app, MissionPanel panel)
        {
            var buttons = panel.layerButtons;
            Check("Panel: four layer buttons (PHOTO first on screen, then HEIGHT, SLOPE, RETURN)", buttons.Length == 4 && Array.TrueForAll(buttons, b => b != null) &&
                buttons[0].label.text == "HEIGHT" && buttons[1].label.text == "SLOPE" && buttons[2].label.text == "RETURN" && buttons[3].label.text == "PHOTO" &&
                buttons[3].transform.position.x < buttons[0].transform.position.x);

            bool collidersMatch = true, targets = true;
            foreach (var b in buttons)
            {
                var box = b.GetComponent<BoxCollider>();
                var r = (RectTransform)b.transform;
                if (box == null || Mathf.Abs(box.size.x - r.sizeDelta.x) > 0.5f || Mathf.Abs(box.size.y - r.sizeDelta.y) > 0.5f ||
                    Mathf.Abs(box.center.x - r.sizeDelta.x / 2f) > 0.5f || Mathf.Abs(box.center.y + r.sizeDelta.y / 2f) > 0.5f) collidersMatch = false;
                if (b.GetComponent<IPointerTarget>() == null) targets = false;
            }
            Check("Panel: each layer button has a collider the size of the button", collidersMatch);
            Check("Panel: each layer button is a pointer target", targets);

            // The pointer will find the button: a ray from the user's eyes to its center hits its collider.
            Physics.SyncTransforms();
            var slope = buttons[1];
            Vector3 center = slope.GetComponent<BoxCollider>().bounds.center;
            RaycastHit hit;
            bool hits = Physics.Raycast(MissionPanel.UserEye, (center - MissionPanel.UserEye).normalized, out hit, 5f);
            Check("Panel: a ray from the user's eyes reaches the SLOPE button", hits && hit.collider.GetComponentInParent<IPointerTarget>() == (IPointerTarget)slope,
                hits ? "hit " + hit.collider.name + " at " + hit.distance.ToString("F2") + " m" : "no hit");

            slope.OnPointerEnter();
            Check("Panel: hover lights the button", slope.Hovered && slope.background.color == PanelButton.HoverBack);
            int before = slope.Presses;
            slope.OnPointerClick(center);
            app.RefreshPanel();
            Check("Panel: pressing SLOPE shows the slope layer and its legend", app.terrain.Layer == MapLayer.Slope && panel.legendLine.text.StartsWith("SLOPE") && slope.Presses == before + 1);
            Check("Panel: the active layer button is amber, the others are not",
                slope.Selected && !buttons[0].Selected && !buttons[2].Selected && slope.background.color == PanelButton.SelectedBack && buttons[0].background.color != PanelButton.SelectedBack);
            slope.OnPointerExit();
            Check("Panel: leaving the button ends the hover", !slope.Hovered);
            buttons[2].OnPointerClick(buttons[2].transform.position);
            Check("Panel: pressing RETURN ON FOOT shows the return layer", app.terrain.Layer == MapLayer.Return && buttons[2].Selected && !slope.Selected);
            buttons[0].OnPointerClick(buttons[0].transform.position);
            app.RefreshPanel();
            Check("Panel: pressing HEIGHT goes back to the height layer", app.terrain.Layer == MapLayer.Height && buttons[0].Selected && panel.legendLine.text.StartsWith("HEIGHT"));
        }

        static void CheckPanelVerdict(MissionPanel panel)
        {
            panel.ShowVerdict(VerdictState.Viable, null, ViableTestLine, null);
            Check("Panel: VIABLE is green, with the ficha A summary line", panel.verdictTitle.text == "VIABLE" && panel.verdictTitle.color == WalkbackTheme.Safe &&
                panel.verdictAccent.color == WalkbackTheme.Safe && panel.verdictDetail.text == ViableTestLine);
            panel.ShowVerdict(VerdictState.NotViable, "NO SAFE ROUTE", "Every route crosses slopes over 20°", "TEST");
            var rect = (RectTransform)panel.verdictTitle.transform;
            Check("Panel: NOT VIABLE / NO SAFE ROUTE is red and fits on one line", panel.verdictTitle.color == WalkbackTheme.Danger && panel.verdictAccent.color == WalkbackTheme.Danger &&
                panel.verdictTitle.preferredWidth <= rect.rect.width, panel.verdictTitle.preferredWidth.ToString("F0") + " of " + rect.rect.width.ToString("F0") + " px");
            Check("Panel: the verdict stays the biggest text", panel.verdictTitle.fontSize > panel.title.fontSize && panel.verdictTitle.fontSize > panel.verdictDetail.fontSize);
            panel.ShowNoRoute();
            Check("Panel: back to NO ROUTE YET, with no number on the card (rule 1)", panel.verdictTitle.text == "NO ROUTE YET" && panel.Verdict == VerdictState.NoRoute &&
                panel.verdictTitle.color == WalkbackTheme.Watch && !HasDigit(panel.verdictDetail.text) && !HasDigit(panel.verdictNote.text));
        }

        /// <summary>Relative luminance of a color (WCAG 2), for the contrast between text and its box.</summary>
        static float Luminance(Color c)
        {
            Color l = c.linear;
            return 0.2126f * l.r + 0.7152f * l.g + 0.0722f * l.b;
        }

        static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        static void CheckTextColor()
        {
            Color text = WalkbackTheme.Text;
            float onPanel = Contrast(text, WalkbackTheme.PanelBack), onCard = Contrast(text, WalkbackTheme.CardBack);
            Check("Theme: text is not pure white (it glares on the headset) and still reads on the dark boxes (contrast 7:1 or more)",
                text.r < 0.99f && text.g < 0.99f && text.b < 0.99f && text.r >= 0.85f && onPanel >= 7f && onCard >= 7f,
                "gray " + text.r.ToString("F2") + ", " + onPanel.ToString("F1") + ":1 on the panel, " + onCard.ToString("F1") + ":1 on a card");
        }

        static void CheckPanelSideBoard(WalkbackApp app, MissionPanel panel)
        {
            CheckTextColor();
            CheckControlsCard(panel);
            var side = panel.sideBoard;
            Check("Panel: side board present", side != null);
            if (side == null) return;
            Vector3 toBoard = side.position - MissionPanel.UserEye;
            Vector3 flat = new Vector3(toBoard.x, 0f, toBoard.z).normalized;
            Check("Panel: side board faces the user", Vector3.Dot(side.forward, flat) > 0.99f, "yaw " + side.eulerAngles.y.ToString("F0") + "°");
            Check("Panel: side board is 1.6 to 2.4 m from the user", toBoard.magnitude >= 1.6f && toBoard.magnitude <= 2.4f, toBoard.magnitude.ToString("F2") + " m");
            Vector3 innerEdge = side.position - side.right * (MissionPanel.SidePixelsWide * MissionPanel.MetersPerPixel / 2f);
            Check("Panel: side board does not cover the main board", innerEdge.x > WalkbackTheme.PanelWidthMeters / 2f + 0.02f, "inner edge at x " + innerEdge.x.ToString("F2") + " m");

            bool sizesOk = true;
            string smallest = "";
            foreach (var t in side.GetComponentsInChildren<Text>(true))
            {
                int size = t.resizeTextForBestFit ? t.resizeTextMinSize : t.fontSize;
                int needed = t.resizeTextForBestFit ? 24 : 28;
                if (size < needed) { sizesOk = false; smallest = t.name + " " + size + " px"; }
            }
            Check("Panel: side board text is 28 px or more (stop names 24 or more)", sizesOk, smallest);

            StopInfo[] stops = app.terrain.Site.Stops;
            app.SelectStop(-1);
            Check("Panel: with no stop chosen, the card says how to choose one", panel.stopName.text == "NO STOP SELECTED" && panel.stopBadge.text.Contains("pillar") && !panel.jumpButton.Interactable);
            Check("Panel: one button per stop", panel.stopButtons.Count == Mathf.Min(stops.Length, 4), panel.stopButtons.Count + " buttons, " + stops.Length + " stops");
            if (panel.stopButtons.Count == 0) return;

            panel.stopButtons[0].OnPointerClick(panel.stopButtons[0].transform.position);
            Check("Panel: pressing a stop button selects that stop", app.SelectedStop == 0 && panel.stopButtons[0].Selected);
            Check("Panel: the stop card shows its name, science and tasks with minutes",
                panel.stopName.text == stops[0].name && panel.stopScience.text == stops[0].science && panel.stopTasks.text.Contains("min"), panel.stopTasks.text);
            if (!app.terrain.Site.StopsAreReal)
                Check("Panel: a TEST STOP reads as test data", panel.stopBadge.text.Contains("TEST") && panel.stopBadge.color == WalkbackTheme.Watch, panel.stopBadge.text);
            else
                Check("Panel: a real stop shows its distance to the habitat", panel.stopBadge.text.Contains(" m from the habitat"), panel.stopBadge.text);

            int jumped = -2;
            Action<int> listen = i => jumped = i;
            app.JumpRequested += listen;
            panel.jumpButton.OnPointerClick(panel.jumpButton.transform.position);
            app.JumpRequested -= listen;
            Check("Panel: JUMP TO SITE asks the app to jump to the chosen stop", jumped == 0, "jump request for stop " + jumped);

            if (panel.stopButtons.Count > 1)
            {
                app.SelectStop(1);
                Check("Panel: choosing another stop (from a pillar too) updates the card", panel.stopName.text == stops[1].name && panel.stopButtons[1].Selected && !panel.stopButtons[0].Selected);
            }
            Check("Panel: tasks line adds up the minutes",
                MissionPanel.TasksLine(new[] { new StopTask { name = "Describe", minutes = 5f }, new StopTask { name = "Collect", minutes = 11f }, new StopTask { name = "Photo", minutes = 2f } })
                    .EndsWith("18 min of work"));
            app.SelectStop(-1);
        }

        static void CheckPanelConditions(MissionPanel panel)
        {
            if (!panel.ConditionsHaveData)
                Check("Panel: no conditions.json -> 'waiting for data' and no numbers", panel.conditionsText.text == Conditions.WaitingText && !HasDigit(panel.conditionsText.text));
            else
                Check("Panel: conditions.json from session B is on the card", panel.conditionsText.text.Length > 0, panel.conditionsText.text.Replace("\n", " | "));

            const string sample = "{\"rems\":{\"sol\":4720,\"earthDate\":\"2026-11-10\",\"air\":{\"min\":-78.2,\"max\":-4.6},\"ground\":{\"min\":-85,\"max\":3}," +
                "\"pressure\":822,\"source\":\"NASA/JPL-Caltech/CAB(CSIC-INTA)\"},\"donki\":{\"from\":\"2026-10-15\",\"to\":\"2026-11-14\"," +
                "\"events\":[{\"activityType\":\"CME\"},{\"activityType\":\"FLR\"}],\"checked\":\"2026-11-14\"},\"season\":{\"value\":\"Ls 22°, early southern autumn\",\"source\":\"Hope/EMIRS\"}," +
                "\"daylight\":\"06:02–17:58 local solar time\",\"radiation\":{\"value\":\"0.64 mSv/day\",\"source\":\"Curiosity RAD\"}}";
            var view = Conditions.Describe(sample);
            string t = view.Text;
            Check("Conditions: REMS sol, Earth date, air, ground and pressure", view.HasData && t.Contains("sol 4720") && t.Contains("Earth date 2026-11-10") &&
                t.Contains("Air -78 / -5 °C") && t.Contains("Ground -85 / 3 °C") && t.Contains("Pressure 822 Pa"), t.Replace("\n", " | "));
            Check("Conditions: season, daylight and radiation with their sources", t.Contains("Season: Ls 22°") && t.Contains("Hope/EMIRS") && t.Contains("Daylight: 06:02") && t.Contains("Radiation: 0.64 mSv/day"));
            Check("Conditions: DONKI window, event count and types", t.Contains("2026-10-15 to 2026-11-14") && t.Contains("2 events (CME, FLR)") && t.Contains("checked 2026-11-14"));

            const string other = "{\"REMS\":{\"Sol\":\"4720\",\"terrestrial_date\":\"2026-11-10\",\"min_air_temp_K\":195,\"max_air_temp_K\":268,\"min_ground_temp_K\":188,\"max_ground_temp_K\":276,\"pressure_hPa\":8.2}}";
            string t2 = Conditions.Describe(other).Text;
            Check("Conditions: other field names, kelvin and hPa are understood", t2.Contains("sol 4720") && t2.Contains("Air -78 / -5 °C") && t2.Contains("Ground -85 / 3 °C") && t2.Contains("Pressure 820 Pa"), t2.Replace("\n", " | "));

            var broken = Conditions.Describe("{not json");
            Check("Conditions: a broken file says so and shows no numbers", !broken.HasData && broken.Text.Contains("could not be read") && !HasDigit(broken.Text));
            Check("Conditions: an empty file waits for data", Conditions.Describe("{}").Text == Conditions.WaitingText);
        }

        /// <summary>The controls card: there, readable, facing the user, clear of the main board, every control on one line.</summary>
        static void CheckControlsCard(MissionPanel panel)
        {
            GlobeLabel card = panel.controlsCard;
            Check("Controls card: next to the table, a child of the panel (it hides and moves with it)", card != null && card.transform.IsChildOf(panel.transform) && card.gameObject.activeSelf);
            if (card == null) return;
            string all = "";
            foreach (var line in card.lines) all += line.text + "\n";
            Check("Controls card: says how to change layers, WHAT IF, MISSION, JUMP, MAP and RECENTER",
                all.Contains("LAYERS") && all.Contains("WHAT IF") && all.Contains("MISSION") && all.Contains("JUMP") && all.Contains("MAP · X") && all.Contains("RECENTER"));
            Canvas.ForceUpdateCanvases();
            string wide = "";
            float smallest = float.MaxValue;
            bool uiOnly = true;
            foreach (var line in card.lines)
            {
                float box = ((RectTransform)line.transform).rect.width;
                if (line.preferredWidth > box) wide += line.text + " " + line.preferredWidth.ToString("F0") + "/" + box.ToString("F0") + " px; ";
                smallest = Mathf.Min(smallest, line.fontSize * line.transform.lossyScale.y);
                if (line.material == null || line.material.shader.name != "Walkback/UI") uiOnly = false;
            }
            Vector3 toCard = card.transform.position - MissionPanel.UserEye;
            float meters = toCard.magnitude;
            // 17 mm at 1.2 m is the project's floor for text; farther away the letters must be that much taller.
            float needed = 0.017f * meters / 1.2f;
            Check("Controls card: every control on one line, letters big enough for its distance, never writing alpha",
                wide.Length == 0 && smallest >= needed && uiOnly, wide.Length > 0 ? wide : (smallest * 1000f).ToString("F0") + " mm letters at " + meters.ToString("F2") + " m (needs " + (needed * 1000f).ToString("F0") + " mm)");
            toCard.y = 0f;
            Vector3 innerEdge = card.transform.position + card.transform.right * (card.backPlate.lossyScale.x / 2f);
            Check("Controls card: faces the user and does not cover the main board",
                Vector3.Dot(card.transform.forward, toCard.normalized) > 0.99f && innerEdge.x < -(WalkbackTheme.PanelWidthMeters / 2f + 0.02f),
                "inner edge at x " + innerEdge.x.ToString("F2") + " m");
        }

        /// <summary>Every fixed text on both boards fits its box (a text spilling over the next one reads as a bug on the projector).</summary>
        static void CheckPanelTextFits(MissionPanel panel)
        {
            Canvas.ForceUpdateCanvases();
            string overflow = "";
            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                if (text.resizeTextForBestFit) continue;
                var r = ((RectTransform)text.transform).rect;
                if (text.preferredHeight > r.height + 6f) overflow += text.name + " " + text.preferredHeight.ToString("F0") + "/" + r.height.ToString("F0") + " px; ";
            }
            Check("Panel: every text fits its box", overflow.Length == 0, overflow);

            string wrapped = "";
            foreach (var button in panel.GetComponentsInChildren<PanelButton>(true))
            {
                if (button.label.resizeTextForBestFit) continue;
                var r = ((RectTransform)button.label.transform).rect;
                if (button.label.preferredWidth > r.width) wrapped += button.label.text + " " + button.label.preferredWidth.ToString("F0") + "/" + r.width.ToString("F0") + " px; ";
            }
            Check("Panel: every button label fits on one line", wrapped.Length == 0, wrapped);

            // Every button is at least 100 mm tall in the room (2.9° at 2 m: a hand ray hits it), and none
            // shrinks its text to fit: a name too long for its button must fail the check above, not get small.
            string small = "";
            bool shrinks = false;
            float lowest = float.MaxValue;
            foreach (var button in panel.GetComponentsInChildren<PanelButton>(true))
            {
                var rt = (RectTransform)button.transform;
                float tall = rt.rect.height * Mathf.Abs(rt.lossyScale.y);
                lowest = Mathf.Min(lowest, tall);
                if (tall < 0.0999f) small += button.label.text + " " + (tall * 1000f).ToString("F0") + " mm; ";
                if (button.label.resizeTextForBestFit) shrinks = true;
            }
            Check("Panel: every button is at least 100 mm tall and keeps its text size", small.Length == 0 && !shrinks && lowest < float.MaxValue,
                small.Length > 0 ? small : (lowest * 1000f).ToString("F0") + " mm the lowest");
        }

        static partial void ShotsPanelPiece(WalkbackApp app)
        {
            var panel = app.panel;
            var cam = app.desktopCamera;
            Vector3 homePosition = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            Vector3 panelEye = new Vector3(0f, 1.5f, 0.35f);

            // 1. VIABLE, slope layer on, the pointer over RETURN ON FOOT. Hand-made test values, said on the card.
            app.terrain.ShowLayer(MapLayer.Slope);
            app.RefreshPanel();
            panel.layerButtons[2].OnPointerEnter();
            panel.ShowVerdict(VerdictState.Viable, null, ViableTestLine, "TEST VALUES for this screenshot only: the walking model is not wired yet.");
            Canvas.ForceUpdateCanvases();
            Shot(cam, "panel_01_viable.png", panelEye, SceneBuilder.PanelCenter);
            panel.layerButtons[2].OnPointerExit();

            // 2. NOT VIABLE with the point of no return.
            panel.ShowVerdict(VerdictState.NotViable, null, "O2 used 81 % · the walk back from the far stop needs 96 %",
                "TEST VALUES · point of no return: 640 m along the route, before TEST STOP 2");
            Canvas.ForceUpdateCanvases();
            Shot(cam, "panel_02_not_viable.png", panelEye, SceneBuilder.PanelCenter);

            // 3. The side board with the first stop chosen.
            panel.ShowNoRoute();
            app.terrain.ShowLayer(MapLayer.Height);
            app.RefreshPanel();
            if (panel.stopButtons.Count > 0) app.SelectStop(0);
            if (panel.sideBoard != null)
            {
                panel.jumpButton.OnPointerEnter();
                Canvas.ForceUpdateCanvases();
                Vector3 side = panel.sideBoard.position;
                Shot(cam, "panel_03_stop_card.png", side - panel.sideBoard.forward * 0.85f, side);
                panel.jumpButton.OnPointerExit();
            }

            // 4. Both boards as the user sees them at start (no route yet, one stop chosen).
            Canvas.ForceUpdateCanvases();
            Shot(cam, "panel_04_boards.png", new Vector3(0.35f, 1.55f, -0.25f), new Vector3(0.55f, 1.45f, 1.9f));

            app.SelectStop(-1);
            panel.ShowNoRoute();
            cam.transform.SetPositionAndRotation(homePosition, homeRotation);
        }

        static bool HasDigit(string s)
        {
            foreach (char c in s) if (char.IsDigit(c)) return true;
            return false;
        }
    }
}
