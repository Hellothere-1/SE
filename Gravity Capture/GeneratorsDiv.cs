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
        public class GeneratorsDiv
        {
            List<Gravity> Generators;

            public GeneratorsDiv(List<Gravity> generators)
            {

                Generators = generators;
            }
            public void SetGravity(float value)
            {
                foreach (Gravity generator in Generators)
                    generator.SetGravity(value);
            }
            public float GetGravity()
            {
                return Generators[0].GetGravity();
            }
            public void ResetDimensions()
            {
                foreach (Gravity generator in Generators)
                    generator.ResetDimensions();
            }
            public void ResetX()
            {
                foreach (Gravity generator in Generators)
                    generator.ResetX();
            }
            public void ResetY()
            {
                foreach (Gravity generator in Generators)
                    generator.ResetY();
            }
            public void ResetZ()
            {
                foreach (Gravity generator in Generators)
                    generator.ResetZ();
            }
            public void OffsetX(float value)
            {
                Vector3 dim = Generators[0].dimensions;
                dim.X += value;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void OffsetY(float value)
            {
                Vector3 dim = Generators[0].dimensions;
                dim.Y += value;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
            public void OffsetZ(float value)
            {
                Vector3 dim = Generators[0].dimensions;
                dim.Z += value;
                foreach (IMyGravityGenerator generator in Generators)
                    generator.FieldSize = dim;
            }
        }
    }
}
