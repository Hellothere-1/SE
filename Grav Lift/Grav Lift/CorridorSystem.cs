using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class CorridorSystem
        {
            public List<Corridor> corridors = new List<Corridor>();
            List<Station> stations = new List<Station>();

            Waypoint active;
            Station target;
            Station origin;

            public static Program program;

            int selectedStation;

            public TravelProgress travelProgress = TravelProgress.Idle;

            MyDetectedEntityInfo activePlayer;

            public CorridorSystem()
            {

            }

            public void AddCorridor(Corridor corridor)
            {
                corridors.Add(corridor);
                foreach (Station station in corridor.stations)
                {
                    stations.Add(station);
                    station.parent = this;
                }
            }

            public void MoveMarker(int dir)
            {
                selectedStation += dir;
                UpdateScreens();
            }

            public void FinishSetup ()
            {
                stations.Sort((x, y) => String.Compare(x.GetName(), y.GetName()));
                UpdateScreens();
            }

            public void Tick()
            {
                switch (travelProgress)
                {
                    case TravelProgress.Launching:
                        if (origin.OperateDoors(Station.DoorState.operation))
                        {
                            travelProgress = TravelProgress.Travelling;
                            SetActivePlayer();
                        }
                        break;
                    case TravelProgress.Travelling:
                        Travel();
                        break;

                    case TravelProgress.Arriving:
                        if (target.OperateDoors(Station.DoorState.exit))
                        {
                            travelProgress = TravelProgress.WaitForDoors;
                        }
                        break;
                    case TravelProgress.WaitForDoors:
                        if (origin.OperateDoors(Station.DoorState.idle))
                        {
                            GetActivePlayer();
                            if (activePlayer.EntityId == 0 || !target.hasOuterDoors || Vector3.Distance(activePlayer.Position, target.panel.GetPosition()) > 3)
                            {
                                target.OperateDoors(Station.DoorState.idle);
                                travelProgress = TravelProgress.Idle;
                            }
                        }
                        break;
                }
            }


            void Travel()
            {
                program.UpdateOrientationMatrix();

                GetActivePlayer();

                if (activePlayer.EntityId == 0)
                {
                    Log.AppendLog("PLAYER EXITED SENSOR AREA");
                    TerminateTravel();
                    return;
                }

                Vector3 pos = program.GetRelativePosition(activePlayer);
                Vector3 vel = program.GetRelativeVelocity(activePlayer);
                Vector3 compensateAcc = Base6Directions.GetVector(program.reference.Orientation.Up) * -9.81f;

                Waypoint next = active.tick(pos, vel, compensateAcc);


                if (active != next)
                {
                    //if(next is Corner)
                    //{
                    //    next = next.nextWaypoint;
                    //}
                    active = next;
                    origin.OperateDoors(Station.DoorState.idle);
                    if (active == null)
                    {
                        TerminateTravel();
                    }
                }
            }

            void TerminateTravel()
            {
                travelProgress = TravelProgress.Arriving;

                foreach (Corridor c in corridors)
                {
                    c.SetGravity(Vector3.Zero);
                }
            }

            public bool TryFindPath(Station from)
            {
                if (travelProgress != TravelProgress.Idle && travelProgress != TravelProgress.WaitForDoors)
                {
                    Log.AppendLog("Corridor System is already active");
                    return false;
                }

                Station to = stations[selectedStation];
                Log.AppendLog("Trying to find Path from " + from.GetName() + " to " + to.GetName());

                if (Waypoint.FindPath(from, to))
                {
                    if (target != null)
                    {
                        target.OperateDoors(Station.DoorState.idle);
                    }
                    active = from.nextWaypoint;
                    target = to;
                    origin = from;

                    Log.AppendLog("Path found");
                    travelProgress = TravelProgress.Launching;
                    return true;
                }
                else
                {
                    Log.AppendLog("No path found");
                    return false;
                }
            }

            void UpdateScreens()
            {
                if (selectedStation >= stations.Count)
                {
                    selectedStation = 0;
                }
                if (selectedStation < 0)
                {
                    selectedStation = stations.Count - 1;
                }

                int linesOnscreen = Math.Min(lines - 3, stations.Count);

                StringBuilder sb = new StringBuilder(lines);

                int startIndex = Clamp(selectedStation - linesOnscreen / 2, 0, stations.Count - linesOnscreen);
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

            public void GetActivePlayer()
            {
                program.GetDetectedPlayers();
                activePlayer = program.detectedPlayers.FirstOrDefault(x => x.EntityId == activePlayer.EntityId);
            }

            public void SetActivePlayer()
            {
                program.GetDetectedPlayers();
                double lowestDistance = double.MaxValue;
                foreach (MyDetectedEntityInfo player in program.detectedPlayers)
                {
                    double d = Vector3.DistanceSquared(player.Position, origin.panel.GetPosition());
                    if (d < lowestDistance)
                    {
                        lowestDistance = d;
                        activePlayer = player;
                    }
                }
            }
        }
    }
}
