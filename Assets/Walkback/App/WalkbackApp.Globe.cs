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
            if (globe != null && !skipGlobeOpening) globe.Begin();
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
