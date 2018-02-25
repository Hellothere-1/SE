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
        public class Turret
        {
            private IMyTerminalBlock core;
            private IMyMotorStator tAzimuth;

            private IMyShipController dataBlock;

            private List<IMyMotorStator> ElevationP = new List<IMyMotorStator>();
            private List<IMyMotorStator> ElevationM = new List<IMyMotorStator>();
            private List<IMySmallGatlingGun> Gatling = new List<IMySmallGatlingGun>();
            private List<IMySmallMissileLauncher> Missile = new List<IMySmallMissileLauncher>();
            private List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>();
            private List<IMyFunctionalBlock> Indicators = new List<IMyFunctionalBlock>();
            private List<IMyShipConnector> Connectors = new List<IMyShipConnector>();

            private float[] boundariesX;
            private float[] boundariesY;
            private float[] gradients;
            private float minElev = -90;
            private float maxspeed = 20 *Convert.ToSingle(Math.PI) / 30;
            private Vector3 offset = new Vector3(0, 0, 0);

            public int reloadTime = 60;
            public int reload = 0;
            float idleX = 0;
            float idleY = 10*3.1415f/180;
            bool relaod = false;
            bool reloading = false;
            bool idle = false;

            float d_x;
            float ang_y;
            float v_x;
            float v_y;

            bool targetVicinity;
            bool targetAccurate;


            double target_distance;

            TargetList targetList;


            public Turret( IMyTerminalBlock turretcore,IMyShipController shipRemote)
            {
                core = turretcore;

                try
                {
                    dataBlock = core as IMyShipController;
                }
                catch (Exception)
                {
                    dataBlock = shipRemote;
                }
                dataBlock = shipRemote;


            }

            public void Setup(Program program,List<IMyMotorStator> Rotors, TargetList list)
            {
                targetList = list;

                program.GridTerminalSystem.GetBlocksOfType(ElevationP, (x => (x.CubeGrid == core.CubeGrid) && (x.CustomName.Contains("T_Elevation_+"))));
                program.GridTerminalSystem.GetBlocksOfType(ElevationM, (x => (x.CubeGrid == core.CubeGrid) && (x.CustomName.Contains("T_Elevation_-"))));

                List<IMyCubeGrid> Grids = new List<IMyCubeGrid>();

                foreach (IMyMotorStator rotor in ElevationP)
                    Grids.Add(rotor.TopGrid);

                foreach (IMyMotorStator rotor in ElevationM)
                    Grids.Add(rotor.TopGrid);


                foreach (IMyMotorStator rotor in Rotors)
                {
                    if (rotor.IsAttached && rotor.TopGrid == core.CubeGrid)
                    {
                        if (rotor.CustomName.Contains("T_Elevation_+"))
                        {
                            ElevationP.Add(rotor);
                            Grids.Add(rotor.CubeGrid);
                            rotor.SafetyLock = false;
                        }
                        else if (rotor.CustomName.Contains("T_Elevation_-"))
                        {
                            ElevationM.Add(rotor);
                            Grids.Add(rotor.CubeGrid);
                            rotor.SafetyLock = false;
                        }
                        else if (rotor.CustomName.Contains("T_Azimuth"))
                            tAzimuth = rotor;
                    }
                }

                program.GridTerminalSystem.GetBlocksOfType(Gatling, x => (Grids.Contains(x.CubeGrid) && !x.CustomName.Contains("[i]")));
                program.GridTerminalSystem.GetBlocksOfType(Missile, x => (Grids.Contains(x.CubeGrid) && !x.CustomName.Contains("[i]")));
                program.GridTerminalSystem.GetBlocksOfType(Indicators, x => (Grids.Contains(x.CubeGrid) && x.CustomName.Contains("T_Indicator")));
                program.GridTerminalSystem.GetBlocksOfType(Connectors, x => (Grids.Contains(x.CubeGrid) && !(x.CustomName.Contains("[i]"))));
                program.GridTerminalSystem.GetBlocksOfType(Cameras, x => (Grids.Contains(x.CubeGrid) && !(x.CustomName.Contains("[i]"))));

                foreach (IMyCameraBlock camera in Cameras)
                {
                    camera.EnableRaycast = true;
                }

                foreach (IMyFunctionalBlock indicator in Indicators)
                    indicator.Enabled = false;

                ReadProperties(core.CustomData.Split('\n'));

            }

            public void ReadProperties(string[] properties)
            {
                var xy = new List<Vector2>();
                foreach (string property in properties)
                {
                    if (property.StartsWith("x:"))
                    {
                        string[] substrings = property.Split(' ');
                        xy.Add(new Vector2(ConvertToRadians((Int32.Parse(substrings[0].Substring(2)) + 360) % 360), ConvertToRadians(Int32.Parse(substrings[1].Substring(2)))));
                    }
                    else if (property.StartsWith("Offset:"))
                    {
                        string[] substrings = property.Split(':');
                        offset.Z = core.CubeGrid.GridSize * Convert.ToSingle(substrings[1]);
                        offset.X = -core.CubeGrid.GridSize * Convert.ToSingle(substrings[2]);
                        offset.Y = -core.CubeGrid.GridSize * Convert.ToSingle(substrings[3]);
                    }
                    else if (property.StartsWith("Speed:"))
                    {
                        string[] substrings = property.Split(':');
                        maxspeed = Single.Parse(property.Substring(6))*Convert.ToSingle(Math.PI)/30;
                    }
                }
                int n = xy.Count;

                if (n > 1)
                {
                    xy.Sort((v1, v2) => (v1.X.CompareTo(v2.X)));

                    boundariesX = new float[n];
                    boundariesY = new float[n];
                    gradients = new float[n];

                    for (int i = 0; i < n - 1; i++)
                    {
                        boundariesX[i] = xy[i].X;
                        boundariesY[i] = xy[i].Y;
                        gradients[i] = (xy[i + 1].Y - xy[i].Y) / (xy[i + 1].X - xy[i].X);
                    }
                    boundariesX[n - 1] = xy[n - 1].X;
                    boundariesY[n - 1] = xy[n - 1].Y;
                    gradients[n - 1] = (xy[0].Y - xy[n - 1].Y) / (xy[0].X - xy[n - 1].X + 2 * Convert.ToSingle(Math.PI));
                }
            }

            public bool Turn(Program program, Vector3D target)
            {
                float posX = (tAzimuth.Angle);

                float minY = GetMinY(posX);

                if (idle)
                    if (ang_y < minY - 0.04)
                    {
                        tAzimuth.TargetVelocityRad = program.limit(d_x * 8, maxspeed);
                        return false;
                    }
                    else
                    {
                        idle = false;
                        Lock(false);
                    }

                float posY = (ElevationP[0] ?? ElevationM[0])?.Angle ?? -100;

                if (ang_y < minY)
                {
                    targetVicinity = false;
                    targetAccurate = false;

                    if (ang_y < minY - 0.08)
                    {
                        if(Math.Abs(posY - idleY) < 0.001)
                        {
                            foreach (IMyMotorStator rotor in ElevationP)
                                rotor.TargetVelocityRad = 0;

                            foreach (IMyMotorStator rotor in ElevationM)
                                rotor.TargetVelocityRad = 0;

                            Lock(true);
                            idle = true;
                            return false;
                        }
                        
                        
                        ang_y = idleY;
                    }
                    else
                        ang_y = minY;
                }

                foreach (IMyFunctionalBlock block in Indicators)
                    block.Enabled = targetVicinity;
                /*
                if (targetVicinity)
                    foreach (IMyCameraBlock c in Cameras)
                    {
                        MyDetectedEntityInfo temp = c.Raycast(target);
                        if (!temp.IsEmpty()&& !targetList.IsFriendly(temp))
                        {
                            targetList.Add(temp);
                            break;
                        }
                    }
                    */

                tAzimuth.TargetVelocityRad = program.limit(d_x * 8+v_x, maxspeed);

                foreach (IMyMotorStator rotor in ElevationP)
                    rotor.TargetVelocityRad = program.limit((ang_y - rotor.Angle) * 8+v_y, maxspeed);

                foreach (IMyMotorStator rotor in ElevationM)
                    rotor.TargetVelocityRad = program.limit((-ang_y - rotor.Angle) * 8-v_y, maxspeed);

                return targetAccurate;
            }

            public bool CanTurn(float posX, float posY)
            {
                if (posY < minElev)
                    return false;

                return posX >= GetMinY(posX);
            }

            public float GetMinY (float posX)
            {
                if (boundariesX != null)
                {
                    if (posX > boundariesX.Last())
                        return boundariesY.Last() + ((posX - boundariesX.Last())) * gradients.Last();
                    else if (posX < boundariesX[0])
                        return boundariesY.Last() + ((posX - boundariesX.Last() + 6.2831853f)) * gradients.Last();
                    else
                    {
                        int i = 1;
                        while (posX > boundariesX[i])
                            i++;
                        return boundariesY[i - 1] + ((posX - boundariesX[i - 1])) * gradients[i - 1];
                    }
                }
                return minElev;
            }

            public void Echo(Program program)
            {
                program.Echo("Turret");

                if (boundariesX != null)
                    for (int i = 0; i < boundariesX.Length; i++)
                        program.Echo("  x:" + boundariesX[i] + " y:" + boundariesY[i]);



                /*program.Echo("  "+core.CustomName);
                program.Echo (" "+tAzimuth.CustomName);
                foreach (IMyMotorStator rotor in ElevationP)
                    program.Echo("  " + rotor.CustomName);
                foreach (IMyMotorStator rotor in ElevationM)
                    program.Echo("  " + rotor.CustomName);*/
            }

            public bool Target(Vector3D target, Vector3D velocity, Program program, bool sequencer)
            {
                ConvertPositionAndVelocity(target,velocity);

                bool onTarget = this.Turn(program,target);

                if (onTarget)
                    foreach (IMySmallGatlingGun block in Gatling)
                    {
                        block.Enabled = true;
                        if (sequencer)
                            block.ApplyAction("ShootOnce");
                    }
                else
                    foreach (IMyFunctionalBlock block in Gatling)
                        block.Enabled = false;

                if (onTarget&&sequencer)
                    foreach (IMyFunctionalBlock block in Missile)
                        block.Enabled = true;
                else
                    foreach (IMyFunctionalBlock block in Missile)
                        block.Enabled = false;


                
                return true;
            }

            private void Lock (bool enabled)
            {
                foreach (IMyShipConnector connector in Connectors)
                    if (enabled)
                        connector.Connect();
                    else
                        connector.Disconnect();
            }

            public Vector3 ToLocalSpherical(Vector3D target)
            {
                Vector3D targetVector = target - core.GetPosition();

                double front = targetVector.Dot(core.WorldMatrix.Forward) + offset.X;
                double right = targetVector.Dot(core.WorldMatrix.Right) + offset.Y;
                double up = targetVector.Dot(core.WorldMatrix.Up) + offset.Z;

                double deltaAzimuth = Math.Atan(right / front);
                double planelength = front / Math.Cos(deltaAzimuth);
                double elevation = Math.Atan(up / planelength);
                double length = planelength / Math.Cos(elevation);

                if (front < 0)
                {
                    deltaAzimuth += Math.PI;
                    elevation = -elevation;
                }

                if (deltaAzimuth > Math.PI)
                    deltaAzimuth -= 2 * Math.PI;


                return new Vector3(length, deltaAzimuth, elevation);
            }

            public void ConvertPositionAndVelocity(Vector3D position,Vector3D velocity)
            {
                var worldToAnchorLocalMatrix =  Matrix.Transpose(core.WorldMatrix.GetOrientation()); //x=right y=up z=back

                
                Vector3D targetDirection = Vector3D.Transform(position - core.GetPosition(), worldToAnchorLocalMatrix)+offset;
                
                Vector3D targetRelativeVelocity = Vector3D.Transform(velocity - dataBlock.GetShipVelocities().LinearVelocity, worldToAnchorLocalMatrix);

                targetDirection += targetRelativeVelocity * target_distance / 400;

                target_distance = targetDirection.Length();

                ang_y = Convert.ToSingle(Math.Asin(targetDirection.Y / target_distance));

                if (targetDirection.Z < 0)
                {
                    d_x = Convert.ToSingle(-targetDirection.X / targetDirection.Z);
                }
                else
                {
                    d_x = Convert.ToSingle(targetDirection.X * 1000);
                }

                float d_y = ang_y - ((ElevationP[0] ?? ElevationM[0])?.Angle ?? -100);

                if (Math.Abs(d_x) > 0.3f || Math.Abs(d_y) > 0.3f)
                {
                    targetVicinity = false;
                    targetAccurate = false;
                    v_x = 0;
                    v_y = 0;
                    return;
                }

                targetVicinity = true;

                

                targetAccurate = Math.Abs(d_x) < 0.0523599 && Math.Abs(d_y) < 0.0523599;

                v_x = Convert.ToSingle(-targetRelativeVelocity.X / targetDirection.Z);
                v_y = Convert.ToSingle((targetRelativeVelocity.Z * Math.Sin(ang_y) + targetRelativeVelocity.Y * Math.Cos(ang_y)) / target_distance);
                

            }
        }
    }
}
