﻿using Sandbox.Game.EntityComponents;
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
        public class GravityControl
        {
            LCDClass lcdHandler;
            Program parent;

            bool isOperating = false;

            IMyGravityGenerator[,] UpDown = new IMyGravityGenerator[2, 2];
            IMyGravityGenerator[,] Rota = new IMyGravityGenerator[2, 2];
            GeneratorsUni GGDis;
            GeneratorsUni GGR;
            GeneratorsUni GGL;
            GeneratorsUni GGUpDo;
            GeneratorsUni GGRot;
            GeneratorsDiv GGFr;
            GeneratorsDiv GGBa;

            int counter = 0;
            int phasetemp = 0;

            IMySensorBlock HangarSensor;
            IMySensorBlock BottomSensor;
            IMyShipController Reference;

            public GravityControl(LCDClass lcd, Program par)
            {
                lcdHandler = lcd;
                parent = par;
            }

            void setup()
            {
                try
                {
                    List<IMyGravityGenerator> Dis = new List<IMyGravityGenerator>();
                    parent.GridTerminalSystem.GetBlocksOfType(Dis, x => x.CustomName.Contains("Gravity Generator DIS"));
                    GGDis = new GeneratorsUni(Dis, 20, 15, 10);

                    List<IMyGravityGenerator> Re = new List<IMyGravityGenerator>();
                    parent.GridTerminalSystem.GetBlocksOfType(Re, x => x.CustomName.Contains("Gravity Generator re"));
                    GGR = new GeneratorsUni(Re, 30, 13.6f, 10);

                    List<IMyGravityGenerator> Le = new List<IMyGravityGenerator>();
                    parent.GridTerminalSystem.GetBlocksOfType(Le, x => x.CustomName.Contains("Gravity Generator le"));
                    GGL = new GeneratorsUni(Le, 30, 13.6f, 10);

                    UpDown[0, 0] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FR") as IMyGravityGenerator;
                    UpDown[0, 1] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FL") as IMyGravityGenerator;
                    UpDown[1, 0] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BR") as IMyGravityGenerator;
                    UpDown[1, 1] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BL") as IMyGravityGenerator;
                    List<IMyGravityGenerator> UpDo = new List<IMyGravityGenerator>();
                    foreach (IMyGravityGenerator gg in UpDown)
                        UpDo.Add(gg);
                    GGUpDo = new GeneratorsUni(UpDo, 10, 33, 7.5f);

                    Rota[0, 0] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator Rot Fr -") as IMyGravityGenerator;
                    Rota[0, 1] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator Rot Ba -") as IMyGravityGenerator;
                    Rota[1, 0] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator Rot Le +") as IMyGravityGenerator;
                    Rota[1, 1] = parent.GridTerminalSystem.GetBlockWithName("Gravity Generator Rot Re +") as IMyGravityGenerator;
                    List<IMyGravityGenerator> Rot = new List<IMyGravityGenerator>();
                    foreach (IMyGravityGenerator gg in Rota)
                        Rot.Add(gg);
                    GGRot = new GeneratorsUni(Rot, 0, 30, 14);

                    List<Gravity> Fr = new List<Gravity>();
                    List<Gravity> Ba = new List<Gravity>();
                    Fr.Add(new Gravity(parent.GridTerminalSystem.GetBlockWithName("Gravity Generator fr 1 +") as IMyGravityGenerator, 6, 16, 35));
                    Ba.Add(new Gravity(parent.GridTerminalSystem.GetBlockWithName("Gravity Generator ba 1 +") as IMyGravityGenerator, 6, 16, 35));
                    Fr.Add(new Gravity(parent.GridTerminalSystem.GetBlockWithName("Gravity Generator fr 2 -") as IMyGravityGenerator, 3, 17, 30));
                    Ba.Add(new Gravity(parent.GridTerminalSystem.GetBlockWithName("Gravity Generator ba 2 -") as IMyGravityGenerator, 3, 17, 30));
                    Fr.Add(new Gravity(parent.GridTerminalSystem.GetBlockWithName("Gravity Generator fr 3 +") as IMyGravityGenerator, 8, 11.8f, 30));
                    Ba.Add(new Gravity(parent.GridTerminalSystem.GetBlockWithName("Gravity Generator ba 3 +") as IMyGravityGenerator, 8, 11.8f, 30));

                    GGFr = new GeneratorsDiv(Fr);
                    GGBa = new GeneratorsDiv(Ba);



                    HangarSensor = parent.GridTerminalSystem.GetBlockWithName("Sensor Hangar") as IMySensorBlock;
                    BottomSensor = parent.GridTerminalSystem.GetBlockWithName("Bottom Sensor") as IMySensorBlock;

                    Reference = parent.GridTerminalSystem.GetBlockWithName("Hangar Reference") as IMyShipController;

                    GGFr.ResetDimensions();
                    GGBa.ResetDimensions();
                    isOperating = true;
                }
                catch (Exception)
                {
                    lcdHandler.logMessage("GGs could not be initiated, something is missing", Tags.GRA, Labels.cERR);
                    isOperating = false;
                }
            }

            public bool isOperational()
            {
                return isOperating;
            }


            public void Capture()
            {
                int phase = phasetemp;
                bool freedirection = true;

                var worldToAnchorLocalMatrix = Matrix.Transpose(Reference.WorldMatrix.GetOrientation());

                MyDetectedEntityInfo fighter;

                if (HangarSensor.IsActive || true)
                    fighter = HangarSensor.LastDetectedEntity;
                else
                {
                    fighter = BottomSensor.LastDetectedEntity;
                    phasetemp = 0;
                }

                Vector3D worldPositon = (fighter.Position - Reference.GetPosition());
                Vector3D position = Vector3D.Transform(worldPositon, worldToAnchorLocalMatrix) + new Vector3D(0, 10.2, 1.25);

                Vector3D worldVelocities = (fighter.Velocity - Reference.GetShipVelocities().LinearVelocity);
                Vector3D velocities = Vector3D.Transform(worldVelocities, worldToAnchorLocalMatrix);

                parent.Echo(Convert.ToString(velocities.X));
                parent.Echo(Convert.ToString(velocities.Y));
                parent.Echo(Convert.ToString(velocities.Z));
                parent.Echo(Convert.ToString(position.X));
                parent.Echo(Convert.ToString(position.Y));
                parent.Echo(Convert.ToString(position.Z));

                float front = Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Forward));
                float right = Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Right));
                float down = Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Down));

                switch (phase)
                {
                    case 0:


                        GGFr.SetGravity(-5 * Convert.ToSingle(position.Z + velocities.Z));
                        GGBa.SetGravity(5 * Convert.ToSingle(position.Z + velocities.Z));
                        GGR.SetGravity(5 * Convert.ToSingle(position.X + velocities.X));
                        GGL.SetGravity(-5 * Convert.ToSingle(position.X + velocities.X));

                        GGDis.SetGravity(-9.81f - 5 * Convert.ToSingle(velocities.Y));
                        GGUpDo.SetGravity(Convert.ToSingle(9.81f - 5 * velocities.Y));

                        if (Math.Abs(position.X) < 8 && Math.Abs(position.Z) < 10)
                        {
                            bool direction = freedirection ^ fighter.Orientation.Down.Dot(Reference.WorldMatrix.Down) < 0;

                            if ((direction && front < -0.99) || (!direction && front > 0.99))
                            {
                                phasetemp = 1;
                                GGRot.OnOff(false);
                                GGDis.OnOff(false);
                            }
                            else

                            {
                                if (direction ^ right > 0)
                                {
                                    Rota[0, 0].GravityAcceleration = -10;
                                    Rota[0, 1].GravityAcceleration = -10;
                                    Rota[1, 0].GravityAcceleration = 10;
                                    Rota[1, 1].GravityAcceleration = 10;
                                }
                                else
                                {
                                    Rota[0, 0].GravityAcceleration = 10;
                                    Rota[0, 1].GravityAcceleration = 10;
                                    Rota[1, 0].GravityAcceleration = -10;
                                    Rota[1, 1].GravityAcceleration = -10;
                                }

                                counter++;
                                if (counter >= 20)
                                {
                                    counter = 0;
                                    Vector3 field = new Vector3(17.5 + 2 * position.Z, 35, 14);
                                    Rota[0, 0].FieldSize = field;
                                    field.X = 17.5f - 2 * Convert.ToSingle(position.Z);
                                    Rota[0, 1].FieldSize = field;
                                    field.X = 10 + 2 * Convert.ToSingle(position.X);
                                    Rota[1, 0].FieldSize = field;
                                    field.X = 10 - 2 * Convert.ToSingle(position.X);
                                    Rota[1, 1].FieldSize = field;
                                }
                            }
                        }
                        else
                        {
                            GGRot.SetGravity(0);
                        }

                        break;

                    case 1:

                        GGR.SetGravity(9.81f + 5 * Convert.ToSingle(velocities.X));
                        GGL.SetGravity(9.81f - 5 * Convert.ToSingle(velocities.X));

                        float height;
                        if (down > 0.92)
                            height = 0;
                        else
                            height = 18f;

                        float upDoBase = Convert.ToSingle(-5 * (position.Y + height) - 7 * velocities.Y);


                        if (position.Y > -3)
                        {
                            float offset;
                            if (freedirection)
                                offset = -5.5f;
                            else
                                offset = 5.5f;

                            GGFr.SetGravity(-5 * Convert.ToSingle(position.Z - offset + velocities.Z));
                            GGBa.SetGravity(5 * Convert.ToSingle(position.Z - offset + velocities.Z));

                            GGUpDo.SetGravity(upDoBase + 9.81f / 4);

                        }
                        else
                        {
                            GGFr.SetGravity(9.81f - 5 * Convert.ToSingle(velocities.Z));
                            GGBa.SetGravity(9.81f + 5 * Convert.ToSingle(velocities.Z));

                            UpDown[0, 0].GravityAcceleration = upDoBase - 10 * (front + right);
                            UpDown[0, 1].GravityAcceleration = upDoBase - 10 * (front - right);
                            UpDown[1, 0].GravityAcceleration = upDoBase + 10 * (front - right);
                            UpDown[1, 1].GravityAcceleration = upDoBase + 10 * (front + right);
                        }


                        if (!HangarSensor.IsActive)
                        {
                            phasetemp = 0;
                            GGRot.OnOff(true);
                            GGDis.OnOff(true);
                            GGFr.ResetDimensions();
                            GGBa.ResetDimensions();
                        }
                        break;

                    default:
                        break;
                }
                /*
                GGFr.SetGravity(9.81f - 5 * Convert.ToSingle(velocities.Z));
                GGBa.SetGravity(9.81f + 5 * Convert.ToSingle(velocities.Z));
                GGR.SetGravity(9.81f + 5 * Convert.ToSingle(velocities.X));
                GGL.SetGravity(9.81f - 5 * Convert.ToSingle(velocities.X));

                if (velocities.Y > 2 || Math.Abs(position.X)> 2 || Math.Abs(position.Y+20) > 2 || Math.Abs(position.Z) > 2)
                {
                    GGDis.OnOff(true);
                    GGDis.SetGravity(-9.81f - 5 * Convert.ToSingle(velocities.Y));
                    GGUpDo.SetGravity(Convert.ToSingle(-10 * (position.Y + 20) - 5 *velocities.Y));
                }
                else
                {
                    GGDis.OnOff(false);
                    float upDoBase = Convert.ToSingle(-10*(position.Y + 20) - 5 * velocities.Y);
                    float front = 20 * Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Forward));
                    float right = 40 * Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Right));

                    UpDown[0, 0].GravityAcceleration = upDoBase - front - right;
                    UpDown[0, 1].GravityAcceleration = upDoBase - front + right;
                    UpDown[1, 0].GravityAcceleration = upDoBase + front - right;
                    UpDown[1, 1].GravityAcceleration = upDoBase + front + right;
                }
                */

            }
        }
    }
}
