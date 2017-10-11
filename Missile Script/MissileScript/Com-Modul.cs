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
        enum Tag { MES, RES, BAD };



        class Message
        {
            public int ID;
            public Tag tag;
            public int tick;
            public string Skey;
            public string payload;
            public string targetName;
            public MyTransmitTarget targetGroup;

            public Message(Tag kind, string tar, string load, int id, string key, MyTransmitTarget group)
            {
                tick = 0;
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
        }


        public class ComModule
        {
           

            /*
             *  Accepted Formats 
             *  COM_MES_target_id_key_message
             *  COM_MES_target_id_message
             *  COM_RES_target_id_message
             *  COM_RES_target_id
             *  COM_BAD_target
            */

            Program parent;
            IMyRadioAntenna antenna;

            List<Message> buffer = new List<Message>();
            int pointer;
            int currentID;

            int RTT = 15;

            bool ComWorking = false;

            public ComModule(Program par, IMyRadioAntenna ant)
            {
                parent = par;
                antenna = ant;
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
                currentID = 0;
            }

            void Run()
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
                    string message = BuildMessage(buffer[pointer]);
                    if (antenna.TransmitMessage(message, buffer[pointer].targetGroup))
                    {
                        pointer++;
                    }
                }

                for (int i = 0; i < pointer; i++)
                {
                    if (buffer[i].tick < RTT)
                    {
                        buffer[i].tick++;
                    }
                    else
                    {
                        Message save = buffer[i];
                        save.tick = 0;
                        buffer.RemoveAt(i);
                        buffer.Add(save);
                    }
                }
            }

            string ProcessMessage(string message)
            {
                string[] parts = message.Split('_');
                try
                {
                    Tag kindOf = Enum.Parse(typeof(Tag), parts[1]);
                }
            }

            string BuildMessage(Message obj)
            {
                string mes = "COM_" + obj.tag + "_" + obj.targetName;
                if (obj.tag == Tag.BAD)
                {
                    return mes;
                }
                mes = mes + "_" + obj.ID;
                if (obj.Skey != "")
                {
                    if (obj.payload != "")
                    {
                        mes = mes + "_" + obj.Skey;
                        mes = mes + "_" + obj.payload;
                    }
                }
                if (obj.payload != "")
                {
                    mes = mes + "_" + obj.payload;
                }
                return mes;
            }
            

            bool SendMessage(Tag tag, string target, string message, string key = "", MyTransmitTarget group = MyTransmitTarget.Default)
            {
                if (ComWorking)
                {
                    Message mes = new Message(tag, target, message, currentID, key, group);
                    buffer.Add(mes);
                    currentID++;
                    return true;
                }
                return false;
            }
        }
    }
}
