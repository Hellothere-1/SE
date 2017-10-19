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
        IMyTextPanel output;


        public Program()
        {
            List<IMyTerminalBlock> temp = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(temp);
            IMyRadioAntenna antenna = temp[0] as IMyRadioAntenna;
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(temp);
            output = temp[0] as IMyTextPanel;
            output.WritePublicText("");
            //TODO assign antenna to pb in script (even possible?)s
            if (Me.CustomName.Contains("Send"))
            {
                isSender = true;
                comHandler = new ComModule(this, antenna, "Sender");
                Me.CustomName = "PB_Sender";
            }
            else
            {
                GridTerminalSystem.GetBlocksOfType<IMyBeacon>(temp);
                myBeacon = temp[0] as IMyBeacon;
                comHandler = new ComModule(this, antenna, "Reciever");
                Me.CustomName = "PB_Reciever";
            }
        }


        public void Main(string argument)
        {
            if (argument != "")
            {
                output.WritePublicText("Input: " + argument + "\n", true);
            }
            if (argument.StartsWith("COM"))
            {
                string input = comHandler.ProcessMessage(argument);
                if (!isSender)
                {
                    myBeacon.CustomName = input;
                }
            }
            if (argument.StartsWith("Send"))
            {
                comHandler.SendMessage("Reciever", "SET ANTENNA ON");
                comHandler.SendMessage("Reciever", "SET BEACON ON");
                comHandler.SendMessage("Reciever", "LET THE BEACON ALONE");
            }
            if (argument == "HEY")
            {
                comHandler.SendHey();
            }
            comHandler.Run();
        }
    }
}   