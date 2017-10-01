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
            Program parent;
            public TargetFuncs(Program par)
            {
                parent = par;
            }

            public Vector3D GetShipPosition(MyDetectedEntityInfo target)
            {
                return target.Position;
            }

            public Vector3D GetShipVelocity(MyDetectedEntityInfo target)
            {
                return target.Velocity;
            }

            public Vector3D GetShipPredictedPosition(Vector3D lastPos, Vector3D lastVel, float ticks)
            {
                parent.Echo("Pos: " + lastPos + " Vel : " + lastVel + " Tick : " + ticks);
                Vector3D prepos = lastPos + ((ticks / 60F) * lastVel);
                return prepos;
            }

            public Vector3 ToLocalSpherical(Vector3D target, IMyRemoteControl core, Vector3 offset)
            {
                Vector3D targetVector = target - core.GetPosition();
                double front = targetVector.Dot(core.WorldMatrix.Forward) + offset.X;
                double right = targetVector.Dot(core.WorldMatrix.Right) + offset.Y;
                double up = targetVector.Dot(core.WorldMatrix.Up) + offset.Z;
                double deltaAzimuth = Math.Atan(right / front);
                double planelength = Math.Cos(deltaAzimuth) * front;
                double elevation = Math.Atan(up / planelength);
                double length = planelength * Math.Cos(elevation);
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
        }
    }
}
