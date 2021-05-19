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
using System.Runtime;

namespace IngameScript
{
    partial class Program
    {
        public static class CustomDataReader
        {
            static List<bool> found = new List<bool>();

            public static void ReadData(IMyTerminalBlock block, string[] properties, string[] commentary, float[] baseValues, float[]outputValues)
            {
                string[] data = block.CustomData.Split('\n');
                bool[] found = new bool[properties.Length];
                StringBuilder s = new StringBuilder();

                bool changes = false;

                for (int line = 0; line < data.Length; line++)
                {
                    if (data[line] == "" || data[line].StartsWith("//"))
                    {
                        continue;
                    }

                    for (int i = 0; i < properties.Length; i++)
                    {
                        if (!found[i])
                        {
                            if (data[line].StartsWith(properties[i]))
                            {
                                if (!float.TryParse(data[line].Substring(properties[i].Length).Replace(" ", ""), out outputValues[i]))
                                {
                                    data[line] = properties[i] + " " + baseValues[i];
                                    outputValues[i] = baseValues[i];
                                    changes = true;
                                }
                                found[i] = true;
                                break;
                            }
                        }
                    }
                }

                if (changes)
                {
                    foreach (string line in data)
                    {
                        s.AppendLine(line);
                    }
                }
                else
                {
                    s.Append(block.CustomData);
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    if (!found[i])
                    {
                        outputValues[i] = baseValues[i];
                        if (commentary[i] != "")
                        {
                            s.AppendLine(commentary[i]);
                        }
                        s.AppendLine(properties[i] + " " + baseValues[i]);
                    }
                }

                block.CustomData = s.ToString();
            }
        }
    }
}
