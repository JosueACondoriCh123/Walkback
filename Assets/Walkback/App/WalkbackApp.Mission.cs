// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;
using UnityEngine.InputSystem;

namespace Walkback
{
    /// <summary>
    /// Mission mode wired into the app: one button walks Table -> Mars -> Room -> Table.
    ///   Left controller: menu button = next mode · left grip = jump to the next point of the route ·
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

        partial void StartMission()
        {
            if (mission != null) mission.Begin(this);
        }

        partial void UpdateMission()
        {
            if (mission == null) return;
            var keyboard = Keyboard.current;
            Transform head = HeadTransform();

            // VERIFY IN THE SDK: OVRInput.Button.Start (the left controller's menu button) is not used anywhere else in the project.
            bool next = (keyboard != null && keyboard.mKey.wasPressedThisFrame) || (!DesktopMode && OVRInput.GetDown(OVRInput.Button.Start));
            if (next) mission.NextMode(head);
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
