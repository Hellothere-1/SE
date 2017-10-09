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
        public class TargetFuncs
        {
            Vector3 offset = new Vector3(0, 0, -0.4);

            
            Program parent;
            IMyCameraBlock visor;
            IMyTextPanel debug;

            //Variables for hit calculation
            int TotalMass = -1;
            float MaxThrust = -1;
            float MaxAcceleration = -1;
            //


            MyDetectedEntityInfo TargetInfo;
            Vector3D predictedHitPoint;
            Vector3D predictedPosition;
            Vector3 predictedPositionAngles;
            Vector3 lastCourse;
            float lastDistance = -1;
            float timeToTarget = -1;
            bool targetFound = false;
            int ticks = 1;


            public TargetFuncs(Program par, IMyCameraBlock cam)
            {
                parent = par;
                visor = cam;
                debug = parent.GridTerminalSystem.GetBlockWithName("Debug") as IMyTextPanel;
            }
            

            public Vector3 run(IMyRemoteControl control)
            {

                int ticksSinceLastHit = RayCastShip(control);
                if (targetFound == false)
                {
                    return new Vector3(0, 0, 0);
                }
                UpdateHitPosition(control, ticksSinceLastHit);
                Vector3D course = GetBurnVector(control, true);
                course = ToLocalSpherical(course, control);
                lastCourse = course;
                if (ticks != 1)
                {
                    return lastCourse;
                }
                return course;
            }
            

            void UpdateHitPosition(IMyRemoteControl control, int tick)
            {
                if (ticks != 1)
                {
                    //RayCast not long enough, no new information acquired
                    return;
                }
                double relativeVelocity = lastDistance - predictedPositionAngles.X;
                double timeFactor = 60D /(double) tick;
                relativeVelocity = relativeVelocity * timeFactor;


                if (TotalMass == -1)
                {
                    TotalMass = control.CalculateShipMass().TotalMass;
                }
                if (MaxThrust == -1)
                {
                    MaxThrust = 0;
                    List<IMyThrust> thruster = new List<IMyThrust>();
                    parent.GridTerminalSystem.GetBlocksOfType(thruster);
                    foreach (IMyThrust thrust in thruster)
                    {
                        if (thrust.WorldMatrix.Forward == control.WorldMatrix.Backward)
                        {
                            MaxThrust += thrust.MaxThrust;
                        }
                    }
                }
                if (MaxAcceleration == -1)
                {
                    MaxAcceleration = MaxThrust / TotalMass;
                }
                /*
                currentVel = control.GetShipSpeed();
                float difference =(float) (100 - currentVel);

                float timeToMaxSpeed = difference == 0 ? 0 : difference / MaxAcceleration;
                float timeToZeroSpeed = relativeVelocity < 0 ? (float)(-relativeVelocity / MaxAcceleration) : 0;
                float timeInPositivSpeed = timeToMaxSpeed - timeToZeroSpeed;*/

                debug.WritePublicText("Real : " + relativeVelocity.ToString() + "m/s \n");

                

                double ownSpeed = control.GetShipSpeed();
                debug.WritePublicText("Speed : " + ownSpeed.ToString() + "m/s \n", true);
                debug.WritePublicText("Acceleration : " + MaxAcceleration.ToString() + "m/s² \n", true);
                double timeToMax;
                double distanceAtMaxSpeed;
                if (ownSpeed <= 99)
                {
                    timeToMax = (100 - ownSpeed) / MaxAcceleration;
                    distanceAtMaxSpeed = predictedPositionAngles.X - (relativeVelocity + (MaxAcceleration * timeToMax) / 2) * timeToMax;
                }
                else
                {
                    timeToMax = 0;
                    distanceAtMaxSpeed = predictedPositionAngles.X;
                }
                debug.WritePublicText("Time to Max: " + timeToMax.ToString() + "s \n", true);
                debug.WritePublicText("Distance : " + distanceAtMaxSpeed.ToString() + "m \n", true);
                timeToTarget = (float) (distanceAtMaxSpeed / (relativeVelocity + (100 - ownSpeed)) + timeToMax);

                
                debug.WritePublicText("Time : " +timeToTarget.ToString() + "s \n", true);


                /*Trying heavy calculations to ensure correct point
                debug.WritePublicText("Real : " + relativeVelocity.ToString() + "m/s \n");

                //relativeVelocity = relativeVelocity + (101 - control.GetShipSpeed());
                if (relativeVelocity <= 0)
                    relativeVelocity = 25; 

                debug.WritePublicText("Corrected : " + relativeVelocity.ToString() + "m/s \n", true);
                */



                //timeToTarget = (float) (predictedPositionAngles.X / relativeVelocity);
                predictedHitPoint = TargetInfo.Position + timeToTarget * TargetInfo.Velocity;
                lastDistance = predictedPositionAngles.X;
            }


            int RayCastShip(IMyRemoteControl control)
            {
                int output = -1;
                GetShipPredictedPosition(TargetInfo.Position, TargetInfo.Velocity, ticks);
                if (predictedPositionAngles.X * 1.1 < visor.AvailableScanRange)
                {
                    //MyDetectedEntityInfo temp = visor.Raycast(predictedPositionAngles.X, predictedPositionAngles.Z, predictedPositionAngles.Y); For better Detecting... not working btw
                    MyDetectedEntityInfo temp = visor.Raycast(predictedPosition);
                    if (temp.IsEmpty())
                    {
                        targetFound = false;
                        //TODO Suche nach ziel einleiten
                    }
                    else
                    {
                        TargetInfo = temp;
                        targetFound = true;
                        predictedPositionAngles = ToLocalSpherical(TargetInfo.Position, control);
                    }
                    output = ticks;
                    ticks = 1;
                }
                else
                {
                    ticks++;
                }
                return output;
            }


            Vector3 ToLocalSpherical(Vector3D target, IMyRemoteControl core)
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
                {
                    deltaAzimuth -= 2 * Math.PI;
                }
                return new Vector3(length, deltaAzimuth, elevation);
            }


            void GetShipPredictedPosition(Vector3D lastPos, Vector3D lastVel, float ticks)
            {
                Vector3D prepos = lastPos + ((ticks / 60F) * lastVel);
                predictedPosition = prepos;
            }


            Vector3D GetBurnVector(IMyRemoteControl core, bool oldSystem)
            {
                
                if (oldSystem)
                {
                    Vector3D targetVector = predictedHitPoint - core.GetPosition();
                    targetVector.Normalize();
                    targetVector = targetVector * 120;
                    Vector3D velocity = core.GetShipVelocities().LinearVelocity;
                    Vector3D correctVector = targetVector - velocity;
                    return correctVector + core.GetPosition();
                }
                else
                {
                    Vector3D targetVector = predictedPosition - core.GetPosition();
                    return targetVector + timeToTarget * (TargetInfo.Velocity - 0.9 * core.GetShipVelocities().LinearVelocity);
                }
            }

            public void LockTarget(MyDetectedEntityInfo target, IMyRemoteControl control)
            {
                targetFound = true;
                TargetInfo = target;
                predictedPositionAngles = ToLocalSpherical(target.Position, control);
                predictedHitPoint = TargetInfo.Position;
                lastDistance = predictedPositionAngles.X;
            }


        }
    }
}
