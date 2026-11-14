// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The route piece of the app: pillars at the habitat and the stops, the straight line between
    /// them, and the safe route once session C's RoutePlanner exists.
    /// </summary>
    public partial class WalkbackApp
    {
        public RouteView routeView;
        public StopPillars stopPillars;

        partial void StartRoute()
        {
            ShowRouteStartState(true);
            // TODO(H2, Fable): when session C's RoutePlanner lands in Assets/Walkback/Model/, plan
            // habitat -> stops -> habitat here and call ShowPlannedRoute(points). Until then no amber
            // route, no minutes and no oxygen are drawn (rule 1): only the honest straight line.
        }

        /// <summary>
        /// What the map shows before any route is planned: the pillars and the straight dotted line
        /// habitat -> stops -> habitat. No amber route. The test calls it with grow = false.
        /// </summary>
        public void ShowRouteStartState(bool grow)
        {
            if (terrain == null || terrain.Site == null) return;
            if (stopPillars != null) stopPillars.Build(this, grow);
            if (routeView != null)
            {
                routeView.Bind(terrain.Site);
                routeView.ClearRoute();
                routeView.ShowStraightLine(StopWaypoints());
            }
        }

        /// <summary>Habitat, every stop in order, habitat again, in local meters (x east, y north).</summary>
        public Vector2[] StopWaypoints()
        {
            SiteData site = terrain.Site;
            var points = new Vector2[site.Stops.Length + 2];
            points[0] = site.Habitat;
            for (int i = 0; i < site.Stops.Length; i++) points[i + 1] = new Vector2(site.Stops[i].x, site.Stops[i].y);
            points[points.Length - 1] = site.Habitat;
            return points;
        }

        /// <summary>
        /// The safe route from session C's RoutePlanner, in local meters (x east, y north from the
        /// south-west corner): the amber ribbon draws itself on the map.
        /// </summary>
        public void ShowPlannedRoute(Vector2[] pointsMeters, bool animate = true)
        {
            if (routeView == null || terrain == null || terrain.Site == null) return;
            routeView.Bind(terrain.Site);
            routeView.ShowRoute(pointsMeters, animate);
        }

        public void ClearPlannedRoute()
        {
            if (routeView != null) routeView.ClearRoute();
        }
    }
}
