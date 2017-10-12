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
            enum Side {RIGHT, UP, BACK, LEFT, DOWN, FRONT};
            
            Program parent;
            IMyCameraBlock visor;

            //Variables for hit calculation
            int TotalMass = -1;
            float MaxThrust = -1;
            float MaxAcceleration = -1;
            //

            Dictionary<Side, List<IMyThrust>> thrusters = new Dictionary<Side, List<IMyThrust>>();
            Dictionary<Side, float> thrust = new Dictionary<Side, float>();

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
                course.Y = 10 * course.Y / Math.PI;
                course.Z = 10 * course.Z / Math.PI;
                lastCourse = course;
                if (ticks != 1)
                {
                    return lastCourse;
                }
                return course;
            }


            public void init(IMyRemoteControl control)
            {
                TotalMass = control.CalculateShipMass().TotalMass;

                List<IMyThrust> thruster = new List<IMyThrust>();
                parent.GridTerminalSystem.GetBlocksOfType(thruster);
                foreach (IMyThrust thru in thruster)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (thru.WorldMatrix.Forward == GetAxis(i, control))
                        {
                            thrusters[(Side) i].Add(thru);
                            thrust[(Side)i] = thrust[(Side)i] + thru.MaxThrust;
                        }
                    }
                }
                //temporary Solution, advanced Flight computing needed

                MaxThrust = thrust[Side.BACK];
                MaxAcceleration = MaxThrust / TotalMass;
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

                double ownSpeed = control.GetShipSpeed();
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
                timeToTarget = (float) (distanceAtMaxSpeed / (relativeVelocity + (100 - ownSpeed)) + timeToMax);
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

            private Vector3D GetAxis(int dimension, IMyTerminalBlock block)
            {
                switch (dimension)
                {
                        case 0:
                            return block.WorldMatrix.Right;
                        case 1:
                            return block.WorldMatrix.Up;
                        case 2:
                            return block.WorldMatrix.Backward;
                        case 3:
                            return -block.WorldMatrix.Right;
                        case 4:
                            return -block.WorldMatrix.Up;
                        default:
                            return -block.WorldMatrix.Backward;
                    }
            }
        }
    }
}
