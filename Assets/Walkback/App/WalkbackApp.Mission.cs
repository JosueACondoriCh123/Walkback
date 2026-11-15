// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;
using UnityEngine.InputSystem;

namespace Walkback
{
    /// <summary>
    /// Mission mode wired into the app: one button walks Table -> Mars -> Room -> Table.
    ///   Left controller: menu button = next mode (in the mission also a click of the left stick) ·
    ///                    left grip = jump to the next point of the route ·
    ///                    B or Y = recenter (the route point back under you).
    ///   Desktop:         M = next mode · J = jump · N = recenter · arrow keys walk and turn (test only).
    /// </summary>
    public partial class WalkbackApp
    {
        public MissionMode mission;
        /// <summary>No free space to walk: skip Mars, the button goes Table -> Room -> Table (the same mode, standing).</summary>
        public bool missionWithoutMars;

        /// <summary>True while the mission mode shows (on Mars or in the room).</summary>
        public bool MissionActive { get { return mission != null && mission.Current != MissionMode.Mode.Table; } }

        /// <summary>START MISSION on the board: from the table into the mission (Mars, or the room without free space).</summary>
        public bool StartMissionFromBoard()
        {
            if (mission == null || MissionActive) return false;
            WalkbackLog.Info("Mission button (START MISSION on the board)");
            return mission.NextMode(HeadTransform());
        }

        bool menuHeldBefore;
        float menuPressedAt = -10f;
        /// <summary>Two readings may see the same press a frame apart: within this time it counts once.</summary>
        const float MenuRepeatSeconds = 0.5f;

        /// <summary>
        /// The left controller's menu button this frame, or null: OVRInput's virtual button, OVRInput's raw
        /// button, and Unity's own XR input (the Oculus Touch profile is on in the OpenXR settings).
        /// Returns the name of the reading that saw the press, for the log.
        /// </summary>
        string ReadMenuButton()
        {
            bool held = false;
            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out held);
            bool pressed = RisingEdge(held, ref menuHeldBefore);
            string reading = null;
            if (OVRInput.GetDown(OVRInput.Button.Start)) reading = "left menu, OVRInput.Button.Start";
            else if (OVRInput.GetDown(OVRInput.RawButton.Start)) reading = "left menu, OVRInput.RawButton.Start";
            else if (pressed) reading = "left menu, XR input menuButton";
            if (reading == null || Time.unscaledTime - menuPressedAt < MenuRepeatSeconds) return null;
            menuPressedAt = Time.unscaledTime;
            return reading;
        }

        /// <summary>True on the frame a held state turns on (a button read as held, not as pressed).</summary>
        public static bool RisingEdge(bool held, ref bool before)
        {
            bool rose = held && !before;
            before = held;
            return rose;
        }

        partial void StartMission()
        {
            if (mission != null) mission.Begin(this);
        }

        partial void UpdateMission()
        {
            if (mission == null) return;
            var keyboard = Keyboard.current;
            Transform head = HeadTransform();

            // OVRInput.Button.Start is the left controller's menu button (checked in Meta XR SDK 209: "Touch, LTouch: Start").
            // At 18:30 no press of it reached the app, so it is read three ways (ReadMenuButton), and inside the
            // mission a click of the left stick does the same: that button is free there (the WHAT IF is a table
            // control) and the headset did see it at the table.
            string source = keyboard != null && keyboard.mKey.wasPressedThisFrame ? "M key" : (DesktopMode ? null : ReadMenuButton());
            if (source == null && !DesktopMode && MissionActive && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
                source = "left stick click";
            if (source != null)
            {
                // Logged with its source, so the app's log says which reading the headset answers to.
                WalkbackLog.Info("Mission button (" + source + ") in mode " + mission.Current);
                mission.NextMode(head);
            }
            if (!MissionActive) return;

            bool jump = (keyboard != null && keyboard.jKey.wasPressedThisFrame) ||
                        (!DesktopMode && OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch));
            if (jump) mission.Jump(head);

            bool recenter = (keyboard != null && keyboard.nKey.wasPressedThisFrame) ||
                            (!DesktopMode && (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)));
            if (recenter) mission.Recenter(head);

            mission.Tick(head, Time.deltaTime);
        }
    }
}
