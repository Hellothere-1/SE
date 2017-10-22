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
        //USER PART, define you custom values here
        //The name this com script should use, if empty, using grid name
        string OWN_NAME = "";
        //Defines if the Antenna should be always on
        bool ALWAYS_ON = false;
        //Defines if output Panels should be used as Chat windows
        bool CHAT_MODE = false;
        //Defines the antenna broadcasting range (0 - 50000)
        int RANGE = 50000;
        //Set custom antenna name which should be searched
        string ANTENNA_NAME = "RC_Antenna";
        //Set custom output name which should be searched
        string OUTPUT_NAME = "RC_Out";
        //Set custom chat window name which should be searched
        string CHAT_NAME = "RC_Chat";
        //Set handling of antenna (0 = all, 1 = own and ally, 2 = own), deafult 1
        short ACCEPT_MESSAGE = 1;
        //END OF USER PART, do not change anything under this line !!

        List<string> knownShips = new List<string>();


        ComModule comHandler;
        List<IMyTextPanel> chat_windows = new List<IMyTextPanel>();
        IMyTerminalBlock output;
        IMyRadioAntenna antenna;
        bool outputIsTextPanel;
        bool isWorking = true;
        //Eventuell sogar chatverlauf etc (Dafür rückmeldung von Sendecode nötig...)

        public Program()
        {
            antenna = GridTerminalSystem.GetBlockWithName(ANTENNA_NAME) as IMyRadioAntenna;
            if(antenna != null)
            {
                output = GridTerminalSystem.GetBlockWithName(OUTPUT_NAME);
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
                //TODO assign antenna to pb in script (even possible?)
                isWorking = true;
                antenna.SetValueFloat("Radius", RANGE);
                switch (ACCEPT_MESSAGE)
                {
                    case 0:
                        antenna.IgnoreOtherBroadcast = false;
                        antenna.IgnoreAlliedBroadcast = false;
                        break;
                    case 1:
                        antenna.IgnoreOtherBroadcast = true;
                        antenna.IgnoreAlliedBroadcast = false;
                        break;
                    case 2:
                        antenna.IgnoreOtherBroadcast = true;
                        antenna.IgnoreAlliedBroadcast = true;
                        break;
                    default:
                        antenna.IgnoreOtherBroadcast = true;
                        antenna.IgnoreAlliedBroadcast = false;
                        break;
                }
                if (ALWAYS_ON)
                {
                    antenna.SetValue("EnableBroadCast", true);
                }
                else
                {
                    antenna.SetValue("EnableBroadCast", false);
                }
            }
            else
            {
                Echo("Antenna could not be found");
                isWorking = false;
                return;
            }
            GridTerminalSystem.GetBlocksOfType(chat_windows, x => x.CustomName.Contains(CHAT_NAME));
            if (CHAT_MODE)
            {
                foreach (IMyTextPanel panel in chat_windows)
                {
                    panel.CustomData = "";
                    panel.WritePublicText("    Com Chat V1.0    \n\n");
                    panel.WritePublicText("Known Ships in Com Distance :\n", true);
                }
            }
            knownShips.Add("Ship 1");
            knownShips.Add("Ship 2");
            knownShips.Add("Ship 3");
            updateChatWindows();
            if (OWN_NAME == "")
            {
                OWN_NAME = Me.CubeGrid.CustomName;
            }
            OWN_NAME = OWN_NAME + "/" + Me.EntityId % 10000;
            comHandler = new ComModule(this, antenna, OWN_NAME, ALWAYS_ON);
            Me.CustomName = "PB-COM-" + OWN_NAME;
            Me.CustomData = "";
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
                if (input != "")
                {
                    printOut(input);
                }
            }
            if (argument.StartsWith("Send"))
            {
                string[] parts = argument.Split('_');
                comHandler.SendMessage(parts[1], parts[2], false);
            }
            if (argument == "HEY")
            {
                comHandler.SendHey();
            }
            comHandler.Run();
        }

        public void updateShipStatus(string name, bool delete = false)
        {
            if (delete && knownShips.Contains(name))
            {
                knownShips.Remove(name);
            }
            if (!delete)
            {
                knownShips.Add(name);
                updateChatWindows();
            }
        }

        void updateChatWindows()
        {
            foreach (IMyTextPanel panel in chat_windows)
            {
                if (panel.CustomData == "")
                {
                    panel.WritePublicText("    Com Chat V1.0    \n\n");
                    panel.WritePublicText("Known Ships in Com Distance : \n", true);
                    foreach (string name in knownShips)
                    {
                        panel.WritePublicText("    " + name + "\n", true);
                    }
                }
            }
        }

        public void printOut(string mes)
        {
            if (output == null)
            {
                Me.CustomData = Me.CustomData + mes + "\n";
            }
            else if (outputIsTextPanel)
            {
                IMyTextPanel panel = output as IMyTextPanel;
                panel.WritePublicText(mes + "\n", true);
            }
            else
            {
                output.CustomData = output.CustomData + mes + "\n";
            }
        }
    }
}
