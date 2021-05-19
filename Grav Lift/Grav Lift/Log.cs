using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public static class Log
        {
            static int tick = 0;
            static readonly string[] indicators = new string[4] { " /", "---", " \\", " |" };
            public static Program program;
            public static bool clearAfter = false;

            static Queue<TimedMessage> timedMessages = new Queue<TimedMessage>();
            public static List<string> singleFrameMessages = new List<string>();

            struct TimedMessage
            {
                public DateTime time;
                public string message;

                public TimedMessage (string message)
                {
                    this.time = DateTime.Now;
                    this.message = message;
                }

                public bool HasExpired(float expiringTime)
                {
                    return (DateTime.Now - time).TotalSeconds > expiringTime;
                }
            }

            public static void Tick()
            {
                tick++;
                program.Echo(indicators[tick % 4]);

                foreach(string s in singleFrameMessages)
                {
                    program.Echo(s);
                }

                program.Echo("\nLog:");

                foreach(TimedMessage message in timedMessages)
                {
                    program.Echo(message.message);
                }
                if (clearAfter)
                {
                    timedMessages.Clear();
                    clearAfter = false;
                }
                else
                {
                    while (timedMessages.Count > 0 && timedMessages.Peek().HasExpired(10))
                    {
                        timedMessages.Dequeue();
                    }
                }

                singleFrameMessages.Clear();
            }

            public static void Clear()
            {
                timedMessages.Clear();
                singleFrameMessages.Clear();
            }

            public static void AppendLog(string message)
            {
                timedMessages.Enqueue(new TimedMessage(message));
            }
        }
    }
}
