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
    partial class Program : MyGridProgram
    {
        IMyBlockGroup blocks;

        IMyShipController reference;

        Corridor[] corridors;
        Station[] stations;

        List<IMySensorBlock> sensors = new List<IMySensorBlock>();


        MatrixD worldToAnchorLocalMatrix;

        IEnumerator<bool> _stateMachine;
        
        public Program()
        {
            _stateMachine = Init();
            //Get Block Group
            blocks = GridTerminalSystem.GetBlockGroupWithName("Grav Lift");

            //Get Ship Controller as Reference

            List<IMyShipController> controllers = new List<IMyShipController>();
            blocks.GetBlocksOfType(controllers, x => x.CubeGrid == Me.CubeGrid);

            if(controllers.Count == 0)
            {
                Echo("No Remote Control or Cockpit found");
                return;
            }
            reference = controllers[0];

            //Get sensors
            blocks.GetBlocksOfType(sensors, x => x.CubeGrid == Me.CubeGrid);

            //Create a Station for each button Panel
            List<IMyButtonPanel> panels = new List<IMyButtonPanel>();
            blocks.GetBlocksOfType(panels, x => x.CubeGrid == Me.CubeGrid);
            stations = new Station[panels.Count];
            for (int i = 0; i < panels.Count; i++)
            {
                stations[i] = new Station(panels[i]);
            }
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public bool RunInit()
        {
            if (_stateMachine == null)
            {
                return false;
            }

            bool hasMoreSteps = _stateMachine.MoveNext();

            if (hasMoreSteps)
            {
                Echo("initializing");
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                return true;
            }

            _stateMachine.Dispose();
            _stateMachine = null;

            foreach (Corridor c in corridors)
            {
                c.SetGravity(Vector3.Zero);
            }

            Echo("Init complete");
            return true;
        }

        public IEnumerator<bool> Init()
        {
            Echo("mark");
            int initCounter = 0;


            //Get All Gravity Generators
            List<IMyGravityGenerator> gravityGenerators = new List<IMyGravityGenerator>();
            blocks.GetBlocksOfType(gravityGenerators, x => x.CubeGrid == Me.CubeGrid);

            //Create Corridors

            List<IMyGravityGenerator> cores = new List<IMyGravityGenerator>();

            foreach (IMyGravityGenerator g in gravityGenerators)
            {
                if (g.CustomName.Contains("core"))
                {
                    cores.Add(g);
                }
            }


            corridors = new Corridor[cores.Count];
            for (int i = 0; i < cores.Count; i++)
            {
                corridors[i] = new Corridor(cores[i], this);
            }
            if (corridors.Length == 0)
            {
                Echo("No Corridors found");
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            yield return true;

            foreach (IMyGravityGenerator g in gravityGenerators)
            {
                int min = int.MaxValue;
                Corridor closest = corridors[0];

                foreach (Corridor corridor in corridors)  //find closest corridor core to current grav gen
                {
                    int d = corridor.GetRectDistance(g);
                    if (d < min)
                    {
                        min = d;
                        closest = corridor;
                    }
                    Echo(initCounter++.ToString());
                }
                closest.AddGenerator(g); //add grav gen to corridor with closest core

                yield return true;
            }

            foreach (Station s in stations)
            {
               float min = float.MaxValue;
                Corridor closest = corridors[0];

                foreach (Corridor corridor in corridors)  //find closest corridor corridor to current Station
                {
                    float d = corridor.GetSquareDistance(s.panel);
                    if (d < min)
                    {
                        min = d;
                        closest = corridor;
                    }
                    Echo(initCounter++.ToString());
                }
                closest.AddStation(s); //add station to closest corridor
                yield return true;
            }

            List<IMyTerminalBlock> corners = new List<IMyTerminalBlock>();
            blocks.GetBlocks(corners, x => x.CustomName.Contains("corner"));

            foreach(IMyTerminalBlock block in corners)
            {
                Corner corner = new Corner(block);

                foreach(Corridor corridor in corridors)
                {
                    if(corridor.IsInCorridor(block))
                    {
                        corridor.AddCorner(corner);
                    }
                }
                Echo(initCounter++.ToString());
                yield return true;
            }

            Array.Sort(stations, (x, y) => String.Compare(x.GetName(), y.GetName()));
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(RunInit())
            {
                return;
            }

            if (argument == "toggle")
            {
                if (Runtime.UpdateFrequency == UpdateFrequency.None)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                }
                else
                {
                    foreach (Corridor c in corridors)
                    {
                        c.SetGravity(Vector3.Zero);
                    }
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }
            }

            UpdateOrientationMatrix();

            List<MyDetectedEntityInfo> players = GetDetectedPlayers();

            if(players.Count == 0)
            {
                return;
            }

            Vector3 pos = GetRelativePosition(players[0]);
            Vector3 vel = GetRelativeVelocity(players[0]);
            Vector3 compensateAcc = Base6Directions.GetVector(reference.Orientation.Up)*9.81f;

            foreach (Station s in stations)
            {
                Echo(s.positionInCorridor.ToString());
            }

            foreach (Corridor c in corridors)
            {
                c.tick(pos, vel, compensateAcc,stations[0].positionInCorridor);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////                                          

        public Vector3D GetShipVelocity(IMyShipController dataBlock)
        {
            var worldLocalVelocities = dataBlock.GetShipVelocities().LinearVelocity;
            var worldToAnchorLocalMatrix = Matrix.Transpose(dataBlock.WorldMatrix.GetOrientation());
            return Vector3D.Transform(worldLocalVelocities, worldToAnchorLocalMatrix);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////   

        public Vector3D GetShipAngularVelocity(IMyShipController dataBlock)
        {
            var worldLocalVelocities = dataBlock.GetShipVelocities().AngularVelocity;
            return Vector3D.Transform(worldLocalVelocities, worldToAnchorLocalMatrix);
        }

        public Vector3D GetRelativeVelocity(MyDetectedEntityInfo entity)
        {
            Vector3 worldVelocity = entity.Velocity - reference.GetShipVelocities().LinearVelocity;
            return Vector3D.Transform(worldVelocity, worldToAnchorLocalMatrix);
        }

        public Vector3D GetRelativePosition(MyDetectedEntityInfo entity)
        {
            Vector3 worldPos = entity.Position - reference.GetPosition() + entity.Orientation.Down * 0.2f;
            return Vector3D.Transform(worldPos, worldToAnchorLocalMatrix);
        }

        public void UpdateOrientationMatrix()
        {
            worldToAnchorLocalMatrix = Matrix.Transpose(Me.CubeGrid.WorldMatrix.GetOrientation());
        }

        public List<MyDetectedEntityInfo> GetDetectedPlayers ()
        {
            List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();

            foreach (IMySensorBlock sensor in sensors)
            {
                sensor.DetectedEntities(entities);
            }
            return entities;
        }


    }
}