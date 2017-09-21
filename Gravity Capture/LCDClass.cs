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
        public enum Labels {BOOTUP, ERROR, WARNING, STATE, INFO, DEBUG };
        
        public class LCDClass
        {
            Program parent;
            Dictionary<IMyTextPanel, int> LCDDict;
            Dictionary<IMyTextPanel, int> WidthLCD;
            bool logEnabled;

            public LCDClass(List<IMyTextPanel> lcds, Program par)
            {
                parent = par;
                LCDDict = lcds.ToDictionary(x => x, y => 4);
                WidthLCD = lcds.ToDictionary(x => x, y => 70);
                logEnabled = lcds.Count() != 0 ? true : false;
                if (logEnabled)
                {
                    setUpDisplays();
                    logTextOnScreen("Output display found, logging activated\n", Labels.BOOTUP);
                }
                else
                {
                    parent.Echo("Output Panels missing, create a LCD/Text Panel with *Output* in the Name");
                }
            }
            //Constructor end ===================================================================


            //Intern method to prerpare the displays at startup
            void setUpDisplays()
            {
                foreach (IMyTextPanel lcd in LCDDict.Keys.ToList())
                {
                    lcd.WritePublicText("Current State : " + State.Idle.ToString() + "\n");
                    lcd.WritePublicText("Current Status : Waiting\n\n", true);
                    lcd.WritePublicText("Status Hangar Doors : Closed\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    logMessage("Booting up ...", Labels.BOOTUP, lcd);
                    findLengthOfLCD(lcd);
                    findLabelOfLCD(lcd);
                }
            }
            //====================================================================================

            
            //Checks the Display Type and saves the maximum amount of chars----
            void findLengthOfLCD(IMyTextPanel lcd)
            {
                string info = lcd.DetailedInfo.Split('\n')[0];
                if (info != "Type: Wide LCD panel")
                {
                    WidthLCD[lcd] = 35;
                }
                logMessage("Display Size : " + WidthLCD[lcd].ToString() + " Chars", Labels.BOOTUP, lcd);
            }
            //======================================================================================



            //Finds the label of the LCD and saves it in the dict--------------
            void findLabelOfLCD(IMyTextPanel lcd)
            {
                string label = lcd.CustomData;
                if(label == "")
                {
                    logMessage(lcd.CustomName + " (this) is now tagged with label INFO", Labels.BOOTUP, lcd);
                    return;
                }
                try
                {
                    Labels lcdLabel = (Labels)Enum.Parse(typeof(Labels), label);
                    if (lcdLabel == Labels.BOOTUP)
                    {
                        throw new ArgumentException("BOOTUP is not allowed as Label");
                    }
                    LCDDict[lcd] = (int)lcdLabel;
                    logMessage(lcd.CustomName + " (this) is now tagged with label " + lcdLabel, Labels.BOOTUP, lcd);
                }
                catch (ArgumentException)
                {
                    logMessage(lcd.CustomName + " (this) does not declare its label correct", Labels.WARNING, lcd);
                    logMessage(lcd.CustomName + " (this) will now be declared with label INFO", Labels.WARNING, lcd);
                    logMessage("Correct Labels are ERROR, WARNING, STATE, INFO, DEBUG; INFO is default", Labels.DEBUG);
                    logMessage("Custom Data should contain one of them (eg. ERROR)", Labels.DEBUG);
                }
                
            }
            //=========================================================================================


            //Intern Method to find the right point to insert log messages
            int findEndOfLogHead(string currentText)
            {
                int index = currentText.IndexOf("Recent Updates");
                index = currentText.IndexOf("\n", index) + 1;
                return index;
            }
            //============================================================================================


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
            //===============================================================================================


            //Intern method to print a log message on all LCDs with correct label
            void logTextOnScreen(string message, Labels label, IMyTextPanel LCD = null)
            {
                foreach (IMyTextPanel lcd in LCDDict.Keys.ToList())
                {
                    if (displayHasLabel(lcd, label) && (lcd.Equals(LCD) || LCD == null))
                    {
                        string currentText = lcd.GetPublicText();
                        int index = findEndOfLogHead(currentText);
                        string output = formatMessage(lcd, message, label);
                        currentText = currentText.Insert(index, output);
                        lcd.WritePublicText(currentText);
                    }
                }
            }
            //===================================================================================================


            //Formates Messages to match screen size ---------------------------------------------------
            string formatMessage(IMyTextPanel lcd, string message, Labels label)
            {
                int maxLength = WidthLCD[lcd];
                string[] words = message.Split(' ');
                string result = (label.ToString() + ": ");
                int length = label.ToString().Length + 2;
                foreach (string word in words)
                {
                    if (length + word.Length + 1 <= maxLength)
                    {
                        result = result + " " + word;
                        length = length + word.Length;
                    }
                    else
                    {
                        result = result + "\n" + word;
                        length = word.Length;
                    }
                }
                return result;
            }
            //=================================================================================================


            //Checks if a lcd allows a label to be printed on it (Das klingt scheiße aber mir fällt gerade nichts besseres ein)
            bool displayHasLabel(IMyTextPanel lcd, Labels label)
            {
                if (LCDDict[lcd] >= (int)label)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            //=============================================================================================


            //When called, logs the current state in the Head of the Display-----------------------------
            public void logHeadOnScreen(State currentState, bool running, bool hangarsOpen)
            {
                string hangardoors = hangarsOpen ? "Open" : "Closed";
                string status = running ? "Running" : "Waiting";
                foreach (IMyTextPanel lcd in LCDDict.Keys.ToList())
                {
                    string currentText = lcd.GetPublicText();
                    string updateText = "";
                    int index = findEndOfLogHead(currentText);
                    updateText = currentText.Substring(index);
                    lcd.WritePublicText("Current State : " + currentState.ToString() + "\n");
                    lcd.WritePublicText("Current Status : " + status + "\n", true);
                    lcd.WritePublicText("Current LCD Label : " + (Labels) LCDDict[lcd] + "\n", true);
                    lcd.WritePublicText("Status Hangar Doors : " + hangardoors + "\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    lcd.WritePublicText(updateText, true);
                }
            }
            //=================================================================================================
        }
    }
}
