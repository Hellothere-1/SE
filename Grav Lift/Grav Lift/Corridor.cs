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
        public class Corridor
        {
            IMyGravityGenerator core;
            Program main;
            List <IMyGravityGenerator> grav = new List<IMyGravityGenerator>(); //List of Generators
            List <Base6Directions.Direction> gravDirections = new List<Base6Directions.Direction>(); //List of directions for the generators 
            int[] numberOfGeneratorsInDirection = new int[3] { 0, 0, 0 };

            Vector3I parallelDir;
            Vector3I normalDir;
            Vector3I position;
            Vector3  referenceOffset;

            Matrix localRotationMatrix;


            public Corridor(IMyGravityGenerator core, Program program)
            {
                this.core = core;
                main = program;
                position = core.Position - Base6Directions.GetIntVector(core.Orientation.Up);
                referenceOffset = (position - main.reference.Position) * 2.5f;

                parallelDir = Vector3I.Abs(Base6Directions.GetIntVector(main.reference.Orientation.TransformDirectionInverse(core.Orientation.Forward)));
                normalDir = Vector3I.One - parallelDir;

                core.Orientation.GetMatrix(out localRotationMatrix);
                localRotationMatrix = Matrix.Transpose(localRotationMatrix);
            }


            public int GetRectDistance(IMyCubeBlock block)
            {
                return core.Position.RectangularDistance(block.Position);
            }


            /// <summary>
            /// Adds Gravity Generator to list of Generators
            /// </summary>
            /// <param name="generator"></param>
            public void AddGenerator(IMyGravityGenerator generator)
            {
                grav.Add(generator);
                Base6Directions.Direction direction = core.Orientation.TransformDirectionInverse(generator.Orientation.Up); //transforms the "Up" direction of the generator into the direction of the reference remote
                gravDirections.Add(direction);
                numberOfGeneratorsInDirection[((int)Base6Directions.GetAxis(direction)+2)%3]++;

            }

            public void SetGravity(Vector3 gravity)
            {
                gravity.X/=numberOfGeneratorsInDirection[0];
                gravity.Y /= numberOfGeneratorsInDirection[1];
                gravity.Z /= numberOfGeneratorsInDirection[2];

                for (int i = 0; i< grav.Count;i++)
                {
                    float acceleration = -gravity.Dot(Base6Directions.GetVector(gravDirections[i]));
                    grav[i].GravityAcceleration = acceleration;
                    grav[i].Enabled = acceleration != 0;
                }
            }

            public void tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc)
            {
                position = Vector3.Transform(position - referenceOffset, localRotationMatrix);
                velocity = Vector3.Transform(velocity, localRotationMatrix);
                compensateAcc = Vector3.Transform(compensateAcc, localRotationMatrix);

                Vector3 grav = - position - velocity;

                grav.Z = Math.Abs(position.X)<1 && Math.Abs(position.Y) < 1 ? 8.91f : 0;

                SetGravity(grav + compensateAcc);

            }
        }


    }
}
