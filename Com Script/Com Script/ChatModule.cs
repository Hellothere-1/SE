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
            //Variables for LCD state
            enum Window { MAIN, SELECTION, REQUEST, REQDEC, CHAT};
            short CHAT_OFFSET = (short)Window.CHAT;

            //Hardware regarding variables
            ChatHandler parent;
            Window currentState = Window.MAIN;
            List<string> knownShips = new List<string>();
            IMyTextPanel window;
            short width = 50;
            int ID;

            //Data pair of current chat partner
            string currentTarget = "";
            int currentTargetID = 0;

            //Data pair of requesting chat partner
            string currentRequest = "";
            int currentRequestID = 0;
            
            //Needed for indication of pointer on screen
            int pointer = 0;
            int subpointer = 0;

            //SCRIPTINPUT (FS)
            public ChatModule(ChatHandler par,IMyTextPanel panel, int number)
            {
                parent = par;
                window = panel;
                ID = number;
                InitWindow();
                window.CustomData = "";
            }

            //NO INPUT (FS)
            public void SetChatPartner()
            {
                currentTarget = currentRequest;
                currentTargetID = currentRequestID;
                currentState = Window.CHAT;
                UpdateChatWindow();
            }

            //SCRIPTINPUT (FS)
            public void UpdateShipStatus(string name, bool delete)
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
                if (currentState == Window.MAIN)
                {
                    UpdateChatWindow();
                }
                
            }
        
            //USERINPUT (FS)
            public void AddText(string mes)
            {
                string output = "[" + currentTarget + "]: " + mes;
                output = FormatMessage(output);
                string[] entry = output.Split('\n');
                string[] lines = window.GetPublicText().Split('\n');
                List<string> linesList = lines.ToList();
                short counter = (short)(lines.Length - 1);
                while (!lines[counter].StartsWith("[You]"))
                {
                    counter--;
                }
                foreach (string line in entry)
                {
                    linesList.Insert(counter, line);
                    counter++;
                }
                lines = linesList.ToArray();
                window.CustomData = "";
                foreach (string line in lines)
                {
                    if (line != lines[lines.Length - 1])
                    {
                        window.CustomData = window.CustomData + line + "\n";
                    }
                    else
                    {
                        window.CustomData = window.CustomData + line;
                    }
                }
                if (currentState == Window.CHAT)
                {
                    UpdateChatWindow();
                }
            }
            
            //NO INPUT (FS)
            public void Run()
            {
                if (currentState != Window.CHAT && currentState != Window.MAIN)
                {
                    return;
                }
                if(currentState == Window.MAIN)
                {
                    if (currentRequest != "")
                    {
                        currentState = Window.REQUEST;
                    }
                    return;
                }
                string input = GetInput();
                if (input != "")
                {
                    input = input.Substring(7);
                    string mes = "Chat/Mes/" + ID + "/" + currentTargetID + "/" + input;
                    parent.SendMessage(currentTarget, mes);
                }
            }

            //SCRIPTINPUT (FS)
            public void SetRequest(string name, int requestID)
            {
                if (currentRequest == "")
                {
                    currentRequest = name;
                    currentRequestID = requestID;
                    if (currentState == Window.MAIN)
                    {
                        currentState = Window.REQUEST;
                        UpdateChatWindow();
                    }
                    
                }
            }

            //SCRIPTINPUT (FS)
            //TODO next assigned after connection declined??
            public void ConnectionDeclined(string requested)
            {
                if (requested == currentTarget)
                {
                    currentState = Window.REQDEC;
                    UpdateChatWindow();
                }
            }

            //USERINPUT (FS)
            public void CheckArgument(string argument)
            {
                if (currentState == Window.REQDEC)
                {
                    currentState = Window.MAIN;
                    window.CustomData = "";
                    UpdateChatWindow();
                    return;
                }
                switch (argument)
                {
                    case "Down":
                        if (currentState == Window.MAIN && pointer < knownShips.Count - 1)
                        {
                            pointer++;
                        }
                        if (currentState == Window.SELECTION && subpointer < Enum.GetNames(typeof(Window)).Length - CHAT_OFFSET)
                        {
                            subpointer++;
                        }
                        if (currentState == Window.REQUEST && subpointer < Enum.GetNames(typeof(Request_Options)).Length - 1)
                        {
                            subpointer++;
                        }
                        break;
                    case "Up":
                        if (currentState == Window.MAIN && pointer > 0)
                        {
                            pointer--;
                        }
                        if (currentState == Window.SELECTION && subpointer > 0)
                        {
                            subpointer--;
                        }
                        if (currentState == Window.REQUEST && subpointer > 0)
                        {
                            subpointer--;
                        }
                        break;
                    case "Confirm":
                        if (currentState == Window.MAIN && knownShips.Count() > 0)
                        {
                            currentState = Window.SELECTION;
                            if (Enum.GetNames(typeof(Window)).Length == (CHAT_OFFSET + 1))
                            {
                                currentState = Window.CHAT;
                            }
                            currentTarget = knownShips[pointer];
                        }
                        else if (currentState == Window.SELECTION)
                        {
                            currentState = (Window)(CHAT_OFFSET + subpointer);
                            currentTarget = knownShips[pointer];
                        }
                        else if (currentState == Window.REQUEST)
                        {
                            parent.HandleRequest(ID, currentRequestID, (Request_Options)subpointer, currentRequest);
                        }

                        break;
                    case "Abort":
                        currentState = Window.MAIN;
                        window.CustomData = "";
                        subpointer = 0;
                        break;
                    default:
                        //TODO improve this structure
                        parent.parent.printOut("WARNING: Bad command at this LCD, was : " + argument);
                        break;
                }
                UpdateChatWindow();
            }

            //SCRIPTINPUT (FS)
            public void MessageDropped(int targetID)
            {
                if (currentTargetID == targetID)
                {
                    //TODO think of better way to do this
                    AddText("Last own message could not be send! Maybe target is out of range or destroyed");
                }
            }
            
            //NO INPUT (FS)
            void UpdateChatWindow()
            {
                switch (currentState)
                {
                    case Window.MAIN:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\nList of known Ships in Com Distance :\n");
                        //24 is the maximum amount of lines which can be show correctly 
                        int startPoint = pointer <= 24 ? 0 : (pointer - 23);
                        int endPoint = startPoint == 0 ? 24 : pointer + 1;
                        for (int i = startPoint; i < endPoint && i < knownShips.Count; i++)
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
                            window.CustomData = window.GetPublicText();
                        }
                        else
                        {
                            window.WritePublicText("");
                            string[] text = window.CustomData.Split('\n');
                            string[] save;
                            int start = text.Length <= 24 ? 0 : text.Length - 24;
                            if (text.Length > 24)
                            {
                                save = new string[24];
                                int counter = 0;
                                for (start = start; start < text.Length; start++)
                                {
                                    save[counter] = text[start];
                                }
                            }
                            else
                            {
                                save = text;
                            }
                            foreach (string line in save)
                            {
                                if (line != save[save.Length - 1])
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
                        for (int i = CHAT_OFFSET; i < Enum.GetNames(typeof(Window)).Length; i++)
                        {
                            if (subpointer != i - CHAT_OFFSET)
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
                    case Window.REQDEC:
                        window.WritePublicText("Com Chat V1.0 LCD: " + ID + "\n\n");
                        window.WritePublicText("Connection target " + currentTarget + " has declined the connection.\n", true);
                        window.WritePublicText("Press any button to continue.\n", true);
                        break;
                }
            }

            //SCRIPTINPUT (FS)
            void InitWindow()
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
                UpdateChatWindow();
            }
            
            //SCRIPTINPUT (FS)
            public bool RemoveRequest(string name)
            {
                if (currentRequest == name)
                {
                    currentRequest = "";
                    currentRequestID = 0;
                    if (currentState == Window.REQUEST)
                    {
                        currentState = Window.MAIN;
                        UpdateChatWindow();
                    }
                    return true;
                }
                return false;
            }

            //SCRIPTINPUT (FS)
            public bool IsEqual(int input)
            {
                return input == ID;
            }

            //NO INPUT (FS)
            string GetInput()
            {
                try
                {
                    List<string> lines = window.GetPublicText().Split('\n').ToList();
                    string output = "";
                    if (lines[lines.Count - 1] == "")
                    {
                        short counter = (short)(lines.Count - 1);
                        while (!lines[counter].StartsWith("[You]"))
                        {
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
                        UpdateChatWindow();
                    }
                    else if (lines[lines.Count - 1] != "[You]: ")
                    {
                        string input = lines[lines.Count - 1];
                        input = FormatMessage(input);
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

                            UpdateChatWindow();
                        }
                    }
                    return output;
                }
                catch (Exception)
                {
                    parent.parent.printOut("ERROR 0X001 @ CHATMODULE/GET_INPUT");
                    throw new Exception("ERROR 0X001 @ CHATMODULE/GET_INPUT");
                }
                
            }

            //SCRIPTINPUT (FS)
            string FormatMessage(string message)
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
        }
    }
}
