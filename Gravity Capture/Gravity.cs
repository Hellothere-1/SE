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
            IMyGravityGeneratorBase Generator;
            float[] dimensions = new float[3];
            bool negateGravity;

            public Gravity(IMyGravityGeneratorBase block,float width,float height,float depth)
            {
                dimensions[0] = width;
                dimensions[1] = height;
                dimensions[2] = depth;
                Generator = block;
                negateGravity = Generator.CustomName.Contains('-');
            }
            public void SetGravity(float value)
            { 
                if (negateGravity)
                    Generator.GravityAcceleration = - value;
                else
                    Generator.GravityAcceleration = value;
            }
        }
    }
}
