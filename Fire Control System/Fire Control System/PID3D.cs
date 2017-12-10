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
        public class PID3D
        {
            private Vector3D p, i, d, lastPoint, sum;

            public PID3D()
            {
                p = new Vector3D(1, 1, 1);
                i = new Vector3D(1, 1, 1);
                d = new Vector3D(1, 1, 1);
                lastPoint = new Vector3D(0, 0, 0);
                sum = new Vector3D(0, 0, 0);
            }

            public PID3D(Vector3D pIn, Vector3D iIn, Vector3D dIn)
            {
                p = pIn;
                i = iIn;
                d = dIn;

                lastPoint = new Vector3D(0, 0, 0);
                sum = new Vector3D(0, 0, 0);
            }

            public Vector3D tick(Vector3D point, Vector3D setpoint)
            {
                Vector3D diff = new Vector3D();
                diff = setpoint - point;

                Vector3D differential = new Vector3D();
                differential = lastPoint - point;

                Vector3D output = new Vector3D();
                output.X = +diff.X * p.X + sum.X * i.X + differential.X * d.X;
                output.Y = +diff.Y * p.Y + sum.Y * i.Y + differential.Y * d.Y;
                output.Z = +diff.Z * p.Z + sum.Z * i.Z + differential.Z * d.Z;

                sum = sum + diff;
                lastPoint = point;
                return output;
            }

            public void reset()
            {
                sum.X = 0;
                sum.Y = 0;
                sum.Z = 0;
            }
            public void multiplySumX(float factor)
            {
                sum.X = sum.X * factor;
            }

            public void multiplySumZ(float factor)
            {
                sum.Z = sum.Z * factor;
            }

            public void limitSumX(float upper, float lower)
            {
                if (sum.X > upper)
                    sum.X = upper;
                else if (sum.X < lower)
                    sum.X = lower;
            }

            public void limitSumY(float upper, float lower)
            {
                if (sum.Y > upper)
                    sum.Y = upper;
                else if (sum.Y < lower)
                    sum.Y = lower;
            }

            public void limitSumZ(float upper, float lower)
            {
                if (sum.Z > upper)
                    sum.Z = upper;
                else if (sum.Z < lower)
                    sum.Z = lower;
            }
        }

    }
}
