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

                public bool isEqual(int ID)
                {
                    return (ID == targetID);
                }
            }

            public Program parent;
            Dictionary<int, ChatModule> chatWindows = new Dictionary<int, ChatModule>();
            //No list in dict, no time tracked, make a list out of it...
            Dictionary<string, List<RequestSave>> requests = new Dictionary<string, List<RequestSave>>();
            List<string> knownShips = new List<string>();

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

            public void HandleRequest(int ownID, int targetID, Request_Options option, string requester)
            {
                RequestSave current = getRequestFromID(requester, targetID);
                if (current.targetID == -1)
                {
                    //Did not found the correct request, bad code maybe?
                    return;
                }
                switch (option)
                {
                    case Request_Options.ACCEPT:
                        foreach (ChatModule cm in chatWindows.Values.ToList())
                        {
                            //TODO assign new request from list
                            cm.RemoveRequest(requester);
                            if (cm.IsEqual(ownID))
                            {
                                cm.SetChatPartner();
                            }
                        }
                        requests[requester].Remove(current);
                        break;
                    case Request_Options.DECLINE:
                        chatWindows[ownID].RemoveRequest(requester);
                        current.requestsOpen--;
                        break;
                    case Request_Options.DECLINE_ALL:
                        foreach (ChatModule cm in chatWindows.Values.ToList())
                        {
                            cm.RemoveRequest(requester);
                        }
                        current.requestsOpen = 0;
                        break;
                }
            }

            public void HandleMessage(string message, string sender)
            {
                string tag = message.Split('/')[1];
                if (tag == "ReqDec")
                {
                    int id = int.Parse(message.Split('/')[2]);
                    chatWindows[id].ConnectionDeclined(sender);
                    return;
                }

                int[] Ids = extractID(message);
                if (Ids[1] == 0)
                {
                    bool found = false;
                    foreach (string name in requests.Keys.ToList())
                    {
                        foreach (RequestSave rs in requests[name])
                        {
                            if (rs.isEqual(Ids[2]))
                            {
                                found = true;
                                rs.messages.Add(message);
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        RequestSave rs = new RequestSave();
                        rs.targetName = sender;
                        rs.targetID = Ids[2];
                        rs.requestsOpen = chatWindows.Count;
                        rs.messages.Add(message);
                        if (!requests.ContainsKey(sender))
                        {
                            requests.Add(sender, new List<RequestSave>());
                        }
                        requests[sender].Add(rs);
                    }
                    foreach (ChatModule cm in chatWindows.Values.ToList())
                    {
                        cm.SetRequest(sender, Ids[2]);
                    }
                }
                else
                {
                    chatWindows[Ids[0]].AddText(message);
                }
            }

            public void HandleArgument(string message)
            {
                string[] parts = message.Split('_');
                int ID = int.Parse(parts[1]);
                chatWindows[ID].CheckArgument(parts[2]);
            }

            public void SendMessage(string target, string message)
            {
                parent.SendMessage(target, message);
            }

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

            public void messageDropped(Message message)
            {
                string mes = message.payload;
                int[] ID = extractID(mes);
                chatWindows[ID[0]].MessageDropped(ID[1]);
            }

            public List<string> getKnownShips()
            {
                return knownShips;
            }

            public void run()
            {
                foreach (ChatModule chat in chatWindows.Values.ToList())
                {
                    chat.Run();
                }
                foreach (string name in requests.Keys.ToList())
                {
                    foreach (RequestSave rs in requests[name])
                    {
                        if (rs.requestsOpen <= 0)
                        {
                            requests[name].Remove(rs);
                            string mes = "Chat/ReqDec/" + rs.targetID;
                            parent.comHandler.SendMessage(rs.targetName, mes, true);
                        }
                    }
                    if (requests[name].Count <= 0)
                    {
                        requests.Remove(name);
                    }
                }
            }

            RequestSave getRequestFromID(string target, int requestID)
            {
                RequestSave result = new RequestSave();
                result.targetID = -1;
                List<RequestSave> list = requests[target];
                foreach (RequestSave rs in list)
                {
                    if (rs.isEqual(requestID))
                    {
                        result = rs;
                        break;
                    }
                }
                return result;
            }

            int[] extractID(string mes)
            {
                string[] parts = mes.Split('/');
                int[] output = { 0, 0 };
                output[0] = int.Parse(parts[2]);
                output[1] = int.Parse(parts[3]);
                return output;
            }
        }
    }
}
