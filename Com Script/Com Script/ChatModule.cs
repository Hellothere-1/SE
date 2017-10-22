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
        public class ChatModule
        {
            const short lcdLenght = 20;

            enum Window { MAIN, SELECTION, CHAT, ORDER, POSITION };

            Window current = Window.MAIN;
            List<string> knownShips = new List<string>();
            string currentTarget = "";
            IMyTextPanel window;
            short width;
            int pointer = 0;
            int subpointer = 0;
            int ID;

            public ChatModule(IMyTextPanel panel, int number)
            {
                window = panel;
                ID = number;
                initWindow();
            }

            public void updateShipStatus(string name, bool delete)
            {
                if (delete && knownShips.Contains(name))
                {
                    if (knownShips.IndexOf(name) <= pointer)
                    {
                        pointer--;
                    }
                    knownShips.Remove(name);
                }
                if (!delete)
                {
                    knownShips.Add(name);
                    updateChatWindows();
                }
            }

            public void addText(string mes)
            {
                string output = "[" + currentTarget + "]: " + mes + "\n";
                output = formatMessage(output);
                window.CustomData = window.CustomData + output;
                updateChatWindows();
            }

            void updateChatWindows()
            {
                switch (current)
                {
                    case Window.MAIN:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\nList of known Ships in Com Distance :\n");
                        for (int i = 0; i < lcdLenght + 4 && i < knownShips.Count; i++)
                        {
                            if (i != pointer)
                            {
                                window.WritePublicText("        " + knownShips[i] + "\n", true);
                            }
                            else
                            {
                                window.WritePublicText("  =>  " + knownShips[i] + "\n", true);
                            }
                        }
                        break;

                    case Window.CHAT:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\nChatting with " + currentTarget + " :\n");
                        string[] text = window.CustomData.Split('\n');
                        short startIndex = 0;
                        if (text.Length > lcdLenght)
                        {
                            startIndex = (short)(text.Length - 20);
                        }
                        //thank you c# for this shit
                        for (startIndex = startIndex; startIndex < text.Length; startIndex++)
                        {
                            window.WritePublicText(text[startIndex], true);
                        }
                        window.WritePublicText("[You]: ", true);
                        if (text.Length >= 40)
                        {
                            startIndex = 10;
                            window.CustomData = "";
                            for (startIndex = startIndex; startIndex < text.Length; startIndex++)
                            {
                                window.CustomData = window.CustomData + text[startIndex];
                            }
                        }
                        break;

                    case Window.SELECTION:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\nList of known Ships in Com Distance :\n");
                        string option = knownShips[pointer];
                        window.WritePublicText("        " + option + "\n", true);
                        for (int i = 2; i < Enum.GetNames(typeof(Window)).Length; i++)
                        {
                            if (subpointer != i - 2)
                            {
                                window.WritePublicText("                " + (Window)i + "\n", true);
                            }
                            else
                            {
                                window.WritePublicText("          =>  " + (Window)i + "\n", true);
                            }
                        }
                        break;
                }
            }

            public void checkArgument(string argument)
            {
                switch (argument)
                {
                    case "Down":
                        if (pointer < knownShips.Count - 1 && current != Window.SELECTION)
                        {
                            pointer++;
                        }
                        if (current == Window.SELECTION && subpointer < Enum.GetNames(typeof(Window)).Length - 3)
                        {
                            subpointer++;
                        }
                        break;
                    case "Up":
                        if (pointer > 0 && current != Window.SELECTION)
                        {
                            pointer--;
                        }
                        if (current == Window.SELECTION && subpointer > 0)
                        {
                            subpointer--;
                        }
                        break;
                    case "Confirm":
                        if (current == Window.MAIN && knownShips.Count() > 0)
                        {
                            current = Window.SELECTION;
                            if (Enum.GetNames(typeof(Window)).Length == 3)
                            {
                                current = (Window)3;
                            }
                        }
                        else if (current == Window.SELECTION)
                        {
                            current = (Window)(2 + subpointer);
                            currentTarget = knownShips[pointer];
                        }
                        break;
                    case "Abort":
                        current = Window.MAIN;
                        subpointer = 0;
                        break;
                    default:
                        current = Window.MAIN;
                        subpointer = 0;
                        break;
                }
                updateChatWindows();
            }

            string formatMessage(string message)
            {
                string[] words = message.Split(' ');
                string result = "";
                int length = result.Length;
                foreach (string word in words)
                {
                    if (length + word.Length + 1 <= width)
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

            void initWindow()
            {
                window.ShowPublicTextOnScreen();
                window.WritePublicTitle("Com Chat V1.0");
                window.WritePublicText("Com Chat V1.0 \n\nList of known Ships in Com Distance :");
                string info = window.DetailedInfo.Split('\n')[0];
                if (info != "Type: Wide LCD panel")
                {
                    width = 35;
                }
                knownShips.Add("Ship 1");
                knownShips.Add("Ship 2");
                knownShips.Add("Ship 3");
                knownShips.Add("Ship 4");
                updateChatWindows();
            }


        }
    }
}
