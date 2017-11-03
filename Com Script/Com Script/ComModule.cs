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
        public enum Tag { MES, RES, HEY, CHAT};

        [Flags]
        public enum Status {SendACK = 1, Activ = 2, MesNotSend = 4, Dead = 8 }
        

        public class Message
        {
            public int ID;
            public int round;
            public Tag tag;
            public int tick;
            public string payload;
            public string targetName;
            public MyTransmitTarget targetGroup;
            public Message(Tag kind, string tar, string load, int id, MyTransmitTarget group)
            {
                tick = 0;
                round = 0;
                tag = kind;
                targetName = tar;
                payload = load;
                ID = id;
                targetGroup = group;
            }

            public bool isEqual(int id)
            {
                return id == ID;
            }

            public string ToString(string ownName)
            {
                string mes = "COM_" + tag + "_" + targetName;
                if (tag == Tag.HEY)
                {
                    return mes;
                }
                mes = mes + "_" + ID;
                mes = mes + "_" + ownName;
                if (tag == Tag.RES)
                {
                    return mes;
                }
                if (payload != "")
                {
                    mes = mes + "_" + payload;
                }
                return mes;
            }
        }


        class Target
        {
            const int MAXACKTIME = 5;
            const int MAXROUND = 3;
            int currentID;

            //List to save all not ACKed Messages
            List<Message> sendBuffer = new List<Message>();
            //Last message which could not be send
            Message lastDropped = null;
            //Indicates wether a responce is needed right now or not
            bool responceNeeded = false;
            //Indicates the time in ticks until responce should be recieved
            int responceTime = MAXACKTIME + (int)(0.2F * (float)MAXACKTIME);
            //Indicates the position in the sendBuffer list
            int pointer = 0;
            //ID which indentifies the last message which has been recieved from the sender
            int lastRecievedID = 0;
            //ID which indentifies the last message which has been acknowledged by this reciever
            int lastACKedID = 0;
            //ID which indentifies the ID which the next message should have
            int awaitedID = 0;
            //Indicates whether a own responce is needed 
            bool ACKneeded = false;
            //Indicates the time in ticks until a new ACK should be send
            int ACKcounter = MAXACKTIME;
            //Indicates the time since last operation happend, if 0 delete this object
            int TARcounter = 3 * MAXACKTIME;

            //Constructor, name not necessary, a long string will do it too
            public Target(string name)
            {
                Random rnd = new Random(name.GetHashCode());
                currentID = rnd.Next();
            }

            //Return the Status of this target
            public Status isAlive()
            {
                Status output = Status.Dead;
                responceTime--;
                if (sendBuffer.Count != 0 || responceNeeded || ACKneeded)
                {
                    TARcounter = 3 * MAXACKTIME;
                    output = Status.Activ;
                }
                TARcounter--;
                if (TARcounter > 0)
                {
                    output = Status.Activ;
                }
                ACKcounter--;
                if (ACKcounter <= 0 && ACKneeded)
                {
                    output |= Status.SendACK;
                    ACKneeded = false;
                }
                if (responceTime <= 0 && responceNeeded)
                {
                    for (int i = 0; i < pointer; i++)
                    {
                        sendBuffer[i].round++;
                        if (sendBuffer[i].round < MAXROUND)
                        {
                            Message save = sendBuffer[i];
                            sendBuffer.RemoveAt(i);
                            pointer--;
                            sendBuffer.Add(save);
                        }
                        else
                        {
                            output |= Status.MesNotSend;
                            lastDropped = sendBuffer[i];
                            sendBuffer.RemoveAt(i);
                            pointer--;
                        }
                    }
                    if (sendBuffer.Count == 0)
                    {
                        responceNeeded = false;
                    }
                    pointer = 0;
                }
                return output;
            }
            
            //Adds the given message to the sendBuffer
            public int addMessage(Message mes)
            {
                mes.ID = currentID;
                currentID++;
                sendBuffer.Add(mes);
                return currentID;
            }

            //Gets the first message for this target
            public Message getMessage()
            {
                if (sendBuffer.Count != 0 && pointer < sendBuffer.Count)
                {
                    TARcounter = 3 * MAXACKTIME;
                    return sendBuffer[pointer];
                }
                return null;
            }

            //Called when Antenna confirmed send operation
            public void increasePointer()
            {
                pointer++;
                responceNeeded = true;
                responceTime = MAXACKTIME + (int) (0.2F * (float) MAXACKTIME);
                TARcounter = 3 * MAXACKTIME;
            }

            //Called when Controller recieved a Message
            public int checkRecieved(int ID)
            {
                TARcounter = 3 * MAXACKTIME;
                if (awaitedID == 0)
                {
                    //Restart or start of communication, reinit variables
                    ACKneeded = true;
                    lastRecievedID = ID;
                    awaitedID = ID + 1;
                    ACKcounter = MAXACKTIME;
                    return 0;
                }
                if (ID == awaitedID)
                {
                    //Message like awaited, no errors while communication
                    ACKneeded = true;
                    lastRecievedID = ID;
                    awaitedID++;
                    ACKcounter = MAXACKTIME;
                    return 0;
                }
                if (ID > awaitedID)
                {
                    //One or more messages lost, give back last recieved ID when it is not already ACKed
                    if (lastRecievedID != lastACKedID)
                    {
                        ACKneeded = false;
                        return lastRecievedID;
                    }
                }
                return -1;
            }

            //Called when controller recieves an ack/Responce from other participant
            public bool recieveACK(int ID)
            {
                
                TARcounter = 3 * MAXACKTIME;
                //Delete all ACKed Messages
                foreach (Message mes in sendBuffer.ToList())
                {
                    if (mes.ID <= ID)
                    {
                        sendBuffer.Remove(mes);
                        pointer--;
                    }
                }
                if (pointer <= 0)
                {
                    pointer = 0;
                }
                if (sendBuffer.Count == 0 || pointer == 0)
                {
                    responceNeeded = false;
                    return true;
                }
                return false;
            }

            //Called when controller sends an ack to other participant
            public bool sendACK(int ID, bool forced)
            {
                if (ID > lastACKedID)
                {
                    lastACKedID = ID;
                }
                if (!forced && lastACKedID == lastRecievedID)
                {
                    //Communication endet successful, reseting variables
                    /*When Communication is completed this object will be destroyed
                    lastRecievedID = 0;
                    awaitedID = 0;
                    lastACKedID = 0;*/
                    ACKneeded = false;
                    return true;
                }
                TARcounter = 3 * MAXACKTIME;
                ACKneeded = false;
                return false;
            }

            //Return the last recieved message ID
            public int getLastRecieved()
            {
                return lastRecievedID;
            }

            public Message getLastDropped()
            {
                return lastDropped;
            }
        }


        public class ComModule
        {
            enum Part { COM, KIND, TARGET, ID, SENDER, MESSAGE };
            
            public Program parent;
            IMyRadioAntenna antenna;
            Dictionary<string, Target> responceList = new Dictionary<string, Target>();
            Dictionary<string, int> knownContacts = new Dictionary<string, int>();
            List<Message> prioList = new List<Message>();
            string ownName;
            int timeToContactLoss = 10;
            int antennaCounter = 0;
            bool ComWorking = false;
            bool antennaAlwaysOn = false;

            //SCRIPTINPUT (FS)
            public ComModule(Program par, IMyRadioAntenna ant, string name, bool antennaOn)
            {
                parent = par;
                antenna = ant;
                ownName = name;
                antennaAlwaysOn = antennaOn;
                init();
            }

            //SCRIPTINPUT (FS)
            void init()
            {
                antenna.Enabled = true;
                antenna.SetValue("EnableBroadCast", true);
                if (antenna.TransmitMessage("Init message", MyTransmitTarget.Owned))
                {
                    parent.Echo("Com System online");
                    ComWorking = true;
                }
                if (!antennaAlwaysOn)
                {
                    antenna.SetValue("EnableBroadCast", false);
                }
                
            }

            //NO INPUT (FS)
            public void Run()
            {
                if (!ComWorking)
                {
                    return;
                }
                if (responceList.Count == 0 && prioList.Count == 0)
                {
                    if (!antennaAlwaysOn && antenna.IsBroadcasting)
                    {
                        antenna.SetValue("EnableBroadCast", false);
                        antennaCounter = 0;
                    }
                    return;
                }
                bool priolist = false;
                Target current = null;
                Message mes = null;
                if (responceList.Keys.Count != 0)
                {
                    current = responceList[responceList.Keys.First()];
                    mes = current.getMessage();
                }
                foreach (string name in responceList.Keys.ToList())
                {
                    Status stat = responceList[name].isAlive();
                    parent.printOut("Stats: " + stat);
                    if ((stat & Status.Dead) == Status.Dead)
                    {
                        //Not activ anymore
                        responceList.Remove(name);
                    }
                    if ((stat & Status.SendACK) == Status.SendACK)
                    {
                        //Create new Responce for given ID
                        Message resp = new Message(Tag.RES, name, "", responceList[name].getLastRecieved(), MyTransmitTarget.Default);
                        prioList.Add(resp);
                    }
                    if ((stat & Status.MesNotSend) == Status.MesNotSend)
                    {
                        Message message = responceList[name].getLastDropped();
                        parent.printOut("MESDROPPED/" + message.targetName + "/" + message.ID);
                        parent.chathandler.MessageDropped(responceList[name].getLastDropped());
                    }
                }
                if (prioList.Count != 0)
                {
                    mes = prioList[0];
                    priolist = true;
                }
                if (mes != null && antenna.IsBroadcasting && antennaCounter > 3 && antenna.TransmitMessage(mes.ToString(ownName), mes.targetGroup))
                {
                    parent.printOut("Message send : " + mes.ToString(ownName));
                    if ((mes.tag == Tag.MES || mes.tag == Tag.CHAT) && !priolist)
                    {
                        current.increasePointer();
                    }
                    else if (mes.tag == Tag.RES && priolist)
                    {
                        responceList[mes.targetName].sendACK(mes.ID, false);
                    }
                    if (priolist)
                    {
                        prioList.RemoveAt(0);
                    }
                }
                else
                {
                    if (!antenna.IsBroadcasting)
                    {
                        antenna.SetValue("EnableBroadCast", true);
                    }
                    antennaCounter++;
                }
                
            }

            //USERINPUT (FS)
            public string ProcessMessage(string message)
            {
                string[] parts = message.Split('_');
                string output = "";
                try
                {
                    Tag kindOf = (Tag)Enum.Parse(typeof(Tag), parts[(int) Part.KIND]);
                    switch (kindOf)
                    {
                        case Tag.RES:
                            if (parts[(int) Part.TARGET] == ownName || responceList.Keys.Contains(parts[(int) Part.SENDER]))
                            {
                                parent.printOut("Recieved Reponse for message(s) with ID " + parts[(int)Part.ID]);
                                if (responceList[parts[(int)Part.SENDER]].recieveACK(int.Parse(parts[(int)Part.ID])))
                                {
                                    //End of communication reached, but remove will be called by stat = DEAD
                                    parent.printOut("Communication with " + parts[(int)Part.SENDER] + " completed, all Data transmitted");
                                }
                            }
                            break;
                        case Tag.MES | Tag.CHAT:
                            if (parts[2] == ownName)
                            {
                                parent.printOut("Recieved message from " + parts[(int)Part.SENDER] + " with ID " + parts[(int)Part.ID]);
                                if (responceList.Count == 0 || !responceList.Keys.Contains(parts[(int)Part.SENDER]))
                                {
                                    responceList.Add(parts[(int)Part.SENDER], new Target(parts[(int) Part.SENDER]));
                                }
                                int status = responceList[parts[(int)Part.SENDER]].checkRecieved(int.Parse(parts[(int)Part.ID]));
                                if (status != 0 && status != -1)
                                {
                                    //Wrong ID recieved, message lost or else, ack last accepted message
                                    responceList[parts[(int)Part.SENDER]].sendACK(status, true);
                                    SendResponce(parts[(int)Part.SENDER], status);
                                }
                                if (status == 0)
                                {
                                    output = parts[(int)Part.MESSAGE];
                                }
                            }
                            break;
                        case Tag.HEY:
                            if (!knownContacts.Keys.Contains(parts[2]))
                            {
                                knownContacts.Add(parts[2], 0);
                                parent.chathandler.updateShip(parts[2], false);
                            }
                            knownContacts[parts[2]] = 0;
                            //parent.printOut("HEY from " + parts[2] + " recieved");
                            break;
                    }
                }
                catch (Exception)
                {
                    parent.printOut("Bad command recieved: " + message);
                }
                return output;
            }
            
            //SCRIPTINPUT (FS)
            void SendResponce(string target, int ID, MyTransmitTarget group = MyTransmitTarget.Ally|MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.RES, target, "",  ID, group);
                    prioList.Add(mes);
                }
            }

            //USERINPUT (FS)
            public int SendMessage(string target, string message, bool chat, MyTransmitTarget group = MyTransmitTarget.Default)
            {
                if (ComWorking)
                {
                    if (responceList.Count == 0 || !responceList.Keys.Contains(target))
                    {
                        responceList.Add(target, new Target(target));
                    }
                    Message mes = new Message(Tag.MES, target, message, 0, group);
                    if (chat)
                    {
                        mes.tag = Tag.CHAT;
                    }
                    parent.printOut("Message added");
                    return responceList[target].addMessage(mes);
                }
                parent.printOut("Message not added");
                return -1;
            }

            //NO INPUT (FS)
            public void SendHey()
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.HEY, ownName, "", 0, MyTransmitTarget.Default);
                    prioList.Add(mes);
                    foreach (string name in knownContacts.Keys.ToList())
                    {
                        if (knownContacts[name] > timeToContactLoss)
                        {
                            knownContacts.Remove(name);
                            parent.chathandler.updateShip(name, true);
                        }
                        else
                        {
                            knownContacts[name]++;
                        }
                    }
                }
            }
        }
    }
}
