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
        public class FlightComputer
        {
            float[] timeToZero = new float[6];
            int totalMass;
            Vector3D currentAxisVelocity;

            public FlightComputer()
            {

            }




            Vector3 ToLocalSpherical(Vector3D target, IMyRemoteControl core)
            {
                Vector3D targetVector = target - core.GetPosition();
                double front = targetVector.Dot(core.WorldMatrix.Forward);
                double right = targetVector.Dot(core.WorldMatrix.Right);
                double up = targetVector.Dot(core.WorldMatrix.Up);
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
        }
    }
}
