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

        public abstract class Waypoint
        {
            public Waypoint nextWaypoint = null;
            public bool visited = false;
            static protected Station targetStation;
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

                targetStation = from;

                to.corridor.nextWaypoint = to;

                ToTest.Enqueue(to.corridor);

                while(ToTest.Count > 0)
                {
                    if(ToTest.Dequeue().FindPathRecursive())
                    {
                        break;
                    }
                }

                bool pathFound = from.visited;

                ClearAll();

                return pathFound;
            }

            public static CorridorSystem CreateCorridorSystem (Corridor origin, List<Corridor> corridors, Program program)
            {
                ToTest.Clear();
                allVisited.Clear();

                ToTest.Enqueue(origin);

                CorridorSystem c = new CorridorSystem(program);

                while (ToTest.Count > 0)
                {
                    Waypoint w = ToTest.Dequeue();
                    w.FindPathRecursive();

                    if(w is Corridor)
                    {
                        c.AddCorridor((Corridor)w);
                        corridors.Remove((Corridor)w);
                    }
                }

                ClearAll();

                return c;
            }

            public virtual Waypoint tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc)
            {
                return null;
            }

            public virtual Vector3I position => Vector3I.Zero;

            public static void ClearAll()
            {
                foreach (Waypoint waypoint in allVisited)
                {
                    waypoint.Clear();
                }
            }

            public virtual bool FindPathRecursive()
            {
                visited = true;
                allVisited.Add(this);
                return false;
            }

            protected void Clear()
            {
                visited = false;
            }

        }
    }
}
