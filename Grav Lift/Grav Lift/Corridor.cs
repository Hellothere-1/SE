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
            public static Program program;
            List<IMyGravityGenerator> grav = new List<IMyGravityGenerator>(); //List of Generators
            List<Base6Directions.Direction> gravDirections = new List<Base6Directions.Direction>(); //List of directions for the generators 
            int[] numberOfGeneratorsInDirection = new int[3] { 0, 0, 0 };

            Vector3I _position;
            Vector3 referenceOffset;

            public readonly Matrix localRotationMatrix;

            public List<Corner> corners { get; private set; } = new List<Corner>();
            List<Vector3> targetCoordinates = new List<Vector3>();

            int targetCornerIndex;

            static readonly Vector3 idleTargetPos = new Vector3(0.002f, 0.002f, 0.002f);
            Vector3 target = idleTargetPos;
            float localTarget = idleTargetPos.Z;

            static string[] properties = new string[7] { "corridorPos:", "front:", "back:", "left:", "right:", "up:", "down:" };
            static string[] commentary = new string[7] { "//How many blocks above this block does the center of the corridor lie?\n//(can be negative if below)", "\n//How many blocks in each of these directions does the corridor\n//need to extend from the center at minimum?", "", "", "", "", "" };
            static float[] baseValues = new float[7] { 1, 20, 20, 0, 0, 0, 0 };
            static float[] values = new float[7];

            Vector3 positiveExtend;
            Vector3 negativeExtend;


            public List<Station> stations { get; private set; } = new List<Station>();


            public Corridor(IMyGravityGenerator core)
            {
                this.core = core;


                CustomDataReader.ReadData(core, properties, commentary, baseValues, values);

                int offset = (int)values[0];
                positiveExtend = new Vector3(values[3], values[5], values[1]);
                negativeExtend = new Vector3(values[4], values[6], values[2]);

                SetGeneratorField(core, Vector3.Max(Vector3.Abs(positiveExtend), Vector3.Abs(negativeExtend)));

                _position = core.Position + offset * Base6Directions.GetIntVector(core.Orientation.Up);
                referenceOffset = (position - program.reference.Position) * 2.5f;

                core.Orientation.GetMatrix(out localRotationMatrix);
                localRotationMatrix = Matrix.Transpose(localRotationMatrix);

            }

            public string GetName()
            {
                return core.CustomName;
            }

            public override Vector3I position => _position;

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
                positionToCorridor = Vector3.Abs(Vector3.Transform(block.Position - _position, localRotationMatrix));
                return Math.Abs(positionToCorridor.X) + Math.Abs(positionToCorridor.Y);
            }

            public bool IsInCorridor(IMyCubeBlock block)
            {
                Vector3 positionToCorridor = Vector3.Abs(Vector3.Transform(block.Position - core.Position, localRotationMatrix)) * 5;
                return Math.Abs(positionToCorridor.X) <= core.FieldSize.X && Math.Abs(positionToCorridor.Y) <= core.FieldSize.Y && Math.Abs(positionToCorridor.Z) <= core.FieldSize.Z;
            }


            /// <summary>
            /// Adds Gravity Generator to list of Generators
            /// </summary>
            /// <param name="generator"></param>
            public void AddGenerator(IMyGravityGenerator generator)
            {
                grav.Add(generator);

                Matrix rotationMatrix;
                generator.Orientation.GetMatrix(out rotationMatrix);
                rotationMatrix = Matrix.Transpose(rotationMatrix);

                Vector3 relativeOffset = Vector3.Transform(position - generator.Position, rotationMatrix);

                Matrix coreMatrix;
                core.Orientation.GetMatrix(out coreMatrix);

                Vector3 posEx = Vector3.Transform(Vector3.Transform(positiveExtend, coreMatrix), rotationMatrix);
                Vector3 negEx = Vector3.Transform(Vector3.Transform(negativeExtend, coreMatrix), rotationMatrix);

                Vector3 extend = Vector3.Max(Vector3.Abs(relativeOffset + posEx), Vector3.Abs(relativeOffset - negEx));
                SetGeneratorField(generator, extend);


                Base6Directions.Direction direction = core.Orientation.TransformDirectionInverse(generator.Orientation.Up); //transforms the "Up" direction of the generator into the direction of the corridor coordinates
                gravDirections.Add(direction);
                numberOfGeneratorsInDirection[((int)Base6Directions.GetAxis(direction) + 2) % 3]++;

            }

            static void SetGeneratorField (IMyGravityGenerator gen, Vector3 blockExtend)
            {
                blockExtend = (blockExtend * 2 + Vector3.One) * 2.5f;

                gen.FieldSize = blockExtend;
            }

            public void SetGravity(Vector3 gravity)
            {
                if (numberOfGeneratorsInDirection[0] > 0)
                    gravity.X = Clamp(gravity.X / numberOfGeneratorsInDirection[0], -9.81f, 9.81f);
                if (numberOfGeneratorsInDirection[1] > 0)
                    gravity.Y = Clamp(gravity.Y / numberOfGeneratorsInDirection[1], -9.81f, 9.81f);
                if (numberOfGeneratorsInDirection[2] > 0)
                    gravity.Z = Clamp(gravity.Z / numberOfGeneratorsInDirection[2], -9.81f, 9.81f);

                for (int i = 0; i < grav.Count; i++)
                {
                    float acceleration = -gravity.Dot(Base6Directions.GetVector(gravDirections[i]));
                    grav[i].GravityAcceleration = acceleration;
                    grav[i].Enabled = acceleration != 0;
                }
            }

            public void SetTestMode (bool test)
            {
                foreach(IMyGravityGenerator g in grav)
                {
                    g.Enabled = test;
                    g.ShowOnHUD = test;
                }
            }

            public override Waypoint tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc)
            {
                //transforming inputs into local coordinates
                position = Vector3.Transform(position - referenceOffset, localRotationMatrix);
                velocity = Vector3.Transform(velocity, localRotationMatrix);
                compensateAcc = Vector3.Transform(compensateAcc, localRotationMatrix);

                Log.singleFrameMessages.Add("gens: " + numberOfGeneratorsInDirection[2]);

                //if (Math.Abs(position.Z) > core.FieldSize.Z / 2)
                //{
                //    target = idleTargetPos;
                //    SetGravity(Vector3.Zero); 
                //    return nextWaypoint;
                //}

                Vector3 grav;
                if (target == idleTargetPos)
                {
                    localTarget = GetTarget(this, out target);
                }
                CheckOpenDoors();

                if (localTarget != 0.001f && Math.Abs(position.Z - localTarget) < 1.5f && Math.Abs(position.X) < 1 && Math.Abs(position.Y) < 1)
                {
                    SetGravity(Vector3.Zero);
                    target = idleTargetPos;
                    return nextWaypoint;
                }

                if (Math.Abs(position.Z - target.Z) < 0.7f && Math.Abs(velocity.Z) < 3f) //near target on z axis
                {
                    if(Vector3.RectangularDistance(position, target) < 1.25f)
                    {
                        SetGravity(Vector3.Zero);
                        target = idleTargetPos;
                        return nextWaypoint;
                    }

                    grav = LimitVectorABS(target - position, 5) - velocity / 3 - compensateAcc;
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

            Vector3 LimitVectorABS(Vector3 v, float max)
            {
                v.X = Math.Min(Math.Max(v.X, -max), max);
                v.Y = Math.Min(Math.Max(v.Y, -max), max);
                v.Z = Math.Min(Math.Max(v.Z, -max), max);

                return v;
            }


            public void AddStation(Station station)
            {
                stations.Add(station);
                Vector3 positionInCorridor = Vector3.Transform(station.panel.Position - _position, localRotationMatrix) * 2.5f;
                station.SetCorridor(this, positionInCorridor);
            }

            public void AddCorner(Corner corner)
            {
                corners.Add(corner);
                targetCoordinates.Add(Vector3.Transform(corner.position - _position, localRotationMatrix) * 2.5f);
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

            public float GetTarget(Corridor offsetOrigin, out Vector3 target)
            {
                target = Vector3.Transform(nextWaypoint.position - offsetOrigin._position, offsetOrigin.localRotationMatrix) * 2.5f;
                float localTarget = target.Z;

                if (nextWaypoint is Corner)
                {
                    Corridor nextCorridor = (Corridor)nextWaypoint.nextWaypoint;
                    if (Base6Directions.GetAxis(nextCorridor.core.Orientation.Forward) == Base6Directions.GetAxis(core.Orientation.Forward))
                    {
                        nextCorridor.GetTarget(offsetOrigin, out target);
                        return localTarget;
                    }
                }

                return 0.001f;
            }

            public override bool FindPathRecursive()
            {
                base.FindPathRecursive();

                if (nextWaypoint is Corner)
                {
                    targetCornerIndex = corners.IndexOf((Corner)nextWaypoint);
                }

                if(stations.Contains(Waypoint.targetStation))
                {
                    Waypoint.targetStation.nextWaypoint = this;
                    Waypoint.targetStation.FindPathRecursive();
                    return true;
                }

                for (int i = 0; i < corners.Count; i++)
                {
                    if (corners[i] != nextWaypoint && !corners[i].visited)
                    {
                        corners[i].nextWaypoint = this;
                        ToTest.Enqueue(corners[i]);
                    }
                }

                return false;
            }
        }


    }
}
