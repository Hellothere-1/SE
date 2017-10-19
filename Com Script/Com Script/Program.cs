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
        string ownName = "";
        string key = "ZHE67-SAD35-HGDF9";
        ComModule comHandler;
        IMyTerminalBlock output;
        IMyRadioAntenna antenna;
        bool outputIsTextPanel;
        bool isWorking = true;

        public Program()
        {
            antenna = GridTerminalSystem.GetBlockWithName("RC_Antenna") as IMyRadioAntenna;
            if(antenna != null)
            {
                //TODO assign antenna to pb in script (even possible?)
                isWorking = true;
            }
            else
            {
                Echo("Antenna could not be found");
                isWorking = false;
                return;
            }
            output = GridTerminalSystem.GetBlockWithName("RC_Out");

            try
            {
                IMyTextPanel textPanel = output as IMyTextPanel;
                textPanel.WritePublicText("");
                outputIsTextPanel = true;
            }
            catch (Exception)
            {
                outputIsTextPanel = false;
            }

            if (key == "")
            {
                generateKey();
            }

            if (ownName == "")
            {
                ownName = Me.CubeGrid.CustomName;
            }
            comHandler = new ComModule(this, antenna, ownName);
            Me.CustomName = "PB_COM _" + ownName;
        }


        void generateKey()
        {
            Random rnd = new Random(Me.CubeGrid.CustomName.GetHashCode());
            for (int i = 1; i < 16; i++)
            {
                char nextLetter = (char) (rnd.Next(0, 35) + 48);
                if (nextLetter > 57)
                {
                    nextLetter += (char) 7;
                }
                key = key + nextLetter;
                if (i % 5 == 0 && i < 12)
                {
                    key = key + "_";
                }
            }
        }


        public void Main(string argument)
        {
            if (!isWorking)
            {
                return;
            }
            if (argument.StartsWith("COM"))
            {
                string input = comHandler.ProcessMessage(argument);
                if (output == null)
                {
                    Me.CustomData = Me.CustomData + input + "\n";
                }
                else if (outputIsTextPanel)
                {
                    IMyTextPanel panel = output as IMyTextPanel;
                    panel.WritePublicText(input + "\n", true);
                }
                else
                {
                    output.CustomData = output.CustomData + input + "\n";
                }
            }
            if (argument.StartsWith("Send"))
            {
                string[] parts = argument.Split('_');
                comHandler.SendMessage(parts[1], parts[2]);
            }
            comHandler.Run();
        }
    }
}