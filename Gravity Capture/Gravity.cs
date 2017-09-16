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
        public class Gravity
        {
            public IMyGravityGenerator Generator;
            public Vector3 dimensions;
            bool negateGravity;

            public Gravity(IMyGravityGenerator block, float width, float height, float depth)
            {
                dimensions.X = width;
                dimensions.Y = height;
                dimensions.Z = depth;
                Generator = block;
                negateGravity = Generator.CustomName.Contains('-');
            }
            public void SetGravity(float value)
            {
                if (negateGravity)
                    Generator.GravityAcceleration = -value;
                else
                    Generator.GravityAcceleration = value;
            }
            public float GetGravity()
            {
                if (negateGravity)
                    return -Generator.GravityAcceleration;
                return Generator.GravityAcceleration;
            }
            public void ResetDimensions()
            {
                Generator.FieldSize=dimensions;
            }
            public void ResetX()
            {
                Vector3 dim = Generator.FieldSize;
                dim.X = dimensions.X;
                Generator.FieldSize = dim;
            }
            public void ResetY()
            {
                Vector3 dim = Generator.FieldSize;
                dim.Y = dimensions.Y;
                Generator.FieldSize = dim;
            }
            public void ResetZ()
            {
                Vector3 dim = Generator.FieldSize;
                dim.Z = dimensions.Z;
                Generator.FieldSize = dim;
            }
            public void SetX(float value)
            {
                Generator.Enabled = value >= 1;
                Vector3 dim = Generator.FieldSize;
                dim.X = value;
                Generator.FieldSize = dim;
            }
            public void SetY(float value)
            {
                Generator.Enabled = value >= 1;
                Vector3 dim = Generator.FieldSize;
                dim.Y = value;
                Generator.FieldSize = dim;
            }
            public void SetZ(float value)
            {
                Generator.Enabled = value >= 1;
                Vector3 dim = Generator.FieldSize;
                dim.Z = value;
                Generator.FieldSize = dim;
            }
        }
    }
}
