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
        public enum Tag { MES, RES, BAD };



        class Message
        {
            public int ID;
            public int round;
            public Tag tag;
            public int tick;
            public string Skey;
            public string payload;
            public string targetName;
            public MyTransmitTarget targetGroup;

            public Message(Tag kind, string tar, string load, int id, string key, MyTransmitTarget group)
            {
                tick = 0;
                round = 0;
                tag = kind;
                targetName = tar;
                payload = load;
                ID = id;
                Skey = key;
                targetGroup = group;
            }

            public bool isEqual(int id)
            {
                return id == ID;
            }

            public string ToString(string ownName = "")
            {
                string mes = "COM_" + tag + "_" + targetName;
                if (tag == Tag.BAD)
                {
                    return mes;
                }
                mes = mes + "_" + ID;
                if (tag == Tag.RES)
                {
                    return mes;
                }
                mes = mes + "_" + ownName;
                if (Skey != "")
                {
                    if (payload != "")
                    {
                        mes = mes + "_" + Skey;
                        mes = mes + "_" + payload;
                    }
                }
                if (payload != "")
                {
                    mes = mes + "_" + payload;
                }
                return mes;
            }
        }


        public class ComModule
        {
            enum Part {COM, KIND, TARGET, ID, SENDER, KEY, MESSAGE };

            /*
             *  Accepted Formats 
             *  COM_MES_target_id_sender_key_message
             *  COM_MES_target_id_sender_message
             *  COM_RES_target_id
             *  COM_BAD_target
             *  
             *  
             *  Idea : Make Dict <target, list<messages>
             *  Sort list after ID 
             *  Mes with ID_M ack all messages with ID < ID_M
             *  (Commulative ACKs)
             *  
            */

            Program parent;
            IMyRadioAntenna antenna;

            List<Message> buffer = new List<Message>();
            int pointer;
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

                if (buffer.Count <= pointer)
                {
                    pointer = buffer.Count;
                }

                if (buffer.Count > pointer)
                {
                    string message = buffer[pointer].ToString(ownName);
                    if (antenna.TransmitMessage(message, buffer[pointer].targetGroup))
                    {
                        parent.output.WritePublicText("Message send to " + buffer[pointer].targetName + " with ID " + buffer[pointer].ID + "\n", true);
                        pointer++;
                        if (buffer[pointer-1].tag != Tag.MES)
                        {
                            pointer--;
                            buffer.RemoveAt(pointer);
                        }
                    }
                }
                parent.Echo("Pointer " + pointer + " Buffer " + buffer.Count);
                for (int i = 0; i < pointer; i++)
                {
                    if (buffer[i].tick < RTT)
                    {
                        parent.Echo("Tick at " + buffer[i].tick);
                        buffer[i].tick++;
                    }
                    else
                    {
                        parent.output.WritePublicText("No Responce for message with ID " + buffer[i].ID + ", retrying \n", true);
                        RepeatMessage(i);
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
                        case Tag.BAD:
                            CheckMessage(parts[(int) Part.TARGET]);
                            parent.output.WritePublicText("Recieved message with BAD tag from " + parts[(int) Part.TARGET] + "\n", true);
                            break;
                        case Tag.RES:
                            if (parts[2] == ownName)
                            {
                                MessageResponce(int.Parse(parts[(int) Part.ID]));
                                parent.output.WritePublicText("Recieved Reponse for message with ID " + parts[(int) Part.ID] + "\n", true);
                            }
                            break;
                        case Tag.MES:
                            if (parts[2] == ownName)
                            {
                                parent.output.WritePublicText("Recieved message from " + parts[(int) Part.SENDER] + "with ID " + parts[(int) Part.ID]+ "\n", true);
                                SendResponce(parts[(int) Part.SENDER], int.Parse(parts[(int) Part.ID]));
                                output = parts[(int) Part.KEY];
                                if (parts.Length > 6)
                                {
                                    output = output + "_" + parts[(int) Part.MESSAGE];
                                }
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    SendError();
                }
                return output;
            }

            void MessageResponce(int ID)
            {
                foreach (Message obj in buffer)
                {
                    if (obj.isEqual(ID))
                    {
                        buffer.Remove(obj);
                        pointer--;
                        break;
                    }
                }
            }

            void CheckMessage(string target)
            {
                for (int i = 0; i < pointer; i++)
                {
                    if (buffer[i].targetName == target)
                    {
                        RepeatMessage(i);
                    }
                }
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

            

            void SendError()
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.BAD, ownName, "", -1, "", MyTransmitTarget.Ally|MyTransmitTarget.Owned);

                }
            }

            void SendResponce(string target, int ID, MyTransmitTarget group = MyTransmitTarget.Ally|MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.RES, target, "", ID, "", group);
                    buffer.Add(mes);
                }
            }

            public void SendMessage(string target, string message, string key = "", MyTransmitTarget group = MyTransmitTarget.Ally | MyTransmitTarget.Owned)
            {
                if (ComWorking)
                {
                    Message mes = new Message(Tag.MES, target, message, currentID, key, group);
                    buffer.Add(mes);
                    currentID++;
                }
            }
        }
    }
}
