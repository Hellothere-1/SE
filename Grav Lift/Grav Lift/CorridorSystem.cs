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
            List<Corridor> corridors = new List<Corridor>();
            List<Station> stations = new List<Station>();

            Waypoint active;
            Station target;
            Station origin;

            Program program;

            int selectedStation;

            public TravelProgress travelProgress = TravelProgress.Idle;

            MyDetectedEntityInfo activePlayer;

            public CorridorSystem(Program program)
            {
                this.program = program;
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

            public void finalize ()
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
                        travel();
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
                            if (activePlayer.EntityId == 0 || Vector3.Distance(activePlayer.Position, target.panel.GetPosition()) > 10)
                            {
                                target.OperateDoors(Station.DoorState.idle);
                                travelProgress = TravelProgress.Idle;
                            }
                        }
                        break;
                }
            }


            void travel()
            {
                program.Echo("blib");
                program.UpdateOrientationMatrix();

                GetActivePlayer();

                if (activePlayer.EntityId == 0)
                {
                    program.Echo("player lost");
                    terminateTravel();
                    return;
                }

                Vector3 pos = program.GetRelativePosition(activePlayer);
                Vector3 vel = program.GetRelativeVelocity(activePlayer);
                Vector3 compensateAcc = Base6Directions.GetVector(program.reference.Orientation.Up) * -9.81f;

                Waypoint next = active.tick(pos, vel, compensateAcc);
                if (active != next)
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

            public bool TryFindPath(Station from)
            {
                if(travelProgress!=TravelProgress.Idle && travelProgress!=TravelProgress.WaitForDoors)
                {
                    program.Echo("Corridor System is already active");
                    return false;
                }

                Station to = stations[selectedStation];
                if (Waypoint.FindPath(from, to))
                {
                    if (target != null)
                    {
                        target.OperateDoors(Station.DoorState.idle);
                    }
                    active = from.nextWaypoint;
                    target = to;
                    origin = from;

                    program.Echo("path found");
                    travelProgress = TravelProgress.Launching;
                    return true;
                }
                else
                {
                    program.Echo("no path found");
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
                program.Echo(selectedStation.ToString());

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
