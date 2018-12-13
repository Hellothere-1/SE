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
        enum Mode {Idle, Flying,  Grinding}

        IMyProgrammableBlock comPB;
        IMyRemoteControl control;
        List<IMyShipGrinder> grinder = new List<IMyShipGrinder>();
        List<Gyroscope> gyros = new List<Gyroscope>();
        IMyCameraBlock visor;

        List<IMySensorBlock> mainGrinderSensors = new List<IMySensorBlock>();
        List<IMySensorBlock> mainCollisionSensors = new List<IMySensorBlock>();

        float baseMass;

        Mode state = Mode.Idle;

        enum Side {Front, Back, Left, Right, Up, Down };
        List<IMyThrust> allThruster = new List<IMyThrust>();
        Dictionary<Side, List<IMyThrust>> thruster = new Dictionary<Side, List<IMyThrust>>();
        Dictionary<Side, float> maxThrust = new Dictionary<Side, float>();


        MyDetectedEntityInfo target = new MyDetectedEntityInfo();



        public Program()
        {
            control = GridTerminalSystem.GetBlockWithName("Control") as IMyRemoteControl;
            visor = GridTerminalSystem.GetBlockWithName("Visor") as IMyCameraBlock;
            visor.EnableRaycast = true;
            for(int i = 0; i < 6; i ++)
            {
                thruster.Add((Side)i, new List<IMyThrust>());
                maxThrust.Add((Side)i, 0);
            }
            List<IMyThrust> thrust = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrust);
            allThruster = thrust;
            foreach (IMyThrust thr in thrust)
            {
                SortThruster(thr);
                thr.SetValueFloat("Override", 0);
            }
            List<IMyGyro> GyrosList = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(GyrosList);
            foreach (IMyGyro gyro in GyrosList)
            {
                gyro.GyroOverride = true;
                gyros.Add(new Gyroscope(gyro, control));
            }
            SetGyros(0, 0, 0);
            GridTerminalSystem.GetBlocksOfType(grinder);
            IMyBlockGroup sensors = GridTerminalSystem.GetBlockGroupWithName("Sensor Grinder");
            sensors.GetBlocksOfType(mainGrinderSensors);
            sensors = GridTerminalSystem.GetBlockGroupWithName("Sensor Collision");
            sensors.GetBlocksOfType(mainCollisionSensors);
            List<IMyCargoContainer> container = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(container);
            baseMass = control.CalculateShipMass().TotalMass;
            Echo("Init completed");
        }

        public void Main(string argument)
        {
            switch (state)
            {
                case Mode.Idle:
                    if (argument == "Wreck")
                    {
                        state = Mode.Grinding;
                    }
                    if (argument == "Start")
                    {
                        if (visor.AvailableScanRange >= 1000)
                        {
                            Echo("Search for target");
                            target = visor.Raycast(1000);
                        }
                    }
                    if (target.IsEmpty())
                    {
                        Echo("Target not found");
                        return;
                    }
                    Echo("Target found!");
                    Vector3D targetPos;
                    if (target.HitPosition != null)
                    {
                        targetPos = (Vector3D)target.HitPosition;
                        Vector3D targetVector = targetPos - control.GetPosition();
                        targetVector.Normalize();
                        targetPos = targetPos - 20 * targetVector;
                        control.ClearWaypoints();
                        control.AddWaypoint(targetPos, "Target");
                        control.GetActionWithName("CollisionAvoidance_On").Apply(control);
                        control.GetActionWithName("DockingMode_On").Apply(control);
                        //!!!Erst einstellungen, dann aktivieren!!!
                        control.GetActionWithName("AutoPilot_On").Apply(control);
                        state = Mode.Flying;
                    }
                    break;
                case Mode.Flying:
                    break;
                case Mode.Grinding:
                    List<string> activSensors = CheckSensors(mainGrinderSensors);
                    Vector3D instructions = CheckDirection(activSensors);
                    instructions = CheckObstacles(instructions);
                    float massFactor = control.CalculateShipMass().TotalMass / baseMass;
                    double thrustFactor = Math.Pow(2, massFactor);
                    foreach (IMyThrust thrust in allThruster)
                    {
                        thrust.SetValueFloat("Override", 0);
                    }
                    SetGyros(0, 0, 0);
                    Echo("Vector is: " + (int)instructions.X + " " + (int)instructions.Y + " " + (int)instructions.Z);
                    if (activSensors.Count == 0 && CheckSensors(mainCollisionSensors).Count == 0)
                    {
                        Echo("Going forward");
                        foreach (IMyThrust thrust in thruster[Side.Front])
                        {
                            thrust.SetValueFloat("Override", (float)(10 * thrustFactor));
                        }
                        
                    }
                    else if (IsZero(instructions))
                    {
                        Echo("Is zero");
                        foreach (IMyThrust thrust in thruster[Side.Back])
                        {
                            thrust.SetValueFloat("Override", (float)(5 * thrustFactor));
                        }
                    }
                    else
                    {
                        Echo("Working");
                        int basethrust = 35;
                        if (instructions.Z == 0)
                        {
                            instructions.X = Math.Sign(instructions.X) * thrustFactor;
                            instructions.Y = Math.Sign(instructions.Y) * thrustFactor;
                            SetGyros((float)instructions.Y, (float)instructions.X, 0);
                            instructions.X = -instructions.X;
                            instructions.Y = -instructions.Y;
                            basethrust = 5;
                        }
                        List<IMyThrust> neededThruster = instructions.X > 0 ? thruster[Side.Right] : thruster[Side.Left];
                        if (instructions.X != 0)
                        {
                            foreach (IMyThrust thrust in neededThruster)
                            {
                                thrust.SetValueFloat("Override", (float)(thrustFactor * basethrust));
                            }
                            foreach (IMyThrust thrust in thruster[Side.Front])
                            {
                                thrust.SetValueFloat("Override", (float)(5 * thrustFactor));
                            }
                        }
                        if (instructions.Y != 0)
                        {
                            neededThruster = instructions.Y > 0 ? thruster[Side.Up] : thruster[Side.Down];
                            foreach (IMyThrust thrust in neededThruster)
                            {
                                thrust.SetValueFloat("Override", (float)(thrustFactor * basethrust));
                            }
                            foreach (IMyThrust thrust in thruster[Side.Front])
                            {
                                thrust.SetValueFloat("Override", (float)(5 * thrustFactor));
                            }
                        }
                        
                    }
                    break;
            }

        }

        bool IsZero(Vector3D input)
        {
            return input.Y == 0 && input.X == 0;
        }

        List<string> CheckSensors(List<IMySensorBlock> sensors)
        {
            List<string> activSensors = new List<string>();
            foreach (IMySensorBlock sensor in sensors)
            {
                if (sensor.IsActive)
                {
                    activSensors.Add(sensor.CustomName);
                }
            }
            return activSensors;
        }

        Vector3D CheckObstacles(Vector3D input)
        {
            List<string> activCollisions = CheckSensors(mainCollisionSensors);
            if (activCollisions.Count == 0)
            {
                return input;
            }
            if (activCollisions.Count == 4)
            {
                return new Vector3D( 0, 0, 0 );
            }
            Vector3D output = new Vector3D(0,0,0);
            foreach (string name in activCollisions)
            {
                string part = name.Split(' ')[1];
                if (part == "Right")
                {
                    output.X++;
                }
                if (part == "Left")
                {
                    output.X--;
                }
                if (part == "Up")
                {
                    output.Y++;
                }
                if (part == "Down")
                {
                    output.Y--;
                }
            }
            return output;
        }

        Vector3D CheckDirection(List<string> sensorNames)
        {
            Vector3D output = new Vector3D(0,0,0);
            if (sensorNames.Count == 4)
            {
                return output;
            }
            foreach (string name in sensorNames)
            {
                string[] parts = name.Split(' ');
                if (parts[1] == "Right")
                {
                    output.X++;
                }
                else
                {
                    output.X--;
                }
                if (parts[2] == "Up")
                {
                    output.Y++;
                }
                else
                {
                    output.Y--;
                }
            }
            if (sensorNames.Count == 2)
            {
                if (output.X == 2 || output.X == -2)
                {
                    output.Y = 0;
                }
                else
                {
                    output.X = 0;
                }
            }
            else
            {
                output.Z = 1;
            }
            //Z == 1 Bewegen Z == 0 Drehen
            return output;
        }

        void SortThruster(IMyThrust thrust)
        {
            Vector3D front = thrust.WorldMatrix.Forward;
            if (front == control.WorldMatrix.Backward)
            {
                thruster[Side.Front].Add(thrust);
                maxThrust[Side.Front] += thrust.MaxThrust;
            }
            else if (front == control.WorldMatrix.Forward)
            {
                thruster[Side.Back].Add(thrust);
                maxThrust[Side.Back] += thrust.MaxThrust;
            }
            else if (front == control.WorldMatrix.Left)
            {
                thruster[Side.Right].Add(thrust);
                maxThrust[Side.Right] += thrust.MaxThrust;
            }
            else if (front == control.WorldMatrix.Right)
            {
                thruster[Side.Left].Add(thrust);
                maxThrust[Side.Left] += thrust.MaxThrust;
            }
            else if (front == control.WorldMatrix.Up)
            {
                thruster[Side.Down].Add(thrust);
                maxThrust[Side.Down] += thrust.MaxThrust;
            }
            else if (front == control.WorldMatrix.Down)
            {
                thruster[Side.Up].Add(thrust);
                maxThrust[Side.Up] += thrust.MaxThrust;
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