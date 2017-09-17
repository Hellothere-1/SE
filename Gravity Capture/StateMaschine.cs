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
        public enum State { None = 0, Idle, OpenHangar, LaunchShip, CaptureShip};

        public class StateMaschine
        {
            public State currentState;
            public State nextState;
            public bool waitForTrigger;
        }

        public void FillStateTableEntry(StateMaschine entry, State current, State next, bool wait)
        {
            entry.currentState = current;
            entry.nextState = next;
            entry.waitForTrigger = wait;
        }

        public void FillStateTable(StateMaschine[] state_table)
        {   //TODO extend for all states 
            FillStateTableEntry(state_table[0], State.Idle, State.OpenHangar, true);
            FillStateTableEntry(state_table[1], State.OpenHangar, State.None, false);
            FillStateTableEntry(state_table[2], State.LaunchShip, State.OpenHangar, true);
            FillStateTableEntry(state_table[3], State.CaptureShip, State.OpenHangar, true);
        }

        public StateMaschine[] CreateStateMaschine(IMyTextPanel debugPanel)
        {
            int lengthStateTable = Enum.GetNames(typeof(State)).Length;
            StateMaschine[] stateMaschine = new StateMaschine[lengthStateTable];
            for (int i = 0; i < lengthStateTable; i++)
            {
                stateMaschine[i] = new StateMaschine();
            }
            FillStateTable(stateMaschine);
            return stateMaschine;
        }
    }
}
