using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {

        public class Waypoint
        {
            public Waypoint nextWaypoint = null;
            public bool visited = false;
            static protected Station target;
            static protected Queue<Waypoint> ToTest = new Queue<Waypoint>();
            static protected List<Waypoint> allVisited = new List<Waypoint>();

            public static bool FindPath(Station from, Station to)
            {
                if(to == from)
                {
                    return false;
                }
                ToTest.Clear();
                allVisited.Clear();
                to.nextWaypoint = null;

                target = from;

                to.corridor.nextWaypoint = to;
                to.corridor.FindPathRecursive();

                bool pathFound = from.visited;

                ClearAll();

                return pathFound;
            }

            public virtual Waypoint tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc)
            {
                return null;
            }

            public static void ClearAll()
            {
                foreach (Waypoint waypoint in allVisited)
                {
                    waypoint.Clear();
                }
            }

            public virtual void FindPathRecursive()
            {
                visited = true;
                allVisited.Add(this);
            }

            protected void Clear()
            {
                visited = false;
            }

        }
    }
}
