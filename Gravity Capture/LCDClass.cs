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
        public enum Labels {BOOTUP, ERROR, WARNING, STATE, INFO, DEBUG };
        
        public class LCDClass
        {
            

            Program parent;
            List<IMyTextPanel> LCDlist;
            int[] LCDlabels;
            bool logEnabled;

            public LCDClass(List<IMyTextPanel> lcds, Program par)
            {
                parent = par;
                LCDlist = lcds;
                logEnabled = lcds.Count() != 0 ? true : false;
                if (logEnabled)
                {
                    LCDlabels = new int[LCDlist.Count];
                    setUpDisplays();
                    logTextOnScreen("Output display found, logging activated\n", Labels.BOOTUP);
                }
                else
                {
                    parent.Echo("Output Panels missing, create a LCD/Text Panel with *Output* in the Name");
                }
            }


            //Intern method to prerpare the displays at startup
            void setUpDisplays()
            {
                int counter = 0;
                foreach (IMyTextPanel lcd in LCDlist)
                {
                    lcd.WritePublicText("Current State : " + State.Idle.ToString() + "\n");
                    lcd.WritePublicText("Current Status : Waiting\n", true);
                    lcd.WritePublicText("Status Hangar Doors : Closed\n\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    logMessage("Booting up ... please stand by", Labels.BOOTUP, lcd);
                    findLabelOfLCD(counter, lcd);
                    counter++;
                }
            }
            //-----------------------------------------------------------------


            void findLabelOfLCD(int lcdID, IMyTextPanel lcd)
            {
                string label = lcd.CustomData;
                LCDlabels[lcdID] = 3;
                try
                {
                    if (label == "")
                    {
                        LCDlabels[lcdID] = (int)Labels.INFO;
                        logMessage(lcd.CustomName + " is now tagged with label INFO", Labels.BOOTUP, lcd);
                        return;
                    }
                    try
                    {
                        Labels lcdLabel = (Labels)Enum.Parse(typeof(Labels), label);
                        LCDlabels[lcdID] = (int)lcdLabel;
                        logMessage(lcd.CustomName + " is now tagged with label " + lcdLabel, Labels.BOOTUP, lcd);
                    }
                    catch (ArgumentException)
                    {
                        logMessage(lcd.CustomName + " does not declare its label correct", Labels.WARNING);
                        logMessage(lcd.CustomName + " will now be declared with label INFO", Labels.WARNING);
                        logMessage("Correct Labels are ERROR, WARNING, STATE, INFO, DEBUG; INFO is default", Labels.DEBUG);
                        logMessage("Custom Data should contain one of them (eg. ERROR)", Labels.DEBUG);

                    }
                }
                catch (Exception e)
                {
                    parent.Echo(e.ToString());
                }
            }

            //Intern Method to find the right point to insert log messages
            int findEndOfLogHead(string currentText)
            {
                int index = currentText.IndexOf("Recent Updates");
                index = currentText.IndexOf("\n", index) + 1;
                return index;
            }
            //-----------------------------------------------------------------


            //When called, logs a message mit the correct label on the screens
            public void logMessage(string message, Labels label = Labels.INFO, IMyTextPanel lcd = null)
            {
                if (logEnabled)
                {
                    message = message + "\n";
                    logTextOnScreen(message, label, lcd);
                }
                else
                {
                    parent.Echo(message);
                }
            }
            //------------------------------------------------------------------


            //Intern method to print a log message on all LCDs with correct label (not implemented by now)
            void logTextOnScreen(string message, Labels label, IMyTextPanel LCD = null)
            {
                int counter = 0;
                foreach (IMyTextPanel lcd in LCDlist)
                {
                    if (displayHasLabel(counter, label, LCD))
                    {
                        string currentText = lcd.GetPublicText();
                        int index = findEndOfLogHead(currentText);
                        currentText = currentText.Insert(index, label + ": " + message);
                        lcd.WritePublicText(currentText);
                    }
                    counter++;
                }
            }
            //------------------------------------------------------------------------------------------


            bool displayHasLabel(int lcdID, Labels label, IMyTextPanel LCD)
            {
                if (!LCDlist[lcdID].Equals(LCD) && LCD != null)
                {
                    return false;
                }
                if (LCDlabels[lcdID] >= (int)label)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }


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
                    lcd.WritePublicText("Current LCD Label : " + getLCDLabel(lcd) + "\n", true);
                    lcd.WritePublicText("Status Hangar Doors : " + hangardoors + "\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    lcd.WritePublicText(updateText, true);
                }
            }
            //------------------------------------------------------------------------------------------

            //Hässlich, das muss noch weg
            string getLCDLabel(IMyTextPanel lcd)
            {
                return ((Labels) LCDlabels[LCDlist.FindIndex(x => x == lcd)]).ToString();
            }
        }
    }
}
