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
        public class Station : Waypoint
        {
            public  enum DoorState { idle, operation, exit}

            public DoorState doorState = DoorState.exit;
            public IMyButtonPanel panel { get; private set; }
            public Corridor corridor { get; private set; }
            public Vector3 positionInCorridor { get; private set; }
            List<IMyTextPanel> screens = new List<IMyTextPanel>();
            List<IMyDoor> inners = new List<IMyDoor>();
            List<IMyDoor> outers = new List<IMyDoor>();
            bool doorsIdle = false;

            int closeTimer = 0;

            public CorridorSystem parent;

            public Station(IMyButtonPanel panel)
            {
                this.panel = panel;
            }

            public override Vector3I position => panel.Position;

            public string GetName()
            {
                return panel.CustomName;
            }

            public void SetCorridor(Corridor corridor, Vector3 positionInCorridor)
            {
                this.corridor = corridor;
                this.positionInCorridor = positionInCorridor;
            }

            public override Waypoint tick(Vector3 position, Vector3 velocity, Vector3 compensateAcc)
            {
                return nextWaypoint;
            }

            public void AddScreen(IMyTextPanel screen)
            {
                this.screens.Add(screen);
            }

            public void AddInnerDoor(IMyDoor door)
            {
                inners.Add(door);
            }

            public void AddOuterDoor(IMyDoor door)
            {
                outers.Add(door);
            }

            public bool OpenDoors(bool inner)
            {
                bool open = true;
                foreach(IMyDoor door in inner? inners : outers)
                {
                    door.Enabled = true;
                    door.OpenDoor();
                    if (door.OpenRatio < 0.9f)
                    {
                        open = false;
                    }
                }
                return open;
            }

            public bool CloseDoors(bool inner, bool secure = false)
            {
                bool closed = true;
                foreach (IMyDoor door in inner ? inners : outers)
                {
                    door.Enabled = true;
                    door.CloseDoor();
                    if (door.Status == DoorStatus.Closed)
                    {
                        if (inner || secure)
                        {
                            door.Enabled = false;
                        }
                    }
                    else
                    {
                        closed = false;
                        closeTimer = 0;
                    }
                }
                if(closed)
                {
                    closeTimer++;
                    return closeTimer > 4;
                }
                return false;
            }

            public bool OperateDoors(DoorState state)
            {
                if (doorState != state)
                {
                    doorsIdle = false;

                }
                doorState = state;
                return OperateDoors();
            }

            public bool OperateDoors()
            {
                if(doorsIdle)
                {
                    return true;
                }
                switch (doorState)
                {
                    case DoorState.operation:
                        if (CloseDoors(false, true))
                        {
                            if (OpenDoors(true))
                            {
                                doorsIdle = true;
                                return true;
                            }
                        }
                        return false;
                    case DoorState.exit:
                        if (CloseDoors(true, true))
                        {
                            if (OpenDoors(false))
                            {
                                doorsIdle = true;
                                return true;
                            }
                        }
                        return false;
                    default:
                        if( CloseDoors(true, true) && CloseDoors(false, false))
                        {
                            doorsIdle = true;
                            return true;
                        }
                        return false;
                }
            }

            public void SetText(string text)
            {
                foreach(IMyTextPanel screen in screens)
                {
                    screen.WriteText(GetName());
                    screen.WriteText(text,true);
                }
            }
        }
    }
}
