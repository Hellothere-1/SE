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
        public class TargetHandler
        {
            IMyCameraBlock visor;
            MyDetectedEntityInfo target;
            Program parent;

            public TargetHandler(Program par)
            {
                parent = par;
                visor = parent.GridTerminalSystem.GetBlockWithName("Visor") as IMyCameraBlock;
                visor.EnableRaycast = true;
            }

            public void getTarget()
            {
                target = visor.Raycast(1500);
                if (!target.IsEmpty())
                {
                    parent.Echo("Name of target : " + target.Position);
                }

            }

        }
    }
}
