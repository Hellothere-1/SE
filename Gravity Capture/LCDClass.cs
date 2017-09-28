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
        public enum Labels {LOCK = -1, BOOT, cERR, WARN, STAT, INFO, DBUG };
        [Flags]
        public enum Tags {GRA = 1, OXY = 2, STM = 4, LCD = 8}
        
        public class LCDClass
        {
            Program parent;
            Dictionary<IMyTextPanel, int[]> LCDDict;
            Dictionary<IMyTextPanel, int> WidthLCD;
            bool logEnabled;

            public LCDClass(Program par)
            {
                parent = par;
                List<IMyTextPanel> outputPanels = new List<IMyTextPanel>();
                parent.GridTerminalSystem.GetBlocksOfType(outputPanels, x => x.CustomName.Contains("Output"));
                LCDDict = outputPanels.ToDictionary(x => x, y => new int[] {4, 15});
                WidthLCD = outputPanels.ToDictionary(x => x, y => 70);
                logEnabled = outputPanels.Count() != 0 ? true : false;
                if (logEnabled)
                {
                    setUpDisplays();
                    logTextOnScreen("Logging operational (LCD-Panels)\n", Labels.BOOT, Tags.LCD);
                }
                else
                {
                    parent.Echo("Logging operational (Console)");
                }
            }
            //Constructor end ===================================================================


            //Intern method to prerpare the displays at startup
            void setUpDisplays()
            {
                foreach (IMyTextPanel lcd in LCDDict.Keys.ToList())
                {
                    lcd.ShowPublicTextOnScreen();
                    lcd.WritePublicText("Current State : " + State.Idle.ToString() + "\n");
                    lcd.WritePublicText("Current Status : Waiting\n\n", true);
                    lcd.WritePublicText("Status Hangar Doors : Closed\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    logMessage("Booting up ...", Tags.LCD, Labels.BOOT, lcd);
                    findLengthOfLCD(lcd);
                    findLabelOfLCD(lcd);
                }
                foreach (IMyTextPanel lcd in LCDDict.Keys.ToList())
                {
                    if (lcd.CustomName.StartsWith("//Not initialized//"))
                    {
                        lcd.CustomName = lcd.CustomName.Substring(19);
                        logMessage(lcd.CustomName + " does not declare its label correct", Tags.LCD, Labels.WARN);
                        logMessage(lcd.CustomName + " (this) will now be declared with label INFO", Tags.LCD, Labels.WARN, lcd);
                        logMessage("Correct Labels are cERR, WARN, STAT, INFO (default), DBUG", Tags.LCD, Labels.DBUG);
                        logMessage("Correct Tags are GRA, LCD, STM, OXY", Tags.LCD, Labels.DBUG);
                        logMessage("Format is: label(optional) tag1 tag2 tag3", Tags.LCD, Labels.DBUG);
                    }
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
                logMessage("Display Size : " + WidthLCD[lcd].ToString() + " Chars", Tags.LCD, Labels.BOOT, lcd);
            }
            //======================================================================================



            //Finds the label of the LCD and saves it in the dict--------------
            void findLabelOfLCD(IMyTextPanel lcd)
            {
                string[] label = lcd.CustomData.Split(' ');
                Labels lcdLabel;
                if (label[0] == "")
                {
                    logMessage(lcd.CustomName + " (this): Label " + (Labels)LCDDict[lcd][0] + ", shows " + (Tags)LCDDict[lcd][1], Tags.LCD, Labels.BOOT, lcd);
                    return;
                }
                try
                {
                    lcdLabel = (Labels)Enum.Parse(typeof(Labels), label[0]);
                    int tag = 0;
                    if (label.Length != 1)
                    {

                        for (int i = 1; i < label.Length; i++)
                        {
                            tag = tag | (int)Enum.Parse(typeof(Tags), label[i]);
                        }
                    }
                    else
                    {
                        tag = 15;
                    }
                    LCDDict[lcd] = new int[] { (int)lcdLabel, tag };
                    logMessage(lcd.CustomName + " (this): Label " + lcdLabel + ", shows " + (Tags) tag , Tags.LCD, Labels.BOOT, lcd);
                }
                catch (ArgumentException)
                {
                    try
                    {
                        int tag = 0;
                        for (int i = 0; i < label.Length; i++)
                        {
                            tag = tag | (int)Enum.Parse(typeof(Tags), label[i]);
                        }
                        LCDDict[lcd] = new int[] { (int)Labels.INFO, tag };
                        logMessage(lcd.CustomName + " (this): Label " + Labels.INFO + ", shows " + (Tags)tag, Tags.LCD, Labels.BOOT, lcd);
                    }
                    catch (ArgumentException)
                    {
                        if (!lcd.CustomName.StartsWith("//Not initialized//"))
                        {
                            lcd.CustomName = "//Not initialized//" + lcd.CustomName;
                        }
                    }
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
            public void logMessage(string message, Tags tag, Labels label = Labels.INFO, IMyTextPanel lcd = null)
            {
                if (logEnabled)
                {
                    message = message + "\n";
                    logTextOnScreen(message, label, tag, lcd);
                }
                else
                {
                    parent.Echo(message);
                }
            }
            //===============================================================================================


            //Intern method to print a log message on all LCDs with correct label
            void logTextOnScreen(string message, Labels label, Tags tag, IMyTextPanel LCD = null)
            {
                foreach (IMyTextPanel lcd in LCDDict.Keys.ToList())
                {
                    if (displayHasLabel(lcd, label, (int) tag) && (lcd.Equals(LCD) || LCD == null))
                    {
                        string currentText = lcd.GetPublicText();
                        int index = findEndOfLogHead(currentText);
                        string output = formatMessage(lcd, message, label, tag);
                        currentText = currentText.Insert(index, output);
                        lcd.WritePublicText(currentText);
                    }
                }
            }
            //===================================================================================================


            //Formates Messages to match screen size ---------------------------------------------------
            string formatMessage(IMyTextPanel lcd, string message, Labels label, Tags tag)
            {
                int maxLength = WidthLCD[lcd];
                string[] words = message.Split(' ');
                string result = (label + "(" + tag +"): ");
                int length = result.Length;
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
            bool displayHasLabel(IMyTextPanel lcd, Labels label, int tag)
            {
                if ((LCDDict[lcd][0] >= (int)label && (tag & (LCDDict[lcd])[1]) == tag ? true : false && lcd != null) || label == Labels.BOOT)
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
                    lcd.WritePublicText("Current LCD Label : " + (Labels) LCDDict[lcd][0] + "\n", true);
                    lcd.WritePublicText("Current LCD Tags : " + (Tags) LCDDict[lcd][1] + "\n", true);
                    lcd.WritePublicText("Status Hangar Doors : " + hangardoors + "\n", true);
                    lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                    lcd.WritePublicText(updateText, true);
                }
            }
            //=================================================================================================
        }
    }
}
