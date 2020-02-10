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

            public void FindPath(Station from, Station to)
            {
                to.corridor.FindPathRecursive(from, to);
            }

            protected virtual void FindPathRecursive(Station target, Waypoint Origin)
            {
                nextWaypoint = Origin;
            }

        }
    }
}
