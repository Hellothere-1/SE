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
            public IMyButtonPanel panel { get; private set; }
            public Corridor corridor { get; private set; }
            public Vector3 positionInCorridor { get; private set; }
            IMyTextPanel screen;

            public Station(IMyButtonPanel panel)
            {
                this.panel = panel;
            }

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

            public void SetScreen(IMyTextPanel screen)
            {
                this.screen = screen;
            }

            public void SetText(string text)
            {
                screen.WriteText(text);
            }
        }
    }
}
