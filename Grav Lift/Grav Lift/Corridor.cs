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
        public class Corridor : Waypoint
        {
            IMyGravityGenerator core;
            Program main;
            List<IMyGravityGenerator> grav = new List<IMyGravityGenerator>(); //List of Generators
            List<Base6Directions.Direction> gravDirections = new List<Base6Directions.Direction>(); //List of directions for the generators 
            int[] numberOfGeneratorsInDirection = new int[3] { 0, 0, 0 };

            Vector3I position;
            Vector3 referenceOffset;

            Matrix localRotationMatrix;

            public List<Corner> corners { get; private set; } = new List<Corner>();
            List<float> cornerCoordinates = new List<float>();

            public List<Station> stations { get; private set; } = new List<Station>();


            public Corridor(IMyGravityGenerator core, Program program)
            {
                this.core = core;
                main = program;
                position = core.Position - Base6Directions.GetIntVector(core.Orientation.Up);
                referenceOffset = (position - main.reference.Position) * 2.5f;

                core.Orientation.GetMatrix(out localRotationMatrix);
                localRotationMatrix = Matrix.Transpose(localRotationMatrix);
            }


            public int GetRectDistance(IMyCubeBlock block)
            {
                return core.Position.RectangularDistance(block.Position);
            }

            public float GetSquareDistance(IMyCubeBlock block)
            {
                Vector3 positionToCorridor = Vector3.Transform(block.Position - core.Position, localRotationMatrix);
                return Math.Abs(positionToCorridor.X) + Math.Abs(positionToCorridor.Y);
            }

            public bool IsInCorridor(IMyCubeBlock block)
            {
                return Vector3I.Dot(block.Position - core.Position, Base6Directions.GetIntVector(core.Orientation.Forward)) == (block.Position-core.Position).RectangularLength();
            }


            /// <summary>
            /// Adds Gravity Generator to list of Generators
            /// </summary>
            /// <param name="generator"></param>
            public void AddGenerator(IMyGravityGenerator generator)
            {
                grav.Add(generator);
                Base6Directions.Direction direction = core.Orientation.TransformDirectionInverse(generator.Orientation.Up); //transforms the "Up" direction of the generator into the direction of the corridor coordinates
                gravDirections.Add(direction);
                numberOfGeneratorsInDirection[((int)Base6Directions.GetAxis(direction) + 2) % 3]++;

            }

            public void SetGravity(Vector3 gravity)
            {
                gravity.X /= numberOfGeneratorsInDirection[0];
                gravity.Y /= numberOfGeneratorsInDirection[1];
                gravity.Z /= numberOfGeneratorsInDirection[2];

                for (int i = 0; i < grav.Count; i++)
                {
                    float acceleration = -gravity.Dot(Base6Directions.GetVector(gravDirections[i]));
                    grav[i].GravityAcceleration = acceleration;
                    grav[i].Enabled = acceleration != 0;
                }
            }

            public void tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc, Vector3 target)
            {
                //transforming inputs into local coordinates
                position = Vector3.Transform(position - referenceOffset, localRotationMatrix);
                velocity = Vector3.Transform(velocity, localRotationMatrix);
                compensateAcc = Vector3.Transform(compensateAcc, localRotationMatrix);

                Vector3 grav;

                //grav = -position - velocity;
                //grav.Z = target.Z - position.Z - velocity.Z;


                if (Math.Abs(position.Z - target.Z) < 1f&& Math.Abs(velocity.Z)<3)
                {
                    if(Vector3.RectangularDistance(position,target)<1.25f)
                    {
                        SetGravity(Vector3.Zero);
                        return;
                    }

                    grav = target - position - velocity/2;
                }
                else if (Math.Abs(position.X) < 1 && Math.Abs(position.Y) < 1)
                {
                    grav = -position - velocity;

                    float dCurrent = target.Z - position.Z;

                    if (velocity.Z * dCurrent < 0)
                    {
                        grav.Z = Math.Sign(dCurrent) * GetMaxacceleration(2);
                    }
                    else
                    {
                        float dStop = GetStoppingDistance(velocity.Z);

                        if (Math.Abs(dCurrent) < dStop)
                        {
                            grav.Z = -Math.Sign(dCurrent) * GetMaxacceleration(2) * 1.2f;
                        }
                        else if (Math.Abs(dCurrent) < dStop + 5)
                        {
                            grav.Z = -Math.Sign(dCurrent);
                        }
                        else
                        {
                            grav.Z = Math.Sign(dCurrent) * GetMaxacceleration(2);
                        }
                    }
                }
                else
                {
                    grav = -position - velocity;
                    grav.Z = 0;
                }

                SetGravity(grav + compensateAcc);
            }


            public void AddStation(Station station)
            {
                stations.Add(station);
                Vector3 positionInCorridor = Vector3.Transform(station.panel.Position -position, localRotationMatrix) * 2.5f;
                station.SetCorridor(this, positionInCorridor);
            }

            public void AddCorner(Corner corner)
            {
                corners.Add(corner);
                cornerCoordinates.Add(Vector3.Transform(corner.block.Position - position, localRotationMatrix).Z * 2.5f);
                corner.AddCorridor(this);
            }

            public float GetStoppingDistance(float vz)
            {
                return 0.5f * vz * vz / GetMaxacceleration(2);
            }


            public float GetMaxacceleration(int axis)
            {
                return numberOfGeneratorsInDirection[axis] * 8f;
            }
        }


    }
}
