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
    partial class Program : MyGridProgram
    {
        IMyProgrammableBlock comPB;
        IMyRemoteControl control;
        List<IMyShipGrinder> grinder = new List<IMyShipGrinder>();

        enum Side {Front, Back, Left, Right, Up, Down };
        Dictionary<Side, List<IMyThrust>> thruster = new Dictionary<Side, List<IMyThrust>>();


        MyDetectedEntityInfo target;



        public Program()
        {
            control = GridTerminalSystem.GetBlockWithName("Control") as IMyRemoteControl;
            List<IMyThrust> thrust = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrust);
            foreach (IMyThrust thr in thrust)
            {

            }

        }

        public void Main(string argument)
        {

        }
    }
}