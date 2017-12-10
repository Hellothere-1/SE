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
        public class GyroscopeSystem
        {
            private struct Gyro
            {
                public IMyGyro block;
                public int[] conversionVector;
            }
            private List<Gyro> Gyros = new List<Gyro>();

            public GyroscopeSystem(List<IMyGyro> Gyroscopes, IMyTerminalBlock Reference)
            {
                foreach (IMyGyro gyroscope in Gyroscopes)
                {
                    Gyro g = new Gyro();
                    g.block = gyroscope;
                    g.conversionVector = new int[3];

                    for (int i = 0; i < 3; i++)
                    {
                        Vector3D vectorShip = GetAxis(i, Reference);

                        for (int j = 0; j < 3; j++)
                        {
                            double dot = vectorShip.Dot(GetAxis(j, g.block));

                            if (dot > 0.9)
                            {
                                g.conversionVector[j] = i;
                                break;
                            }
                            if (dot < -0.9)
                            {
                                g.conversionVector[j] = i + 3;
                                break;
                            }
                        }
                    }
                    Gyros.Add(g);
                }
            }

            public void SetRotation(float pitch, float yaw, float roll)
            {
                float[] rotationVector = new float[] { -pitch, yaw, -roll, pitch, -yaw, roll };
                foreach (Gyro g in Gyros)
                {
                    g.block.Pitch = rotationVector[g.conversionVector[0]]*100;
                    g.block.Yaw = rotationVector[g.conversionVector[1]]*100;
                    g.block.Roll = rotationVector[g.conversionVector[2]]*100;
                    //p.Echo(Convert.ToString(g.conversionVector[0]) + Convert.ToString(g.conversionVector[1]) + Convert.ToString(g.conversionVector[2]));
                }
            }

            public void SetOverride(bool enabled)
            {
                foreach (Gyro g in Gyros)
                {
                    g.block.GyroOverride = enabled;
                }
            }

            private Vector3D GetAxis(int dimension, IMyTerminalBlock block)
            {
                switch (dimension)
                {
                    case 0:
                        return block.WorldMatrix.Right;
                    case 1:
                        return block.WorldMatrix.Up;
                    default:
                        return block.WorldMatrix.Backward;
                }
            }
        }
    }
}
