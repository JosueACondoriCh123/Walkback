// Written with Claude Code (AI), directed and tested by team MARSXR
namespace Walkback
{
    /// <summary>Panel piece: wires the boards' buttons and the stop and layer events at start.</summary>
    public partial class WalkbackApp
    {
        partial void StartPanelPiece()
        {
            if (panel == null) return;
            panel.Bind(this);
            // Until session C's model is wired, the verdict card says there is no route: no numbers are made up.
            panel.ShowNoRoute();
            // PLAN B: if the route piece already planned the mission (MissionFallback), show its verdict,
            // labelled as an estimate on the card.
            ShowPlanOnPanel();
            WalkbackLog.Info("Panel bound: " + panel.layerButtons.Length + " layer buttons, " + panel.stopButtons.Count + " stop buttons, conditions " + (panel.ConditionsHaveData ? "loaded" : "waiting for data"));
        }
    }
}
