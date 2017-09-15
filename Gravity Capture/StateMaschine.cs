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
        enum State { Idle, CatchShip, ShipInHangar, Eject };

        [Flags]
        //Setting Flags as Instructions for multiple Data in one
        enum Instructions
        {   ResetFieldValue     = 0x01,
            SetFieldValue       = 0x02,
            PowerOn             = 0x04,
            PowerOff            = 0x08,
            SetStrength         = 0x16,
            SetDynamicStrength  = 0x32,
            NegateStrength      = 0x64
        };


        public class StateMaschine
        {
            State currentState;
            State[] nextState;
            Instructions[] instructions;
            
            Object[] InstructionData;
        }

        //TODO dynamic size of state_table
        public StateMaschine[] state_table = new StateMaschine[4];

        public void FillStateTableEntry(StateMaschine entry)
        {

        }

    }
}
