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

        public class ChatHandler
        {
            struct RequestSave
            {
                public string targetName;
                public int targetID;
                public int requestsOpen;
                public List<string> messages;

                public bool IsEqual(int ID)
                {
                    return (ID == targetID);
                }
            }

            public Program parent;
            Dictionary<int, ChatModule> chatWindows = new Dictionary<int, ChatModule>();
            List<string> knownShips = new List<string>();
            List<RequestSave> requests = new List<RequestSave>();
            Dictionary<string, int[]> lookUpTable = new Dictionary<string, int[]>();
            
            //SCRIPTINPUT (FS)
            public ChatHandler(Program par, string CHAT_NAME)
            {
                parent = par;
                List<IMyTextPanel> lcdPanel = new List<IMyTextPanel>();
                parent.GridTerminalSystem.GetBlocksOfType(lcdPanel, x => x.CustomName.Contains(CHAT_NAME));
                int id = 1;
                foreach (IMyTextPanel lcd in lcdPanel)
                {
                    chatWindows.Add(id, new ChatModule(this, lcd, id));
                    id++;
                }
            }

            //SCRIPTINPUT (FS)
            public void HandleRequest(int ownID, int requesterID, Request_Options option, string requester)
            {
                if (!lookUpTable.Keys.Contains(requester))
                {
                    parent.printOut("ERROR: 0X006 @ CHAT_HANDLER/HANDLE_REQUEST");
                    throw new Exception("ERROR: 0X006 @ CHAT_HANDLER/HANDLE_REQUEST");
                }
                int position = GetRequestFromID(requester, requesterID);
                if (position == -1)
                {
                    parent.printOut("ERROR: 0X0001 @ CHAT_HANDLER/HANDLE_REQUEST");
                    throw new Exception("ERROR: 0X0001 @ CHAT_HANDLER/HANDLE_REQUEST");
                }
                RequestSave current = requests[position];
                switch (option)
                {
                    case Request_Options.ACCEPT:
                        foreach (ChatModule cm in chatWindows.Values.ToList())
                        {
                            if (cm.IsEqual(ownID))
                            {
                                if (current.requestsOpen > 0)
                                {
                                    cm.SetChatPartner();
                                    foreach (string mes in current.messages)
                                    {
                                        cm.AddText(mes);
                                    }
                                }
                            }
                            AssignNext(cm, position, requester);
                        }
                        RemoveRequestFromList(position, false);
                        break;
                    case Request_Options.DECLINE:
                        AssignNext(chatWindows[ownID], position, requester);
                        current.requestsOpen--;
                        if (current.requestsOpen <= 0)
                        {
                            RemoveRequestFromList(position, true);
                        }
                        break;
                    case Request_Options.DECLINE_ALL:
                        foreach (ChatModule cm in chatWindows.Values.ToList())
                        {
                            AssignNext(cm, position, requester);
                        }
                        RemoveRequestFromList(position, true);
                        break;
                }
            }

            //SCRIPTINPUT (FS)
            public void HandleMessage(string message, string sender)
            {
                string[] messageParts = message.Split('/');
                if (messageParts.Length < 3)
                {
                    parent.printOut("ERROR: 0X007 @ CHAT_HANDLER/HANDLE_MESSAGE");
                    throw new Exception("ERROR: 0X007 @ CHAT_HANDLER/HANDLE_MESSAGE");
                }
                if (messageParts[1] == "ReqDec")
                {
                    int id;
                    try
                    {
                        id = int.Parse(messageParts[2]);
                        chatWindows[id].ConnectionDeclined(sender);
                    }
                    catch (Exception)
                    {
                        parent.printOut("WARNING: Bad message recieved: " + message);
                        return;
                    }
                    return;
                }
                try
                {
                    int[] Ids = ExtractID(message);
                    if (Ids[1] == 0)
                    {
                        RequestSave rs = new RequestSave
                        {
                            targetName = sender,
                            targetID = Ids[0],
                            requestsOpen = chatWindows.Count,
                            messages = new List<string>()
                        };
                        string mes = message.Split('/')[4];
                        rs.messages.Add(mes);
                        requests.Add(rs);
                        if (!lookUpTable.Keys.Contains(sender))
                        {
                            int[] temp = { requests.Count - 1 };
                            lookUpTable.Add(sender, temp);
                        }
                        else
                        {
                            int[] save = lookUpTable[sender];
                            int[] copy = new int[save.Length];
                            for (int i = 0; i < save.Length; i++)
                            {
                                copy[i] = save[i];
                            }
                            copy[copy.Length - 1] = Ids[0];
                        }
                        foreach (ChatModule cm in chatWindows.Values.ToList())
                        {
                            cm.SetRequest(sender, Ids[0]);
                        }
                    }
                    else if(Ids[0] != -1)
                    {
                        string mes = message.Split('/')[4];
                        chatWindows[Ids[1]].AddText(mes);
                    }
                }
                catch (Exception e)
                {
                    parent.printOut(e.Message);
                    parent.printOut("ERROR: 0X008 @ CHAT_HANDLER/HANDLE_MESSAGE");
                    throw new Exception("ERROR: 0X008 @ CHAT_HANDLER/HANDLE_MESSAGE");
                }
            }

            //USERINPUT (FS)
            public void HandleArgument(string message)
            {
                try
                {
                    string[] parts = message.Split('_');
                    int ID = int.Parse(parts[1]);
                    chatWindows[ID].CheckArgument(parts[2]);
                }
                catch (Exception)
                {
                    parent.printOut("WARNING: Bad argument recieved: " + message);
                }
            }

            //SCRIPTINPUT (FS)
            public void SendMessage(string target, string message)
            {
                parent.SendMessage(target, message);
            }

            //SCRIPTINPUT (FS)
            public void updateShip(string name, bool delete)
            {
                foreach (ChatModule cm in chatWindows.Values.ToList())
                {
                    cm.UpdateShipStatus(name, delete);
                }
                if (delete && knownShips.Contains(name))
                {
                    knownShips.Remove(name);
                }
                if (!delete)
                {
                    if (!knownShips.Contains(name))
                    {
                        knownShips.Add(name);
                    }
                }
            }

            //SCRIPTINPUT (FS)
            public void MessageDropped(Message message)
            {
                string mes = message.payload;
                int[] ID = ExtractID(mes);
                chatWindows[ID[0]].MessageDropped(ID[1]);
            }

            //SCRIPTINPUT (FS)
            void AssignNext(ChatModule cm, int position, string requester)
            {
                if (cm.RemoveRequest(requester))
                {
                    if (position + 1 < requests.Count)
                    {
                        cm.SetRequest(requests[position + 1].targetName, requests[position + 1].targetID);
                    }
                }
            }

            //NO INPUT (FS)
            public List<string> GetKnownShips()
            {
                return knownShips;
            }

            //NO INPUT (FS)
            public void Run()
            {
                foreach (ChatModule chat in chatWindows.Values.ToList())
                {
                    chat.Run();
                }
            }

            //SCRIPTINPUT (FS)
            int GetRequestFromID(string target, int requestID)
            {
                int[] possiblePositions = lookUpTable[target];
                int position = -1;
                foreach (int pos in possiblePositions)
                {
                    if (requests[pos].IsEqual(requestID))
                    {
                        position = pos;
                        break;
                    }
                }
                return position;
            }

            //USERINPUT (FS)
            int[] ExtractID(string mes)
            {
                try
                {
                    string[] parts = mes.Split('/');
                    int[] output = { 0, 0 };
                    output[0] = int.Parse(parts[2]);
                    output[1] = int.Parse(parts[3]);
                    return output;
                }
                catch
                {
                    parent.printOut("WARNING: Bad IDs recieved: " + mes);
                    return new int[] {-1, -1 };
                }
            }

            //SCRIPTINPUT (FS)
            void RemoveRequestFromList(int position, bool declined)
            {
                RequestSave toDelete = requests[position];
                int[] cPos = lookUpTable[toDelete.targetName];
                if (cPos.Length == 1)
                {
                    if (cPos[0] != position)
                    {
                        parent.printOut("ERROR: 0X002 @ CHAT_HANDLER/REMOVE_REQUEST");
                        throw new Exception("ERROR: 0X002 @ CHAT_HANDLER/REMOVE_REQUEST");
                    }
                    lookUpTable.Remove(toDelete.targetName);
                    requests.RemoveAt(position);
                }
                else
                {
                    int[] newLookup = new int[lookUpTable[toDelete.targetName].Length - 1];
                    short counter = 0;
                    try
                    {
                        foreach (int i in cPos)
                        {
                            if (i != position)
                            {
                                newLookup[counter] = i;
                                counter++;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //ID not found => tried to copy all n entries in n-1 array => exception => ??? => profit
                        parent.printOut("ERROR: 0X003 @ CHAT_HANDLER/REMOVE_REQUEST");
                        throw new Exception("ERROR: 0X003 @ CHAT_HANDLER/REMOVE_REQUEST");
                    }
                    lookUpTable[toDelete.targetName] = newLookup;
                    requests.RemoveAt(position);
                }
                if (declined)
                {
                    string mes = "Chat/ReqDec/" + toDelete.targetID;
                    parent.SendMessage(toDelete.targetName, mes);
                }
            }
        }
    }
}
