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
        public class Gyroscope
        {
            public IMyGyro Gyro;
            private int[] conversionVector = new int[3];

            public Gyroscope(IMyGyro Gyroscope, IMyTerminalBlock Reference)
            {
                Gyro = Gyroscope;

                for (int i = 0; i < 3; i++)
                {
                    Vector3D vectorShip = GetAxis(i, Reference);

                    for (int j = 0; j < 3; j++)
                    {
                        double dot = vectorShip.Dot(GetAxis(j, Gyro));

                        if (dot > 0.9)
                        {
                            conversionVector[j] = i;
                            break;
                        }
                        if (dot < -0.9)
                        {
                            conversionVector[j] = i + 3;
                            break;
                        }
                    }
                }
            }

            public void SetRotation(float[] rotationVector)
            {
                Gyro.Pitch = rotationVector[conversionVector[0]];
                Gyro.Yaw = rotationVector[conversionVector[1]];
                Gyro.Roll = rotationVector[conversionVector[2]];
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
