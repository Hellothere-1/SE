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

        //int lengthSave;
        //Maybe save some 

        IMyProgrammableBlock programmableBlock;
        IMyCameraBlock visor;
        IMyRemoteControl control;
        IMyShipMergeBlock merge;

        IMyBlockGroup starterBlocks;

        TargetFuncs funcs;
        MyDetectedEntityInfo target;

        Vector3 directions;

        List<IMyWarhead> warheads = new List<IMyWarhead>();
        List<Gyroscope> gyros = new List<Gyroscope>();
        Dictionary<IMyThrust, int[]> thrusterDict = new Dictionary<IMyThrust, int[]>();

        bool launched = false;
        bool init = false;

        public Program()
        {
            programmableBlock = Me;
            

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
            List<IMyTerminalBlock> tempBlocks = new List<IMyTerminalBlock>();
            try
            {
                starterBlocks.GetBlocksOfType<IMyCameraBlock>(tempBlocks);
                visor = tempBlocks[0] as IMyCameraBlock;
                visor.EnableRaycast = true;
                Echo("Camera found and activ");
            }
            catch (Exception)
            {
                Echo("Camera could not be found in starter group");
            }
            try
            {
                starterBlocks.GetBlocksOfType<IMyShipMergeBlock>(tempBlocks);
                merge = tempBlocks[0] as IMyShipMergeBlock;
                Echo("Merge Block found and activ");
            }
            catch (Exception)
            {
                Echo("Merge Block could not be found in starter group");
            }
            try
            {
                starterBlocks.GetBlocksOfType<IMyRemoteControl>(tempBlocks);
                control = tempBlocks[0] as IMyRemoteControl;
                Echo("Remote Control found and activ");
            }
            catch (Exception)
            {
                Echo("Remote Control could not be found in starter group");
            }
            funcs = new TargetFuncs(this, visor);
            Echo("Setup completed, Missile ready to fire");

        }

        public void Main(string argument)
        {

            if (argument == "Fire")
            {
                if (visor.AvailableScanRange < 5000)
                {
                    Echo("Scanning is recharging");
                    return;
                }
                target = visor.Raycast(5000);
                if (target.IsEmpty())
                {
                    //Search in cone form
                    Echo("No target found");
                    return;
                }
                funcs.LockTarget(target, control);
                merge.Enabled = false;
                launched = true;
                Echo("Launched");
                return;
            }
            if (launched)
            {
                if (!init)
                {
                    initMissile();
                    init = true;
                    Echo("Init completed");
                }
                else
                {
                    directions = funcs.run(control);
                    SetGyros(directions.Z, directions.Y, 0);
                    if (directions.Y == 0 && directions.Z == 0)
                    {
                        Echo("Target lost");
                    }
                    else
                    {
                        Echo("Tracking target");
                    }
                }
            }
        }

        void initMissile()
        {
            GridTerminalSystem.GetBlocksOfType(warheads);
            List<IMyThrust> thruster = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thruster);
            thrusterDict = thruster.ToDictionary(x => x, y => new int[] { 0 });
            foreach (IMyWarhead boom in warheads)
            {
                if (!boom.GetValueBool("Safety"))
                {
                    boom.SetValueBool("Safety", true);
                }
            }
            List<IMyGyro> GyrosList = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(GyrosList);
            foreach (IMyGyro gyro in GyrosList)
            {
                gyro.GyroOverride = true;
                gyros.Add(new Gyroscope(gyro, control));
            }

            foreach (IMyThrust thrust in thrusterDict.Keys.ToList())
            {
                thrust.SetValueFloat("Override", 100);
            }
            

        }

        void SetGyros(float pitch, float yaw, float roll)
        {
            float[] controls = new float[] { -pitch, yaw, -roll, pitch, -yaw, roll };
            foreach (Gyroscope g in gyros)
                g.SetRotation(controls);
        }
    }
}