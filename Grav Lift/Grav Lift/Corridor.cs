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

            int targetCornerIndex;

            public List<Station> stations { get; private set; } = new List<Station>();


            public Corridor(IMyGravityGenerator core, Program program)
            {
                this.core = core;
                main = program;
                int offset;
                if(!int.TryParse(core.CustomData, out offset))
                {
                    offset = 1;
                }    
                position = core.Position - offset * Base6Directions.GetIntVector(core.Orientation.Up);
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
                Vector3 positionToCorridor = Vector3.Abs(Vector3.Transform(block.Position - core.Position, localRotationMatrix));
                if (Math.Abs(positionToCorridor.X) * 5 > core.FieldSize.X || Math.Abs(positionToCorridor.Y) * 5 > core.FieldSize.Y || Math.Abs(positionToCorridor.Z) * 5 > core.FieldSize.Z)
                {
                    return float.MaxValue;
                }
                positionToCorridor = Vector3.Abs(Vector3.Transform(block.Position - position, localRotationMatrix));
                return Math.Abs(positionToCorridor.X) + Math.Abs(positionToCorridor.Y);
            }

            public bool IsInCorridor(IMyCubeBlock block)
            {
                Vector3I v1 = block.Position - position;

                if(Vector3I.Dot(v1, Base6Directions.GetIntVector(core.Orientation.Up)) != 0 || Vector3I.Dot(v1, Base6Directions.GetIntVector(core.Orientation.Left)) != 0)
                {
                    return false;
                }
                Vector3 positionToCorridor = Vector3.Abs(Vector3.Transform(v1, localRotationMatrix)) * 5;
                return Math.Abs(positionToCorridor.X) <= core.FieldSize.X && Math.Abs(positionToCorridor.Y) <= core.FieldSize.Y && Math.Abs(positionToCorridor.Z) <= core.FieldSize.Z;

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
                gravity.X = Clamp(gravity.X / numberOfGeneratorsInDirection[0], -9.81f, 9.81f);
                gravity.Y = Clamp(gravity.Y / numberOfGeneratorsInDirection[1], -9.81f, 9.81f);
                gravity.Z = Clamp(gravity.Z / numberOfGeneratorsInDirection[2], -9.81f, 9.81f);

                for (int i = 0; i < grav.Count; i++)
                {
                    float acceleration = -gravity.Dot(Base6Directions.GetVector(gravDirections[i]));
                    grav[i].GravityAcceleration = acceleration;
                    grav[i].Enabled = acceleration != 0;
                }
            }

            public override Waypoint tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc)
            {
                //transforming inputs into local coordinates
                position = Vector3.Transform(position - referenceOffset, localRotationMatrix);
                velocity = Vector3.Transform(velocity, localRotationMatrix);
                compensateAcc = Vector3.Transform(compensateAcc, localRotationMatrix);

                main.Echo("gens: " + numberOfGeneratorsInDirection[2]);

                if (Math.Abs(position.Z) > core.FieldSize.Z / 2)
                {
                    return nextWaypoint;
                }

                Vector3 grav;

                Vector3 target = GetTarget(this);
                CheckOpenDoors();

                if (Math.Abs(position.Z - target.Z) < 0.7f && Math.Abs(velocity.Z) < 3f) //near target on z axis
                {
                    if(Vector3.RectangularDistance(position, target)<1.25f)
                    {
                        SetGravity(Vector3.Zero);
                        return nextWaypoint;
                    }

                    grav = target - position - velocity/3 - compensateAcc;
                }
                else if (Math.Abs(position.X) < 1 && Math.Abs(position.Y) < 1) // inside corridor
                {
                    grav = -position - velocity - compensateAcc;

                    float dCurrent = target.Z - position.Z;

                    if (velocity.Z * dCurrent < 0)
                    {
                        grav.Z = dCurrent - compensateAcc.Z ;
                    }
                    else
                    {
                        float aStop = GetStoppingAccellerationPercentage(velocity.Z, dCurrent, compensateAcc);

                        if(Math.Abs(aStop) > 0.4f)
                        {
                            grav.Z = Math.Sign(aStop) * GetMaxacceleration(2) * (2 * Math.Abs(aStop) - 0.9f);
                            //if (Math.Abs(aStop) >= 1)
                            //{
                            //    grav.Z = aStop * GetMaxacceleration(2) * (Single)Math.Exp(-Math.Abs(dCurrent));
                            //}
                            //else
                            //{
                            //    grav.Z = aStop * GetMaxacceleration(2)*0.5f;
                            //}
                        }
                        else
                        {
                            grav.Z = dCurrent -compensateAcc.Z;
                        }
                    }
                }
                else //outside corridor
                {
                    grav = -position - velocity - compensateAcc;

                    float zBlockcenter = (Single)Math.Round(position.Z / 2.5) * 2.5f;
                    grav.Z += zBlockcenter;
                }

                SetGravity(grav);
                return this;
            }


            public void AddStation(Station station)
            {
                stations.Add(station);
                Vector3 positionInCorridor = Vector3.Transform(station.panel.Position - position, localRotationMatrix) * 2.5f;
                station.SetCorridor(this, positionInCorridor);
            }

            public void AddCorner(Corner corner)
            {
                corners.Add(corner);
                cornerCoordinates.Add(Vector3.Transform(corner.block.Position - position, localRotationMatrix).Z * 2.5f);
                corner.AddCorridor(this);
            }

            public float GetStoppingDistance(float vz, Vector3 compensateAcc)
            {
                return 0.5f * vz * vz / (GetMaxacceleration(2) * 0.8f - compensateAcc.Z * Math.Sign(vz));
            }

            float GetStoppingAccellerationPercentage(float vz, float dz, Vector3 compensateAcc)
            {
                float value = -0.5f * vz * vz / dz / (GetMaxacceleration(2) - compensateAcc.Z * Math.Sign(vz));
                return value;
            }

            public float GetMaxacceleration(int axis)
            {
                return numberOfGeneratorsInDirection[axis] * 9.81f;
            }

            void CheckOpenDoors()
            {
                if (nextWaypoint is Station)
                {
                    ((Station)nextWaypoint).OperateDoors(Station.DoorState.operation);
                }
            }

            public Vector3 GetTarget(Corridor offsetOrigin)
            {
                Vector3 target;
                if (nextWaypoint is Station)
                {
                    target = ((Station)nextWaypoint).positionInCorridor;
                }
                else
                {
                    Corridor nextCorridor = nextWaypoint.nextWaypoint as Corridor;

                    if (Base6Directions.GetAxis(nextCorridor.core.Orientation.Forward) == Base6Directions.GetAxis(core.Orientation.Forward))
                    {
                        target = nextCorridor.GetTarget(offsetOrigin);
                    }
                    else
                    {
                        target = new Vector3(0, 0, cornerCoordinates[targetCornerIndex]);
                    }
                }

                if(offsetOrigin != this)
                {
                    int offset = Vector3I.Dot(position - offsetOrigin.position,Base6Directions.GetIntVector(offsetOrigin.core.Orientation.Forward));
                    target.Z = offset * 2.5f + Vector3I.Dot(Base6Directions.GetIntVector(core.Orientation.Forward), Base6Directions.GetIntVector(offsetOrigin.core.Orientation.Forward)) * target.Z;
                }

                return target;
            }

            public override void FindPathRecursive()
            {
                base.FindPathRecursive();

                if (nextWaypoint is Corner)
                {
                    targetCornerIndex = corners.IndexOf((Corner)nextWaypoint);
                }

                if(stations.Contains(target))
                {
                    target.nextWaypoint = this;
                    target.FindPathRecursive();
                    return;
                }

                for(int i=0;i<corners.Count;i++)
                {
                    if (corners[i] != nextWaypoint && !corners[i].visited)
                    {
                        corners[i].nextWaypoint = this;
                        ToTest.Enqueue(corners[i]);
                    }
                }

                if (ToTest.Count > 0)
                {
                    ToTest.Dequeue().FindPathRecursive();
                }
            }
        }


    }
}
