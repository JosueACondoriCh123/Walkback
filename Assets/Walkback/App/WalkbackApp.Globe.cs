// Written with Claude Code (AI), directed and tested by team MARSXR
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Walkback
{
    /// <summary>The globe opening and the jump to the site, wired into the app.</summary>
    public partial class WalkbackApp
    {
        public GlobeOpening globe;
        public SiteJump siteJump;
        /// <summary>Skip the globe and start with the map on the table (for quick checks in the headset).</summary>
        public bool skipGlobeOpening;

        /// <summary>Raised when the map has landed on the table after the globe opening (e.g. to make the pillars grow).</summary>
        public event Action MapLanded;

        public void RaiseMapLanded()
        {
            if (MapLanded != null) MapLanded();
        }

        partial void StartGlobe()
        {
            if (siteJump != null)
            {
                siteJump.EnsureBuilt();
                JumpRequested += siteJump.Jump;
            }
            // In the headset the head pose often is not there yet in the first frame (it reads 0, 0, 0): the table
            // then stayed at the scene's origin layout, behind the user or to one side, and with the controls locked
            // by the opening B and Y could not bring it. Wait for the pose (a few frames; HeadWaitSeconds at most).
            if (!DesktopMode && !HeadPoseTracked()) StartCoroutine(OpenWhenHeadIsTracked());
            else OpenStage();
        }

        /// <summary>How long the start waits for the head pose before it opens where the scene was laid out.</summary>
        public const float HeadWaitSeconds = 3f;

        /// <summary>An untracked head reads 0, 0, 0; a tracked one is at eye height over the floor.</summary>
        public static bool IsTrackedPose(Vector3 headPosition)
        {
            return headPosition.sqrMagnitude >= 0.01f;
        }

        bool HeadPoseTracked()
        {
            Transform head = HeadTransform();
            return head != null && IsTrackedPose(head.position);
        }

        System.Collections.IEnumerator OpenWhenHeadIsTracked()
        {
            float until = Time.unscaledTime + HeadWaitSeconds;
            int frames = 0;
            while (!HeadPoseTracked() && Time.unscaledTime < until)
            {
                frames++;
                yield return null;
            }
            WalkbackLog.Info("Start: waited " + frames + " frames for the head pose" + (HeadPoseTracked() ? "" : ", it did not come"));
            OpenStage();
        }

        /// <summary>The table in front of the user, then the opening globe over it (the opening locks the controls).</summary>
        void OpenStage()
        {
            PlaceStageInFrontOfUser();
            if (globe != null && !skipGlobeOpening) globe.Begin();
        }

        /// <summary>
        /// The scene is laid out around the tracking origin (table 1 m along +z), but the user may put the
        /// headset on anywhere in the room, looking anywhere. Before the opening locks the controls, bring
        /// the table setup in front of the head once, and the globe with it (it floats over the table).
        /// Headset only; skipped while the head pose is not tracked yet (it then reads 0, 0, 0).
        /// </summary>
        void PlaceStageInFrontOfUser()
        {
            if (DesktopMode || tableControls == null || table == null || mapRoot == null) return;
            Transform head = HeadTransform();
            if (head == null || !IsTrackedPose(head.position))
            {
                WalkbackLog.Warn("Start: head pose not tracked yet, the table stays at the scene's origin layout");
                return;
            }
            // TableControls.Begin runs again in StartPointer; calling it here first is harmless (it only stores references).
            tableControls.Begin(this);
            Vector3 globeOffset = globe != null ? globe.center - table.position : Vector3.zero;
            float yawBefore = tableControls.StageYaw;
            if (!tableControls.Recenter(head.position, head.forward)) return;
            if (globe != null)
            {
                Quaternion turn = Quaternion.Euler(0f, Mathf.DeltaAngle(yawBefore, tableControls.StageYaw), 0f);
                globe.center = table.position + turn * globeOffset;
            }
            WalkbackLog.Info("Start: table placed in front of the user at " + table.position.ToString("F2"));
        }

        partial void UpdateGlobe()
        {
            // Without the pointer: G on the keyboard, or A on the right controller, presses the globe.
            if (globe == null || globe.Current != GlobeOpening.State.Opening) return;
            var keyboard = Keyboard.current;
            bool key = keyboard != null && keyboard.gKey.wasPressedThisFrame;
            if (key || OVRInput.GetDown(OVRInput.Button.One)) globe.Press();
        }
    }
}
