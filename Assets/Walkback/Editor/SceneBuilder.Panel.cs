// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEditor;
using UnityEngine;

namespace Walkback.EditorTools
{
    /// <summary>Panel piece: the side board (stop card and current conditions) next to the main board, and the controls card on the other side.</summary>
    public static partial class SceneBuilder
    {
        static partial void BuildPanelPiece(WalkbackApp app)
        {
            if (app.panel == null) return;
            app.panel.BuildSideBoard();
            BuildControlsCard(app.panel);
        }

        /// <summary>
        /// A fixed card with what each control does, to the left of the main board and facing the user.
        /// It is a child of the panel: it hides with it (globe opening, 360° photo, mission mode) and
        /// moves with it on RECENTER. Letters of 32 mm at about 2.1 m read like 18 mm at 1.2 m.
        /// </summary>
        static void BuildControlsCard(MissionPanel panel)
        {
            Material backPlate = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelBack.mat");
            Material ui = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/PanelUI.mat");
            string[] text = MissionPanel.ControlsText;
            var sizes = new int[text.Length];
            var colors = new Color[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                sizes[i] = i == 0 ? 40 : 32;
                colors[i] = i == 0 ? WalkbackTheme.TextSoft : WalkbackTheme.Text;
            }
            GlobeLabel card = Card(panel.transform, "ControlsCard", 760f, 350f, sizes, colors, backPlate, ui, MissionPanel.ControlsCardOffset);
            Vector3 toCard = panel.transform.position + MissionPanel.ControlsCardOffset - MissionPanel.UserEye;
            toCard.y = 0f;
            card.transform.localRotation = Quaternion.LookRotation(toCard.normalized, Vector3.up);
            card.SetLines(text);
            panel.controlsCard = card;
        }
    }
}
