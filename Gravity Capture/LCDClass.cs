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
        public class LCDClass
        {
            Program parent;
            List<IMyTextPanel> LCDlist;
            bool logEnabled;

            public LCDClass(List<IMyTextPanel> lcds, Program par)
            {
                parent = par;
                LCDlist = lcds;
                logEnabled = lcds.Count() != 0 ? true : false;
                if (logEnabled)
                {
                    setUpDisplays();
                    logTextOnScreen("Output display found, logging activated\n", "INFO");
                }
                else
                {
                    parent.Echo("Output Panels missing, create a LCD/Text Panel with *Output* in the Name");
                }
            }


            //Intern method to prerpare the displays at startup
            void setUpDisplays()
            {
                foreach (IMyTextPanel lcd in LCDlist)
                {
                    lcd.WritePublicText("");
                    lcd.WritePublicText("Current State : " + State.Idle.ToString() + "\n");
                    lcd.WritePublicText("Current Status : Waiting\n", true);
                    lcd.WritePublicText("Status Hangar Doors : Closed\n\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                }
            }
            //-----------------------------------------------------------------
            

            //Intern Method to find the right point to insert log messages
            int findEndOfLogHead(string currentText)
            {
                int index = currentText.IndexOf("Recent Updates");
                index = currentText.IndexOf("\n", index) + 1;
                return index;
            }
            //-----------------------------------------------------------------


            //When called, logs a message mit the correct label on the screens
            public void logMessage(string message, string label = "INFO")
            {
                if (logEnabled)
                {
                    message = message + "\n";
                    logTextOnScreen(message, label);
                }
                else
                {
                    parent.Echo(message);
                }
            }
            //------------------------------------------------------------------


            //Intern method to print a log message on all LCDs with correct label (not implemented by now)
            void logTextOnScreen(string message, string label)
            {
                foreach (IMyTextPanel lcd in LCDlist)
                {
                    string currentText = lcd.GetPublicText();
                    int index = findEndOfLogHead(currentText);
                    currentText = currentText.Insert(index, label + ": " + message);
                    lcd.WritePublicText(currentText);
                }
            }
            //------------------------------------------------------------------------------------------


            //When called, logs the current state in the Head of the Display-----------------------------
            public void logHeadOnScreen(State currentState, bool running, bool hangarsOpen)
            {
                string hangardoors = hangarsOpen ? "Open" : "Closed";
                string status = running ? "Running" : "Waiting";
                foreach (IMyTextPanel lcd in LCDlist)
                {
                    string currentText = lcd.GetPublicText();
                    string updateText = "";
                    int index = findEndOfLogHead(currentText);
                    updateText = currentText.Substring(index);
                    lcd.WritePublicText("Current State : " + currentState.ToString() + "\n");
                    lcd.WritePublicText("Current Status : " + status + "\n", true);
                    lcd.WritePublicText("Status Hangar Doors : " + hangardoors + "\n\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    lcd.WritePublicText(updateText, true);
                }
            }
            //------------------------------------------------------------------------------------------

        }
    }
}
