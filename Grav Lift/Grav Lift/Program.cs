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
        List<IMyDoor> OpenDoors = new List<IMyDoor>();

        const float fontSize = 1.7f;

        int lines = (int)(17/fontSize);

        List<MyDetectedEntityInfo> detectedPlayers = new List<MyDetectedEntityInfo>();
        List<MyDetectedEntityInfo> sensorContacts = new List<MyDetectedEntityInfo>();
        MyDetectedEntityInfo activePlayer;


        MatrixD worldToAnchorLocalMatrix;

        IEnumerator<bool> _stateMachine;

        Waypoint active;
        Station target;
        Station origin;

        int selectedStation;

        enum TravelProgress {Idle, Launching, Travelling, Arriving, WaitForDoors}

        TravelProgress travelProgress = TravelProgress.Idle;
        
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
            foreach (IMySensorBlock sensor in sensors)
            {
                sensor.Enabled = false;
            }

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

            List<IMyTextPanel> screens = new List<IMyTextPanel>();

            blocks.GetBlocksOfType(screens, x => x.CubeGrid == Me.CubeGrid);

            foreach (IMyTextPanel screen in screens)
            {
                screen.FontSize = fontSize;

                FindClosestStation(screen).SetScreen(screen);
                yield return true;
            }

            List<IMyDoor> doors = new List<IMyDoor>();

            blocks.GetBlocksOfType(doors, x => x.CubeGrid == Me.CubeGrid);

            foreach (IMyDoor door in doors)
            {
                if (door.CustomName.Contains("inner"))
                {
                    FindClosestStation(door).AddInnerDoor(door);
                }
                else if (door.CustomName.Contains("outer"))
                {
                    FindClosestStation(door).AddOuterDoor(door);
                }
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
            UpdateScreens();

            bool doorsIdle = false;
            while(!doorsIdle)
            {
                doorsIdle = true;
                foreach(Station station in stations)
                {
                    doorsIdle &= station.OperateDoors(Station.DoorState.idle);
                }
                yield return true;
            }
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


        public Station FindClosestStation(IMyTerminalBlock block)
        {
            if (stations.Length == 0)
            {
                return null;
            }
            Station closest = stations[0];
            float min = float.MaxValue;

            foreach (Station s in stations)
            {
                float d = block.Position.RectangularDistance(s.panel.Position);
                if (d < min)
                {
                    min = d;
                    closest = s;
                }
            }
            return closest;
        
        }

        void TargetSelection(string argument)
        {
            if (argument == "up" || argument == "u") //
            {
                selectedStation--;
                UpdateScreens();
                return;
            }
            else if (argument == "down" || argument == "d")
            {
                selectedStation++;
                UpdateScreens();
                return;
            }
            string s = argument;

            Station from = stations.FirstOrDefault(x => x.GetName() == s);
            Station to = stations[selectedStation];

            if (from == null)
            {
                Echo("Station name not found");
                Echo(argument);
                return;
            }


            if (Waypoint.FindPath(from, to))
            {
                if(target!=null)
                {
                    target.OperateDoors(Station.DoorState.idle);
                }
                active = from.nextWaypoint;
                target = to;
                origin = from;
                foreach (IMySensorBlock sensor in sensors)
                {
                    sensor.Enabled = true;
                }
                Echo("path found");
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
                travelProgress = TravelProgress.Launching;
            }
            else
            {
                Echo("no path found");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (RunInit())
            {
                return;
            }

            switch (travelProgress)
            {
                case TravelProgress.Idle:
                    if (argument != "")
                    {
                        TargetSelection(argument);
                    }
                    else
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    break;
                case TravelProgress.Launching:
                    if (origin.OperateDoors(Station.DoorState.operation))
                    {
                        travelProgress = TravelProgress.Travelling;
                        SetActivePlayer();
                    }
                    break;
                case TravelProgress.Travelling:
                    travel();
                    break;

                case TravelProgress.Arriving:
                    if(target.OperateDoors(Station.DoorState.exit))
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update100;
                        travelProgress = TravelProgress.WaitForDoors;
                    }
                    break;
                case TravelProgress.WaitForDoors:
                    if (origin.OperateDoors(Station.DoorState.idle))
                        {
                        if (argument != "")
                        {
                            TargetSelection(argument);
                            return;
                        }
                        GetActivePlayer();
                        if (activePlayer.EntityId == 0 || Vector3.Distance(activePlayer.Position, target.panel.GetPosition()) > 10)
                        {
                            target.OperateDoors(Station.DoorState.idle);
                            travelProgress = TravelProgress.Idle;
                            foreach (IMySensorBlock sensor in sensors)
                            {
                                sensor.Enabled = false;
                            }
                            Runtime.UpdateFrequency = UpdateFrequency.None;
                        }
                    }
                    break;
            }
        }

        void travel()
        {
            Echo("blib");
            UpdateOrientationMatrix();

            GetActivePlayer();
            if (activePlayer.EntityId == 0)
            {
                Echo("player lost");
                terminateTravel();
                return;
            }

            Vector3 pos = GetRelativePosition(activePlayer);
            Vector3 vel = GetRelativeVelocity(activePlayer);
            Vector3 compensateAcc = Base6Directions.GetVector(reference.Orientation.Up) * -9.81f;

            Waypoint next = active.tick(pos, vel, compensateAcc);
            if(active!=next)
            {
                active = next;
                origin.OperateDoors(Station.DoorState.idle);
                if (active == null)
                {
                    terminateTravel();
                }
            }
        }

        void terminateTravel()
        {
            travelProgress = TravelProgress.Arriving;

            foreach (Corridor c in corridors)
            {
                c.SetGravity(Vector3.Zero);
            }
        }

        void UpdateScreens()
        {
            if (selectedStation >= stations.Length)
            {
                selectedStation = 0;
            }
            if (selectedStation < 0)
            {
                selectedStation = stations.Length - 1;
            }

            int linesOnscreen = Math.Min(lines - 3, stations.Length);

            StringBuilder sb = new StringBuilder(lines);

            int startIndex = Clamp(selectedStation - linesOnscreen / 2, 0, stations.Length - linesOnscreen);
            int endIndex = startIndex + linesOnscreen;

            sb.Append("\n================================================");

            for (int i = startIndex; i < endIndex; i++)
            {
                sb.Append('\n');
                sb.Append(i == selectedStation ? "> " : "   ");
                sb.Append(stations[i].GetName());
            }
            sb.Append("\n================================================");
            foreach (Station s in stations)
            {
                s.SetText(sb.ToString());
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

        public void GetDetectedPlayers ()
        {
            sensorContacts.Clear();
            detectedPlayers.Clear();
            foreach (IMySensorBlock sensor in sensors)
            {
                sensor.DetectedEntities(sensorContacts);
                detectedPlayers.AddList(sensorContacts);
            }
        }

        public void GetActivePlayer()
        {
            GetDetectedPlayers();
            activePlayer = detectedPlayers.FirstOrDefault(x=>x.EntityId==activePlayer.EntityId);
        }

        public void SetActivePlayer()
        {
            GetDetectedPlayers();
            double lowestDistance = double.MaxValue;
            foreach(MyDetectedEntityInfo player in detectedPlayers)
            {
                double d = Vector3.DistanceSquared(player.Position, origin.panel.GetPosition());
                if(d<lowestDistance)
                {
                    lowestDistance = d;
                    activePlayer = player;
                }
            }
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

    }
}