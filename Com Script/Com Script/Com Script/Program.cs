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
        string key = "ZHE67-SAD35-HGDF9";
        ComModule comHandler;
        bool isSender = false;
        IMyBeacon myBeacon;

        public Program()
        {
            List<IMyTerminalBlock> temp = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(temp);
            IMyRadioAntenna antenna = temp[0] as IMyRadioAntenna;
            
            comHandler = new ComModule(this, antenna, "Sender");
            if (Me.CustomName.Contains("Send"))
            {
                isSender = true;
            }
            else
            {
                GridTerminalSystem.GetBlocksOfType<IMyBeacon>(temp);
                myBeacon = temp[0] as IMyBeacon;
            }
        }


        public void Main(string argument)
        {
            if (argument.StartsWith("COM"))
            {
                string input = comHandler.ProcessMessage(argument);
                if (!isSender)
                {
                    string inputkey = input.Split('_')[0];
                    if (inputkey == key)
                    {
                        myBeacon.CustomName = input.Split('_')[1];
                    }
                }
            }
            if (argument.StartsWith("Send"))
            {
                Me.CustomName = Me.CustomName + " |";
                comHandler.SendMessage("Reciever", "SET ANTENNA ON", key);
                Me.CustomName = Me.CustomName + " 0";
            }
            comHandler.Run();
        }
    }
}