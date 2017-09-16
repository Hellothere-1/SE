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
        public enum State { None = 0, Idle, CatchShip, ShipInHangar, Eject, CraftDetected, CraftCatched };
        public enum Conditions { None, Time, ExternalTrigger };

        //Setting Flags as Instructions for multiple Data in one
        [Flags]
        public enum Instructions
        {
            None                = 0x00,
            ResetFieldValue     = 0x01,
            SetFieldValue       = 0x02,
            PowerOn             = 0x04,
            PowerOff            = 0x08,
            SetStrength         = 0x16,
            SetDynamicStrength  = 0x32,
            NegateStrength      = 0x64
        };

        public class StateMaschine
        {
            public State currentState;
            public State nextState;
            public Instructions[] instructions;
            public Conditions conditionForNextState;
            public Object[] InstructionData;
            public float ConditionTime;
        }

        public void FillStateTableEntry(StateMaschine entry, State current, State next, Instructions[] instr, Conditions conds, Object[] data, float time)
        {
            entry.currentState = current;
            entry.nextState = next;
            entry.instructions = instr;
            entry.conditionForNextState = conds;
            entry.InstructionData = data;
            entry.ConditionTime = time;
        }

        public void FillStateTable(StateMaschine[] state_table)
        {   //TODO extend for all states 
            FillStateTableEntry(state_table[0], State.Idle, State.None, null, Conditions.ExternalTrigger, null, 0);
            FillStateTableEntry(state_table[1], State.CatchShip, State.None, null, Conditions.ExternalTrigger, null, 0);
            FillStateTableEntry(state_table[2], State.ShipInHangar, State.None, null, Conditions.ExternalTrigger, null, 0);
            FillStateTableEntry(state_table[3], State.Eject, State.Idle, null, Conditions.Time, null, 4);
            FillStateTableEntry(state_table[4], State.CraftDetected, State.CraftCatched, null, Conditions.Time, null, 6);
            FillStateTableEntry(state_table[5], State.CraftCatched, State.None, null, Conditions.ExternalTrigger, null, 0);
        }

        public StateMaschine[] CreateStateMaschine(IMyTextPanel debugPanel)
        {
            debugPanel.WritePublicText("Starting Setup \n", false);
            StateMaschine[] stateMaschine = new StateMaschine[6];
            for (int i = 0; i < 4; i++)
            {
                stateMaschine[i] = new StateMaschine();
            }
            debugPanel.WritePublicText("Created State Maschine\n", true);
            if (stateMaschine[0] == null)
            {
                debugPanel.WritePublicText("Is empty \n", true);
                return null;
            }
            FillStateTable(stateMaschine);
            debugPanel.WritePublicText("Filled State Maschine \n", true);
            return stateMaschine;
        }
    }
}
