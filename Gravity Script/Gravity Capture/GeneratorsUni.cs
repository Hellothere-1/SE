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
        public class GeneratorsUni
        {
            List <IMyGravityGenerator> Generators;
            public Vector3 dimensions;
            bool negateGravity;

            public GeneratorsUni(List<IMyGravityGenerator> blocks, float width, float height, float depth)
            {
                dimensions.X = width;
                dimensions.Y = height;
                dimensions.Z = depth;
                Generators = blocks;
                negateGravity = Generators[0].CustomName.Contains('-');
            }
            public void SetGravity(float value)
            {
                if (negateGravity)
                    value = -value;

                foreach (IMyGravityGenerator generator in Generators)
                    generator.GravityAcceleration = value;
            }
            public float GetGravity()
            {
                if (negateGravity)
                    return -Generators[0].GravityAcceleration;
                return Generators[0].GravityAcceleration;
            }
            public void ResetDimensions()
            {
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dimensions;
            }
            public void ResetX()
            {
                Vector3 dim = Generators[0].FieldSize;
                dim.X = dimensions.X;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void ResetY()
            {
                Vector3 dim = Generators[0].FieldSize;
                dim.Y = dimensions.Y;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void ResetZ()
            {
                Vector3 dim = Generators[0].FieldSize;
                dim.Z = dimensions.Z;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void SetX(float value)
            {
                Vector3 dim = Generators[0].FieldSize;
                dim.X = value;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void SetY(float value)
            {
                Vector3 dim = Generators[0].FieldSize;
                dim.Y = value;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void SetZ(float value)
            {
                Vector3 dim = Generators[0].FieldSize;
                dim.Z = value;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void OnOff(bool enabled)
            {
                foreach (IMyGravityGenerator generator in Generators)
                    generator.Enabled = enabled;
            }
        }
    }
}
