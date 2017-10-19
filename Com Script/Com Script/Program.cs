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
    partial class Program : MyGridProgram
    {
        //USER PART, define you custom values here
        //The name this com script should use, if empty, using grid name
        string OWN_NAME = "";
        //Defines if the Antenna should be always on
        bool ALWAYS_ON = false;
        //Defines the antenna broadcasting range (0 - 50000)
        int RANGE = 50000;
        //Set custom antenna name which should be searched
        string ANTENNA_NAME = "RC_Antenna";
        //Set custom output name which should be searched
        string OUTPUT_NAME = "RC_Out";
        //END OF USER PART, do not change anything under this line !!


        ComModule comHandler;
        IMyTerminalBlock output;
        IMyRadioAntenna antenna;
        bool outputIsTextPanel;
        bool isWorking = true;
        //IDEE textpanel benutzen um mögliche kandidaten für nachrichten auszuwählen, dann achricht tippen und dann mit enter senden
        //Eventuell sogar chatverlauf etc (Dafür rückmeldung von Sendecode nötig...)

        public Program()
        {
            antenna = GridTerminalSystem.GetBlockWithName(ANTENNA_NAME) as IMyRadioAntenna;
            if(antenna != null)
            {
                //TODO assign antenna to pb in script (even possible?)
                isWorking = true;
                antenna.SetValueFloat("Radius", RANGE);
                if (ALWAYS_ON)
                {
                    antenna.SetValueBool("Broadcasting", true);
                }
                else
                {
                    antenna.SetValueBool("Broadcasting", false);
                }
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
            if (OWN_NAME == "")
            {
                OWN_NAME = Me.CubeGrid.CustomName;
            }
            comHandler = new ComModule(this, antenna, OWN_NAME);
            Me.CustomName = "PB_COM _" + OWN_NAME;
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
            if (argument == "HEY")
            {
                //Send Hey message through network
            }
            comHandler.Run();
        }
    }
}