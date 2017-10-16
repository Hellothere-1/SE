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
        public enum Tag { MES, RES};

        [Flags]
        public enum Status { Dead = 0, SendACK = 1, Activ = 2, MesNotACK = 4}
        

        class Message
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

            public string ToString(string ownName = "")
            {
                string mes = "COM_" + tag + "_" + targetName;
                mes = mes + "_" + ID;
                if (tag == Tag.RES)
                {
                    return mes;
                }
                mes = mes + "_" + ownName;
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

            public Target(string name)
            {
                Random rnd = new Random(name.GetHashCode());
                currentID = rnd.Next();
            }


            public Status isAlive()
            {
                Status output = Status.Dead;
                if (sendBuffer.Count != 0 || responceNeeded)
                {
                    output = Status.Activ;
                }
                TARcounter--;
                if (TARcounter <= 0 || ACKneeded)
                {
                    output = Status.Activ;
                }
                responceTime--;
                if (responceTime <= 0 && responceNeeded)
                {
                    output = output | Status.MesNotACK;
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
                            sendBuffer.RemoveAt(i);
                            pointer--;
                        }
                    }
                    pointer = 0;
                }
                ACKcounter--;
                if (ACKcounter >= 0)
                {
                    output = output | Status.SendACK;
                    ACKneeded = false;
                }
                return output;
            }

            public void deleteMessage(Message mes)
            {
                sendBuffer.Remove(mes);
                pointer--;
            }

            public void addMessage(Message mes)
            {
                mes.ID = currentID;
                currentID++;
                sendBuffer.Add(mes);
            }

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
                foreach (Message mes in sendBuffer)
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
                    lastRecievedID = 0;
                    awaitedID = 0;
                    lastACKedID = 0;
                    ACKneeded = false;
                    return true;
                }
                TARcounter = 3 * MAXACKTIME;
                ACKneeded = false;
                return false;
            }
        }


        public class ComModule
        {
            enum Part { COM, KIND, TARGET, ID, SENDER, MESSAGE };

            /*
             *  Accepted Formats 
             *  KEY kann weg, idee zur überprüfung für gesichter kommunikation mit einmaliger anmeldung
             *  COM_MES_target_id_sender_message
             *  COM_RES_target_id_sender
            */

            Program parent;
            IMyRadioAntenna antenna;

            Dictionary<string, Target> responceList = new Dictionary<string, Target>();
            
            string ownName;
            int RTT = 15;
            int RETRY = 3;

            bool ComWorking = false;

            public ComModule(Program par, IMyRadioAntenna ant, string name)
            {
                parent = par;
                antenna = ant;
                ownName = name;
                init();
            }

            private void init()
            {
                antenna.Enabled = true;
                if (antenna.TransmitMessage("Init message", MyTransmitTarget.Owned))
                {
                    parent.Echo("Com System online");
                    ComWorking = true;
                }
                else
                {
                    parent.Echo("Com System failure");
                    return;
                }

            }

            public void Run()
            {
                if (!ComWorking)
                {
                    return;
                }
                if (responceList.Count == 0)
                {
                    parent.Echo("Nothing to do");
                    return;
                }
                Target current = responceList[responceList.Keys.First()];
                Message mes = current.getMessage();
                foreach (string name in responceList.Keys.ToList())
                {
                    Status stat = responceList[name].isAlive();
                    if ((stat & Status.Dead) == stat)
                    {
                        //Not activ anymore
                        responceList.Remove(name);
                    }
                    if ((stat & Status.SendACK) == stat)
                    {
                        mes = responceList[name].getMessage();
                        //ACK needed
                    }
                }
                if (mes != null && antenna.TransmitMessage(mes.ToString(), mes.targetGroup))
                {
                    parent.output.WritePublicText("Message send to " + mes.targetName + " with ID " + mes.ID + "\n", true);
                    current.increasePointer();
                    if (mes.tag != Tag.MES)
                    {
                        current.deleteMessage(mes);
                    }
                    parent.output.WritePublicText("Message send completed \n", true);
                }
                
            }

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
                                parent.output.WritePublicText("Recieved Reponse for message(s) with ID " + parts[(int)Part.ID] + "\n", true);
                                if (responceList[parts[(int)Part.SENDER]].recieveACK(int.Parse(parts[(int)Part.ID])))
                                {
                                    //End of communication reached
                                    responceList.Remove(parts[(int)Part.SENDER]);
                                    parent.output.WritePublicText("Communication with " + parts[(int)Part.SENDER] + " completed, all Data transmitted\n");
                                }
                            }
                            break;
                        case Tag.MES:
                            if (parts[2] == ownName)
                            {
                                parent.output.WritePublicText("Recieved message from " + parts[(int)Part.SENDER] + "with ID " + parts[(int)Part.ID] + "\n", true);
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
                    }
                }
                catch (Exception)
                {
                    parent.output.WritePublicText("Bad command recieved: " + message + "\n", true);
                }
                return output;
            }
            
            void SendResponce(string target, int ID, MyTransmitTarget group = MyTransmitTarget.Ally|MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.RES, target, "",  ID, group);
                }
            }

            public void SendMessage(string target, string message, string key = "", MyTransmitTarget group = MyTransmitTarget.Ally | MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    if (responceList.Count == 0 || !responceList.Keys.Contains(target))
                    {
                        responceList.Add(target, new Target(target));
                    }
                    Message mes = new Message(Tag.MES, target, message, 0, group);
                    responceList[target].addMessage(mes);
                }
            }
        }
    }
}
