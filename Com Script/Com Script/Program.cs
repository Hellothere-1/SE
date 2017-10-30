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
        bool CHAT_MODE = true;
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



        public enum Request_Options { ACCEPT, DECLINE, DECLINE_ALL }

        ComModule comHandler;
        ChatHandler chathandler;
        
        IMyTerminalBlock output;
        IMyRadioAntenna antenna;
        bool outputIsTextPanel;
        bool isWorking = true;
        short chat_counter = 0;
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
            if (CHAT_MODE)
            {
                chathandler = new ChatHandler(this, CHAT_NAME);
            }
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

            if (argument.StartsWith("CHAT"))
            {
                chathandler.HandleArgument(argument);
            }

            if (argument.StartsWith("COM"))
            {
                string input = comHandler.ProcessMessage(argument);
                if (input != "")
                {
                    printOut(input);
                    if (input.StartsWith("Chat/"))
                    {
                        string sender = argument.Split('_')[4];
                        chathandler.HandleMessage(input, sender);
                    }
                }
            }

            if (argument.StartsWith("SEND"))
            {
                string[] parts = argument.Split('_');
                comHandler.SendMessage(parts[1], parts[2], false);
            }

            if (argument == "HEY")
            {
                comHandler.SendHey();
            }

            if (chat_counter >= 5)
            {
                chat_counter = 0;
                chathandler.run();
            }
            else
            {
                chat_counter++;
            }
            
            comHandler.Run();
        }

        public void SendMessage(string target, string message)
        {
            comHandler.SendMessage(target, message, true);
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
