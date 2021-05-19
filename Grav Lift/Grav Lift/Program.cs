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
        const float airlockCycleTime = 1f;

        const string groupName = "Grav Lift";

        Color cornerLightColor = new Color(0.7f, 0.8f, 1f);

        IMyBlockGroup blocks;

        IMyShipController reference;

        List<CorridorSystem> corridorSystems;

        List<CorridorSystem> activeCorridorSystems;

        Station[] stations;

        List<IMySensorBlock> sensors;
        List<IMyDoor> OpenDoors;

        const float fontSize = 1.7f;

        const int lines = (int)(17/fontSize);

        bool initAborted = false;

        List<MyDetectedEntityInfo> detectedPlayers = new List<MyDetectedEntityInfo>();
        List<MyDetectedEntityInfo> sensorContacts = new List<MyDetectedEntityInfo>();


        MatrixD worldToAnchorLocalMatrix;

        bool matrixUpdated;
        bool sensorsUpdated;

        IEnumerator<bool> _stateMachine;

        bool testMode = false;
       
        public enum TravelProgress { Idle, Launching, Travelling, Arriving, WaitForDoors }
        public Program()
        {
            Log.program = Station.program = Corridor.program = CorridorSystem.program = this;
            Setup();
        }

        void Setup()
        {
            if(_stateMachine !=null)
            {
                _stateMachine.Dispose();
            }
            _stateMachine = Init();
            Log.Clear();
            initAborted = false;
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }
        

        public bool RunInit()
        {
            if (_stateMachine == null)
            {
                return false;
            }

            if(initAborted)
            {
                Setup();
                return true;
            }

            bool hasMoreSteps = _stateMachine.MoveNext();

            if (initAborted)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return true;
            }

            if (hasMoreSteps)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                return true;
            }



            _stateMachine.Dispose();
            _stateMachine = null;

            Log.AppendLog("\nINIT COMPLETE");
            Log.clearAfter = true;
            return true;
        }

        public IEnumerator<bool> Init()
        {
            int initCounter = 0;

            corridorSystems = new List<CorridorSystem>();
            activeCorridorSystems = new List<CorridorSystem>();

            sensors = new List<IMySensorBlock>();
            OpenDoors = new List<IMyDoor>();

            testMode = Me.CustomData == "test";


            //Get Block Group
            blocks = GridTerminalSystem.GetBlockGroupWithName(groupName);

            if (blocks == null)
            {
                Log.AppendLog("NO BLOCKS IN GROUP " + groupName + " FOUND! Aborting.");
                initAborted = true;
                yield return false;
            }

            //Get Ship Controller as Reference

            List<IMyShipController> controllers = new List<IMyShipController>();
            blocks.GetBlocksOfType(controllers, x => x.CubeGrid == Me.CubeGrid);

            if (controllers.Count == 0)
            {
                Log.AppendLog("NO REMOTE OR COCKPIT FOUND! Aborting.");
                initAborted = true;
                yield return false;
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

            yield return true;

            //Get All Gravity Generators
            List<IMyGravityGenerator> gravityGenerators = new List<IMyGravityGenerator>();
            blocks.GetBlocksOfType(gravityGenerators, x => x.CubeGrid == Me.CubeGrid);

            ////////////////////////////////////////////////////////////////////////////////////////////////

            //Create Corridors

           Log.AppendLog("Finding corridor cores");

           List <Corridor> corridors = new List<Corridor>();
            foreach (IMyGravityGenerator g in gravityGenerators)
            {
                if (g.CustomName.Contains("core")|| g.CustomName.Contains("Core"))
                {
                    corridors.Add(new Corridor(g));
                }
            }

            if (corridors.Count == 0)
            {
                Log.AppendLog("\nNO CORRIDOR CORES FOUND. Aborting");
                initAborted = true;
                yield return false;
            }
            else
            {
                Log.AppendLog(corridors.Count.ToString() + " Corridors found with " + gravityGenerators.Count + " Generators in total");
            }
            yield return true;

            ////////////////////////////////////////////////////////////////////////////////////////////////

            Log.AppendLog("Adding all Gravity Generators to the closest Corridor\n");
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
                    initCounter++;
                }
                closest.AddGenerator(g); //add grav gen to corridor with closest core

                yield return true;
            }

            foreach (Corridor corridor in corridors)
            {
                corridor.SetGravity(Vector3.Zero);
                corridor.SetTestMode(testMode);
            }


            ////////////////////////////////////////////////////////////////////////////////////////////////

            Log.AppendLog("Assigning all stations to closest overlapping corridor");
            Log.AppendLog(stations.Length.ToString() + " Stations Found");

            foreach (Station s in stations)
            {
               float min = float.MaxValue;
                Corridor closest = null;

                foreach (Corridor corridor in corridors)  //find closest corridor corridor to current Station
                {
                    float d = corridor.GetSquareDistance(s.panel);
                    if (d < min)
                    {
                        min = d;
                        closest = corridor;
                    }
                    initCounter++;
                }
                if (closest == null)
                {
                    Log.AppendLog("\nSTATION '" + s.GetName() + "' IS NOT ATTACHED TO ANY CORRIDOR");
                }
                else
                {
                    closest.AddStation(s); //add station to closest corridor
                }
                yield return true;
            }


            ////////////////////////////////////////////////////////////////////////////////////////////////

            Log.AppendLog("\nAssigning screens and doors to the closest station\n");

            List<IMyTextPanel> screens = new List<IMyTextPanel>();

            blocks.GetBlocksOfType(screens, x => x.CubeGrid == Me.CubeGrid);

            foreach (IMyTextPanel screen in screens)
            {
                screen.FontSize = fontSize;
                screen.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;

                FindClosestStation(screen).AddScreen(screen);
                yield return true;
            }

            List<IMyDoor> doors = new List<IMyDoor>();

            blocks.GetBlocksOfType(doors, x => x.CubeGrid == Me.CubeGrid);

            foreach (IMyDoor door in doors)
            {
                if (door.CustomName.Contains("inner") || door.CustomName.Contains("Inner"))
                {
                    FindClosestStation(door).AddInnerDoor(door);
                }
                else if (door.CustomName.Contains("outer") || door.CustomName.Contains("Outer"))
                {
                    FindClosestStation(door).AddOuterDoor(door);
                }
                yield return true;
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////

            //Finding Corners and connecting corridors

            List<IMyTerminalBlock> corners = new List<IMyTerminalBlock>();
            blocks.GetBlocks(corners, x => x.CustomName.IndexOf("corner", StringComparison.OrdinalIgnoreCase) >= 0);

            Log.AppendLog("Connecting corridor systems");
            Log.AppendLog(corners.Count.ToString() + " Corners found");

            foreach (IMyTerminalBlock block in corners)
            {
                Corner corner = new Corner(block);

                if (block is IMyLightingBlock && cornerLightColor != Color.Black)
                {
                    ((IMyLightingBlock)block).Color = cornerLightColor;
                }
                ((IMyLightingBlock)block).Color = cornerLightColor;

                foreach (Corridor corridor in corridors)
                {
                    if(corridor.IsInCorridor(block))
                    {
                        corridor.AddCorner(corner);
                    }
                }
                switch(corner.corridors.Count)
                {
                    case 0: 
                        Log.AppendLog("\nCORNER '" + block.CustomName + "' IS NOT IN ANY CORRIDOR");
                        break;
                    case 1:
                        Log.AppendLog("\nCORNER '" + block.CustomName + "' IS ONLY IN CORRIDOR '" + corner.corridors[0].GetName() + "' AND DOESN'T CONNECT TO ANYTHING ELSE");
                        break;
                }
                initCounter++;
                yield return true;
            }

            while (corridors.Count > 0)
            {
                CorridorSystem c = Waypoint.CreateCorridorSystem(corridors[0], corridors);
                c.FinishSetup();
                corridorSystems.Add(c);
                Log.AppendLog("\nOne connected system with " + c.corridors.Count() + " corridors found");
            }

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


        public void Main(string argument, UpdateType updateSource)
        {
            Echo(DateTime.Now.ToString());
            if (RunInit())
            {
                Log.Tick();
                return;
            }

            matrixUpdated = false;
            sensorsUpdated = false;

            if (argument != "")
            {
                if(argument == "Setup")
                {
                    Setup();
                    return;
                }
                TargetSelection(argument);
            }

            int waitForDoorsCount = 0;

            for(int i= 0; i < activeCorridorSystems.Count; i++)
            {
                activeCorridorSystems[i].Tick();

                switch(activeCorridorSystems[i].travelProgress)
                {
                    case TravelProgress.WaitForDoors:
                        waitForDoorsCount++;
                        break;
                    case TravelProgress.Idle:
                        activeCorridorSystems.RemoveAt(i);
                        i--;
                        break;
                }
            }

            if (activeCorridorSystems.Count == 0)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;

                foreach (IMySensorBlock sensor in sensors)
                {
                    sensor.Enabled = false;
                }
            }
            else if (waitForDoorsCount == activeCorridorSystems.Count)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }

            Log.Tick();


        }
        void TargetSelection(string argument)
        {
            if (argument == "up" || argument == "u")
            {
                foreach (CorridorSystem c in corridorSystems)
                {
                    c.MoveMarker(-1);
                }
                return;
            }
            else if (argument == "down" || argument == "d")
            {
                foreach (CorridorSystem c in corridorSystems)
                {
                    c.MoveMarker(1);
                }
                return;
            }
            string s = argument;

            Station from = stations.FirstOrDefault(x => x.GetName() == s);

            if (from == null)
            {
                Log.AppendLog("Station '" + argument + "' not found");
                return;
            }

            if (from.parent == null)
            {
                Log.AppendLog("Station '" + argument + "' is not attached to a Corridor System");
                return;
            }

            if (from.parent.TryFindPath(from))
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update1;

                foreach (IMySensorBlock sensor in sensors)
                {
                    sensor.Enabled = true;
                }

                activeCorridorSystems.Add(from.parent);
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
            if (matrixUpdated) return;
            worldToAnchorLocalMatrix = Matrix.Transpose(Me.CubeGrid.WorldMatrix.GetOrientation());

            matrixUpdated = true;
        }

        public void GetDetectedPlayers ()
        {
            if (sensorsUpdated) return;

            sensorContacts.Clear();
            detectedPlayers.Clear();
            foreach (IMySensorBlock sensor in sensors)
            {
                sensor.DetectedEntities(sensorContacts);
                detectedPlayers.AddList(sensorContacts);
            }
            sensorsUpdated = true;
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