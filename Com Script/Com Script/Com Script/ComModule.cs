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
            //List to save all not ACKed Messages
            List<Message> sendBuffer = new List<Message>();
            int responceTime = 4;
            int pointer = 0;

            //ID which indentifies the last message which has been recieved from the sender
            int lastRecievedID = 0;

            //ID which indentifies the last message which has been acknowledged by this reciever
            int lastACKedID = 0;
            
            //ID which indentifies the ID which the next message should have
            int awaitedID = 0;

            int ACKcounter = MAXACKTIME;
            int TARcounter = 3 * MAXACKTIME;

            public void deleteMessage(Message mes)
            {
                sendBuffer.Remove(mes);
                pointer--;
            }

            public void addMessage(Message mes)
            {
                sendBuffer.Add(mes);
            }

            public Message getMessage()
            {
                if (sendBuffer.Count != 0)
                {
                    return sendBuffer[pointer];
                }
                return null;
            }

            public void increasePointer()
            {
                pointer++;
            }

            public int checkRecieved(int ID)
            {
                TARcounter = 3 * MAXACKTIME;
                if (awaitedID == 0)
                {
                    //Restart or start of communication, reinit variables
                    lastRecievedID = ID;
                    awaitedID = ID + 1;
                    ACKcounter = MAXACKTIME;
                    return 0;
                }
                if (ID == awaitedID)
                {
                    //Message like awaited, no errors while communication
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
                        return lastRecievedID;
                    }
                }
                return -1;
            }

            //This unit is recieving an ACK Signal
            public bool recieveACK(int ID)
            {
                //Delete all ACKed Messages
                foreach (Message mes in sendBuffer)
                {
                    if (mes.ID <= ID)
                    {
                        sendBuffer.Remove(mes);
                        pointer--;
                    }
                }
                TARcounter = 3 * MAXACKTIME;
                return false;
            }

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
                    return true;
                }
                TARcounter = 3 * MAXACKTIME;
                return false;
            }

            public bool responceNeeded()
            {
                if (ACKcounter >= 0)
                {
                    return true;
                }
                ACKcounter--;
                return false;
            }

            public bool needACK()
            {
                responceTime--;
                if (responceTime <= 0)
                {
                    return true;
                }
                return false;
            }

            public bool activCounter()
            {
                TARcounter--;
                if (TARcounter <= 0 && sendBuffer.Count <= 0)
                {
                    return true;
                }
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
             *  COM_RES_target_id
             *  
             *  
             *  Idea : Make Dict <target, list<messages>
             *  Sort list after ID 
             *  Mes with ID_M ack all messages with ID < ID_M
             *  (Commulative ACKs)
             *  
             *  
            */

            Program parent;
            IMyRadioAntenna antenna;

            List<Message> buffer = new List<Message>();
            int pointer;

            Dictionary<string, Target> responceList = new Dictionary<string, Target>();

            int currentID;
            string ownName;
            int RTT = 15;
            int RETRY = 3;

            bool ComWorking = false;
            Random rnd;

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
                pointer = 0;
                rnd = new Random(antenna.CustomNameWithFaction.GetHashCode());
                currentID = (int)(rnd.NextDouble() * rnd.Next());

            }

            public void Run()
            {
                if (!ComWorking)
                {
                    return;
                }
                Target current = responceList[responceList.Keys.First()];
                Message mes = current.getMessage();
                if (antenna.TransmitMessage(mes.ToString(), buffer[pointer].targetGroup))
                {
                    parent.output.WritePublicText("Message send to " + mes.targetName + " with ID " + mes.ID + "\n", true);
                    current.increasePointer();
                    if (mes.tag != Tag.MES)
                    {
                        current.deleteMessage(mes);
                    }
                }
                foreach (string name in responceList.Keys.ToList())
                {
                    if (responceList[name].activCounter())
                    {
                        //Not activ anymore
                        responceList.Remove(name);
                    }
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
                                if (!responceList.Keys.Contains(parts[(int)Part.SENDER]))
                                {
                                    responceList.Add(parts[(int)Part.SENDER], new Target());
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

            void RepeatMessage(int index)
            {
                Message save = buffer[index];
                save.tick = 0;
                save.round++;
                buffer.RemoveAt(index);
                if (save.round >= RETRY)
                {
                    parent.output.WritePublicText("Message droped after " + RETRY + " retries, ID " + save.ID + "\n", true);
                    return;
                }
                buffer.Add(save);
                pointer--;
            }
            

            void SendResponce(string target, int ID, MyTransmitTarget group = MyTransmitTarget.Ally|MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.RES, target, "",  ID, group);
                    buffer.Add(mes);
                }
            }

            public void SendMessage(string target, string message, string key = "", MyTransmitTarget group = MyTransmitTarget.Ally | MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.MES, target, message, currentID, group);
                    buffer.Add(mes);
                    currentID++;
                }
            }
        }
    }
}
