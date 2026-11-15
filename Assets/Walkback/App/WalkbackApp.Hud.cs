// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The astronaut's view, wired into the app: while standing at a stop (inside its 360° photo after
    /// JUMP TO SITE) the helmet HUD follows the head and the 3D map shows on the left wrist.
    /// Back at the table both hide.
    /// </summary>
    public partial class WalkbackApp
    {
        public HelmetHud hud;
        public WristMap wristMap;

        int hudStop = -1;

        partial void StartHud()
        {
            JumpRequested += index => hudStop = index;
            if (hud != null) hud.Hide();
            if (wristMap != null) wristMap.SetVisible(false);
        }

        partial void UpdateHud()
        {
            if (hud == null || wristMap == null) return;
            // In mission mode the HUD is driven by the headset's position (MissionMode), not by a stop's photo.
            if (MissionActive) return;
            bool atStop = siteJump != null && siteJump.Inside && hudStop >= 0 && terrain != null && terrain.Site != null && hudStop < terrain.Site.Stops.Length;
            Transform head = HeadTransform();
            if (!atStop || head == null)
            {
                if (hud.Visible) hud.Hide();
                wristMap.ReturnToWrist();
                wristMap.SetVisible(false);
                return;
            }

            if (!hud.Visible || hud.AtStop != hudStop)
            {
                hud.ShowAt(hudStop);
                hud.SnapTo(head);
                var stop = terrain.Site.Stops[hudStop];
                wristMap.Prepare(this, new Vector2(stop.x, stop.y), stop.name);
            }
            else hud.Follow(head, Time.deltaTime);
            HoldWristMap(head);
        }

        /// <summary>
        /// The 3D map on the left wrist: it shows when the user looks at the hand, or stays with X.
        /// Without a headset it floats low on the left. Shared by the stop view and the mission mode.
        /// </summary>
        public void HoldWristMap(Transform head)
        {
            if (wristMap == null || head == null) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var rig = !DesktopMode && cameraRig != null ? cameraRig.GetComponent<OVRCameraRig>() : null;
            Transform hand = rig != null ? rig.leftHandAnchor : null;

            // X (left controller) or the X key: off the wrist and fixed in the air, or back to the wrist.
            bool toggle = DesktopMode ? keyboard != null && keyboard.xKey.wasPressedThisFrame : OVRInput.GetDown(OVRInput.Button.Three);
            if (toggle) ToggleMapPlaced(head, hand);

            if (wristMap.Placed)
            {
                wristMap.SetVisible(true);
                // In mission mode Mars is anchored in the room: the placed map lies with its north on Mars' north.
                bool north = MissionActive && mission.mars != null;
                wristMap.KeepPlaced(head, north, north ? mission.mars.rotation : Quaternion.identity);
                return;
            }
            if (DesktopMode)
            {
                wristMap.SetVisible(true);
                wristMap.HoldInFrontOf(head);
                return;
            }
            bool show = hand != null && WristMap.LookingAt(hand, head);
            wristMap.SetVisible(show);
            if (show) wristMap.HoldAt(hand, head);
        }

        /// <summary>
        /// Leaves the map in the air where the left hand is (without a hand: low in front of the viewer),
        /// or takes it back to the wrist if it was already placed.
        /// </summary>
        public void ToggleMapPlaced(Transform head, Transform hand)
        {
            if (wristMap == null || head == null) return;
            if (wristMap.Placed)
            {
                wristMap.ReturnToWrist();
                return;
            }
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Vector3 at = hand != null ? hand.position + Vector3.up * 0.07f : head.position + flat * 0.7f + Vector3.down * 0.45f;
            wristMap.PlaceAt(at, head);
        }

        /// <summary>The viewer's eyes: the headset's center eye, or the desktop camera.</summary>
        public Transform HeadTransform()
        {
            if (!DesktopMode && cameraRig != null && cameraRig.activeInHierarchy)
            {
                var rig = cameraRig.GetComponent<OVRCameraRig>();
                if (rig != null && rig.centerEyeAnchor != null) return rig.centerEyeAnchor;
            }
            return desktopCamera != null ? desktopCamera.transform : null;
        }
    }
}
