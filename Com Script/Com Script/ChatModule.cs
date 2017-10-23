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

            enum Window { MAIN, SELECTION, REQUEST, CHAT};

            Program parent;
            Window current = Window.MAIN;
            List<string> knownShips = new List<string>();
            string currentTarget = "";
            string currentRequest = "";
            int requesterID = 0;
            int targetTerminalID = 0;
            IMyTextPanel window;
            short width = 50;
            int pointer = 0;
            int subpointer = 0;
            int ID;

            public ChatModule(Program par,IMyTextPanel panel, int number)
            {
                parent = par;
                window = panel;
                ID = number;
                initWindow();
                window.CustomData = "";
            }

            public bool isEqual(int input)
            {
                return input == ID;
            }

            public void SetChatPartner()
            {
                currentTarget = currentRequest;
                targetTerminalID = requesterID;
                current = Window.CHAT;
                updateChatWindows();
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
                    if (!knownShips.Contains(name))
                    {
                        knownShips.Add(name);
                    }
                }
                if (current == Window.MAIN)
                {
                    updateChatWindows();
                }
                
            }

            public void addText(string mes)
            {
                string output = "[" + currentTarget + "]: " + mes + "\n";
                output = formatMessage(output);
                string[] entry = output.Split('\n');
                string[] lines = window.GetPublicText().Split('\n');
                List<string> linesList = lines.ToList();
                short counter = (short)(lines.Length - 1);
                while (!lines[counter].StartsWith("[You]"))
                {
                    counter--;
                }
                counter--;
                foreach (string line in entry)
                {
                    linesList.Insert(counter, line);
                    counter++;
                }
                lines = linesList.ToArray();
                window.CustomData = "";
                foreach (string line in lines)
                {
                    window.CustomData = window.CustomData + line + "\n";
                }
                if (current == Window.CHAT)
                {
                    updateChatWindows();
                }
            }
            
            public void run()
            {
                if (current != Window.CHAT)
                {
                    return;
                }
                string input = getInput();
                if (input != "")
                {
                    string mes = "Chat/" + ID + "/" + targetTerminalID + "/" + input;
                    parent.comHandler.SendMessage(currentTarget, mes, true);
                    parent.Echo("Message send");
                }
            }

            public void lockRequest(string name, int requestID)
            {
                if (current == Window.MAIN)
                {
                    current = Window.REQUEST;
                    currentRequest = name;
                    requesterID = requestID;
                    updateChatWindows();
                }
            }

            public string releaseRequest(string name)
            {
                if (current == Window.REQUEST && currentRequest == name)
                {
                    current = Window.MAIN;
                    string lastRequest = currentRequest;
                    currentRequest = "";
                    requesterID = 0;
                    updateChatWindows();
                    return lastRequest;
                }
                return "";
            }
            
            string getInput()
            {
                List<string> lines = window.GetPublicText().Split('\n').ToList();
                string output = "";
                if (lines[lines.Count - 1] == "")
                {
                    short counter = (short)(lines.Count - 1);
                    while (!lines[counter].StartsWith("[You]"))
                    {
                        parent.Echo(lines[counter]);
                        counter--;
                        output = lines[counter] + " " + output;
                    }
                    lines[lines.Count - 1] = "[You]: ";
                    window.CustomData = "";
                    //Rewrite custom data with new content
                    foreach (string line in lines)
                    {
                        if (line != "[You]: ")
                        {
                            window.CustomData = window.CustomData + line + "\n";
                        }
                        else
                        {
                            window.CustomData = window.CustomData + line;
                        }
                    }
                    updateChatWindows();
                    /*
                }*/
                }
                else if(lines[lines.Count - 1] != "[You]: ")
                {
                    string input = lines[lines.Count - 1];
                    input = formatMessage(input);
                    string[] inputLines = input.Split('\n');
                    if (inputLines.Length != 1)
                    {
                        lines[lines.Count - 1] = inputLines[0];
                        lines.Add(inputLines[1]);
                        window.CustomData = "";
                        foreach (string line in lines)
                        {
                            if (line != lines[lines.Count - 1])
                            {
                                window.CustomData = window.CustomData + line + "\n";
                            }
                            else
                            {
                                window.CustomData = window.CustomData + line;
                            }
                        }
                        
                        updateChatWindows();
                    }
                }
                return output;
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
                        if (window.CustomData == "")
                        {
                            window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\nChatting with " + currentTarget + " :\n");
                            window.WritePublicText("[You]: ", true);
                        }
                        else
                        {
                            window.WritePublicText("");
                            string[] text = window.CustomData.Split('\n');
                            foreach (string line in text)
                            {
                                if (line != text[text.Length -1])
                                {
                                    window.WritePublicText(line + "\n", true);
                                }
                                else
                                {
                                    window.WritePublicText(line, true);
                                }
                                
                            }
                        }
                        break;

                    case Window.SELECTION:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\n");
                        string option = knownShips[pointer];
                        window.WritePublicText("        " + option + "\n", true);
                        for (int i = 3; i < Enum.GetNames(typeof(Window)).Length; i++)
                        {
                            if (subpointer != i - 3)
                            {
                                window.WritePublicText("                " + (Window)i + "\n", true);
                            }
                            else
                            {
                                window.WritePublicText("          =>  " + (Window)i + "\n", true);
                            }
                        }
                        break;
                    case Window.REQUEST:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\n");
                        window.WritePublicText("Hot single " + currentRequest + " in your area wants to chat with you\n", true);
                        for (int i = 0; i < Enum.GetNames(typeof(Request_Options)).Length; i++)
                        {
                            if (subpointer != i)
                            {
                                window.WritePublicText("                " + (Request_Options)i + "\n", true);
                            }
                            else
                            {
                                window.WritePublicText("          =>  " + (Request_Options)i + "\n", true);
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
                        if (current == Window.MAIN && pointer < knownShips.Count - 1)
                        {
                            pointer++;
                        }
                        if (current == Window.SELECTION && subpointer < Enum.GetNames(typeof(Window)).Length - 3)
                        {
                            subpointer++;
                        }
                        if (current == Window.REQUEST && subpointer < Enum.GetNames(typeof(Request_Options)).Length - 1)
                        {
                            subpointer++;
                        }
                        break;
                    case "Up":
                        if (current == Window.MAIN && pointer > 0)
                        {
                            pointer--;
                        }
                        if (current == Window.SELECTION && subpointer > 0)
                        {
                            subpointer--;
                        }
                        if (current == Window.REQUEST && subpointer > 0)
                        {
                            subpointer--;
                        }
                        break;
                    case "Confirm":
                        if (current == Window.MAIN && knownShips.Count() > 0)
                        {
                            current = Window.SELECTION;
                            if (Enum.GetNames(typeof(Window)).Length == 4)
                            {
                                current = (Window)3;
                            }
                            currentTarget = knownShips[pointer];
                        }
                        else if (current == Window.SELECTION)
                        {
                            current = (Window)(3 + subpointer);
                            currentTarget = knownShips[pointer];
                        }
                        else if (current == Window.REQUEST)
                        {
                            parent.HandleRequest(ID, (Request_Options)subpointer, currentRequest);
                        }
                        
                        break;
                    case "Abort":
                        current = Window.MAIN;
                        window.CustomData = "";
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
                string[] words = message.Split(' ', '\n');
                string result = "";
                int length = 0;
                foreach (string word in words)
                {
                    if (length + word.Length + 1 <= width)
                    {
                        result = result + word + " ";
                        length = length + word.Length + 1;
                    }
                    else
                    {
                        result = result + "\n" + word + " ";
                        length = word.Length + 1;
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
                    width = 30;
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
