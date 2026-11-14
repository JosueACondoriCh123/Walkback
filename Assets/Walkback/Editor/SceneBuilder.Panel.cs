// Written with Claude Code (AI), directed and tested by team MARSXR
namespace Walkback.EditorTools
{
    /// <summary>Panel piece: the side board (stop card and current conditions) next to the main board.</summary>
    public static partial class SceneBuilder
    {
        static partial void BuildPanelPiece(WalkbackApp app)
        {
            if (app.panel == null) return;
            app.panel.BuildSideBoard();
        }
    }
}
