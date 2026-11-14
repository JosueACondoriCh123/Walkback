// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEditor;
using UnityEngine;

namespace Walkback.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>The route view and the pillars under MapRoot. The pillars themselves are made at start, from the site.</summary>
        static partial void BuildRoute(WalkbackApp app)
        {
            try
            {
                Material light = Shader.Find("Walkback/RouteLight") != null
                    ? MakeMaterial("RouteLight", "Walkback/RouteLight", Color.white, 0f)
                    : MakeMaterial("RouteLight", "Universal Render Pipeline/Unlit", Color.white, 0f);
                Material label = MakeMaterial("PanelUI", "Walkback/UI", Color.white, 0f);

                var routeObject = new GameObject("Route");
                routeObject.transform.SetParent(app.mapRoot, false);
                var routeView = routeObject.AddComponent<RouteView>();
                routeView.lightMaterial = light;

                var pillarsObject = new GameObject("StopPillars");
                pillarsObject.transform.SetParent(app.mapRoot, false);
                var pillars = pillarsObject.AddComponent<StopPillars>();
                pillars.lightMaterial = light;
                pillars.labelMaterial = label;

                app.routeView = routeView;
                app.stopPillars = pillars;
            }
            catch (System.Exception e)
            {
                // A failure here must not stop the rest of the scene from being built.
                Debug.LogError("[Walkback] BuildRoute failed: " + e);
            }
        }
    }
}
