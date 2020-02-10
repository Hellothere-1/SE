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
        public class Corner : Waypoint
        {
            public IMyTerminalBlock block { get; private set; }
            public List<Corridor> corridors { get; private set; } = new List<Corridor>();

            public Corner (IMyTerminalBlock block)
            {
                this.block = block;
            }

            public void AddCorridor(Corridor corridor)
            {
                corridors.Add(corridor);
            }
        }
    }
}
