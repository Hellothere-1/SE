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
        enum Direction {Front, Back, Left, Right, Up, Down}

        IMyTextPanel debug;

        IMyProgrammableBlock programmableBlock;
        IMyCameraBlock visor;
        IMyRemoteControl control;
        IMyShipMergeBlock merge;

        IMyBlockGroup starterBlocks;

        MyDetectedEntityInfo target;

        List<IMyWarhead> warheads;
        Dictionary<IMyGyro, int> gyroDict = new Dictionary<IMyGyro, int>();
        Dictionary<IMyThrust, int[]> thrusterDict = new Dictionary<IMyThrust, int[]>();

        bool launched = false;
        bool init = false;

        public Program()
        {
            programmableBlock = Me;

            debug = GridTerminalSystem.GetBlockWithName("Debug") as IMyTextPanel;

            //Search for starter Group with this pb in it
            List<IMyBlockGroup> allGroups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(allGroups);
            bool found = false;
            foreach (IMyBlockGroup group in allGroups)
            {
                List<IMyProgrammableBlock> pbsInList = new List<IMyProgrammableBlock>();
                group.GetBlocksOfType(pbsInList);
                foreach (IMyProgrammableBlock pb in pbsInList)
                {
                    if (pb.Equals(programmableBlock))
                    {
                        starterBlocks = group;
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    break;
                }
            }
            if (!found)
            {
                Echo("Starter Group not found, is this Programmable Block in it?");
                return;
            }
            //Hopefully found the group
            
            List<IMyTerminalBlock> tempBlocks = new List<IMyTerminalBlock>();
            starterBlocks.GetBlocksOfType<IMyCameraBlock>(tempBlocks);
            visor = tempBlocks[0] as IMyCameraBlock;
            visor.EnableRaycast = true;
            starterBlocks.GetBlocksOfType<IMyShipMergeBlock>(tempBlocks);
            merge = tempBlocks[0] as IMyShipMergeBlock;
            starterBlocks.GetBlocksOfType<IMyRemoteControl>(tempBlocks);
            control = tempBlocks[0] as IMyRemoteControl;
            
            
            Echo("Setup completed, Missile ready to fire");

        }

        public void Main(string argument)
        {
            Vector3D temp = GetShipAngularVelocity(control);
            debug.WritePublicText(((int)(temp.X)).ToString() + "\n");
            debug.WritePublicText(((int)(temp.Y)).ToString() + "\n", true);
            debug.WritePublicText(((int)(temp.Z)).ToString() + "\n", true);

            if (argument == "Fire")
            {
                if (visor.AvailableScanRange < 1000)
                {
                    Echo("Scanning is recharging");
                    return;
                }
                target = visor.Raycast(1000);
                if (target.IsEmpty())
                {
                    Echo("No target found");
                    return;
                }
                merge.Enabled = false;
                launched = true;
                Echo("Launched");
                return;
            }
            //for tests (delete ! after test)
            if (launched)
            {
                if (!init)
                {
                    initMissile();
                    init = true;
                }
            }

        }

        void initMissile()
        {
            GridTerminalSystem.GetBlocksOfType(warheads);

            List<IMyThrust> thruster = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thruster);
            thrusterDict = thruster.ToDictionary(x => x, y => new int[] { 0 });

        }

        public Vector3D GetShipAngularVelocity(IMyShipController dataBlock)
        {
            var worldLocalVelocities = dataBlock.GetShipVelocities().AngularVelocity;
            var worldToAnchorLocalMatrix = Matrix.Transpose(dataBlock.WorldMatrix.GetOrientation());
            return Vector3D.Transform(worldLocalVelocities, worldToAnchorLocalMatrix);
        }
    }
}