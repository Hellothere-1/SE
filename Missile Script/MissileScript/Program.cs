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
        private int group = 0;
        private int volley = 0;
        private int id = 0;
        private String comPartner = "";
        private String key = "";

        //Launch orders
        enum Side {UP, DOWN, LEFT, RIGHT, FORWARD, BACKWARD };



        //int lengthSave;
        //Maybe save some amount of raycast distance for emergency use

        IMyProgrammableBlock programmableBlock;
        IMyCameraBlock visor;
        IMyRemoteControl control;
        IMyShipMergeBlock merge;
        IMyRadioAntenna antenna;
        IMyBlockGroup starterBlocks;

        TargetFuncs funcs;
        Launch launchHandler;

        Vector3 directions;

        List<IMyWarhead> warheads = new List<IMyWarhead>();
        List<Gyroscope> gyros = new List<Gyroscope>();

        bool setupSuccess = false;

        bool launchSequence = false;
        bool launched = false;
        bool init = false;



        public Program()
        {
            programmableBlock = Me;
            //Search for starter Group with this pb in it
            /*
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

            //Starter group is found, trying to acquire the needed compoents
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
                return;
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
                return;
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
                return;
            }
            try
            {
                starterBlocks.GetBlocksOfType<IMyRadioAntenna>(tempBlocks);
                antenna = tempBlocks[0] as IMyRadioAntenna;
                Echo("Radio Antenna found and activ");
            }
            catch (Exception)
            {
                Echo("Antenna could not be found in starter group");
                return;
            }*/
            setupSuccess = true;
            //funcs = new TargetFuncs(this, visor);
            Echo("Setup completed, Missile ready to fire");
            //Components found, setup complete, missile ready to fire
        }

        public void Main(string argument)
        {
            if (!setupSuccess)
            {
                return;
            }
            if (argument == "Parse")
            {
                string[] orders = Me.CustomData.Split('\n');
                short errors = 0;
                foreach (string order in orders)
                {
                    if (!launchHandler.ParseLaunchOrder(order))
                    {
                        Echo("Parsing of line " + order + " failed!");
                        errors++;
                    }
                }
                Echo("Finished parsing with " + errors + " incorrect lines");
                Echo("Remenber that type errors will occur only at runtime");
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
                    if (funcs.lastDistance < 3)
                    {
                        foreach (IMyWarhead boom in warheads)
                        {
                            if (boom.GetValueBool("Safety"))
                            {
                                boom.SetValueBool("Safety", false);
                            }
                        }
                        foreach (IMyWarhead boom in warheads)
                        {
                            boom.Detonate();
                        }
                    }
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
                //TODO currently only one argument is needed, if more are needed delete the following return
                return;
            }
            if (argument == "Fire")
            {
                if (visor.AvailableScanRange < 5000)
                {
                    Echo("Scanning is recharging");
                    return;
                }
                MyDetectedEntityInfo target;
                target = visor.Raycast(5000);
                if (target.IsEmpty())
                {
                    //Search in cone form
                    //TODO launch sequence without direct line of sight possible, redirecting missile per antenna
                    Echo("No target found");
                    return;
                }
                funcs.LockTarget(target, control);
                merge.Enabled = false;
                launched = true;
                Echo("Launched");
                return;
            }
        }

        void initMissile()
        {
            //TODO arm warheads at launch? or better to arm them after time/correct launch?
            GridTerminalSystem.GetBlocksOfType(warheads);
            /*
            foreach (IMyWarhead boom in warheads)
            {
                if (!boom.GetValueBool("Safety"))
                {
                    boom.SetValueBool("Safety", true);
                }
            }
            */
            List<IMyGyro> GyrosList = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(GyrosList);
            merge.Enabled = false;
            foreach (IMyGyro gyro in GyrosList)
            {
                gyro.GyroOverride = true;
                gyros.Add(new Gyroscope(gyro, control));

            }

            List<IMyThrust> thruster = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thruster);
            foreach (IMyThrust t in thruster)
            {
                t.SetValueFloat("Override", t.MaxThrust);
            }
            //thrust.SetValueFloat("Override", 100);
            //Method used to set override, should be in target function class
        }





        void SetGyros(float pitch, float yaw, float roll)
        {
            float[] controls = new float[] { -pitch, yaw, -roll, pitch, -yaw, roll };
            foreach (Gyroscope g in gyros)
                g.SetRotation(controls);
        }
    }
}
