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
        public class Launch
        {
            /*
                * ACCEPTED INPUT:
                * 
                * BURN <Block> <value>
                * TURN <Degree>
                * WAIT <Condition>
                * LOCK <Target>/<Method>
                * WHEN <Condition>
                * SET <Block> <ON/OFF>
                * STOP
                * 
                * <Conditions> :
                * - TIME <time>(<time> is float)
                * - PGRA <amount> <op> (<amount> is float, <op> one of: EQUAL, MORE, LESS (default EQUAL))  PGRA = Planetary Gravity Amount
                * - AGRA <amount> <op> (see above)                                                          AGRA = Artificial Gravity Amount
                * - SPEED <velocity> <op> (<velocity> is float, op from above)
                * - LOCATION <GPS> (<GPS> is world matrix coordinates, format (x:y:z))
                * - NONE (makes no sense for WAIT but everything else)
                * - more if needed
                * 
                * <Block> :
                * - {<name>}
                * - {GR:<groupname>}
                * - {<name>, <name2>, ...} (Several Groups and names)
                * 
                * <Degree> :
                * - DEG(azimuth, elevation, roll)
                * - <Direction> <Degrees> (<direction> := UP, DOWN, RIGHT, LEFT, FORWARD, BACKWARD, <degrees> float)
                * - GPS(x:y:z)
                * 
                * <Target> :
                * - {x:y:z} (use only for static targets)
                * 
                * <Method> :
                * - SENSOR <Block> <Shipname> (<Shipname> is the string of the ship from what to get the target)
                * - ANTENNA <Block> <Shipname>
                * 
                * 
                * Developer dreams (Feature creep)
                * 
                * SYNC (sync volley of missiles for timed hits)
                * 
                * 
                * 
                 */

            enum LaunchOrders { BURN, TURN, WAIT, LOCK, WHEN, SET, STOP };
            enum Conditions { TIME, PGRA, AGRA, SPEED, LOCATION, NONE };
            enum Operators { EQUAL, MORE, LESS };
            LaunchOrders currentCommand;
            Conditions currentCondition;
            Operators currentOperator;
            Side currentSide;
            List<IMyTerminalBlock> currentBlocks = new List<IMyTerminalBlock>();
            float value = 0;
            Vector3D conditionGPS = new Vector3D();
            Vector3D degree = new Vector3D();
            long time = -1;
            bool setOn;

            string currentLaunchOrder = "";

            Program mainProgram;
            IMyRemoteControl control;

            public Launch(Program par, IMyRemoteControl control)
            {
                mainProgram = par;
                this.control = control;
            }

            public bool executeLaunchSequence()
            {
                if (currentLaunchOrder == "")
                {
                    if (ExtractNextLaunchOrdner())
                    {
                        //End of launch sequence
                        //Switch to flight mode
                        return true;
                    }
                    if (!ParseLaunchOrder(currentLaunchOrder))
                    {
                        //Could not be parsed
                        currentLaunchOrder = "";
                    }
                }
                switch (currentCommand)
                {
                    case LaunchOrders.BURN:
                        foreach (IMyTerminalBlock block in currentBlocks)
                        {
                            try
                            {
                                IMyThrust thrust = block as IMyThrust;
                                thrust.SetValueFloat("Override", value * thrust.MaxThrust);
                            }
                            catch (Exception)
                            {
                                mainProgram.Echo(block.CustomName + " is not a thruster");
                            }
                        }
                        currentLaunchOrder = "";
                        break;
                    case LaunchOrders.SET:
                        foreach (IMyTerminalBlock block in currentBlocks)
                        {
                            try
                            {
                                IMyFunctionalBlock funcBlock = block as IMyThrust;
                                funcBlock.Enabled = setOn;
                            }
                            catch (Exception)
                            {
                                mainProgram.Echo(block.CustomName + " is not a functional block (cannot be turned on/off)");
                            }
                        }
                        currentLaunchOrder = "";
                        break;
                    case LaunchOrders.WHEN:
                        if (EvaluateCondition())
                        {
                            ModifyCode(true);
                        }
                        else
                        {
                            ModifyCode(false);
                        }
                        currentLaunchOrder = "";
                        break;
                    case LaunchOrders.WAIT:
                        if (EvaluateCondition())
                        {
                            currentLaunchOrder = "";
                        }
                        break;

                }
                return false;
            }

            private bool EvaluateCondition()
            {
                switch (currentCondition)
                {
                    case Conditions.NONE:
                        return true;
                    case Conditions.PGRA:
                        double currentPGravity = control.GetNaturalGravity().Length();
                        switch (currentOperator)
                        {
                            case Operators.EQUAL:
                                return currentPGravity == value;
                            case Operators.LESS:
                                return currentPGravity < value;
                            case Operators.MORE:
                                return currentPGravity > value;
                        }
                        break;
                    case Conditions.AGRA:
                        double currentAGravity = control.GetArtificialGravity().Length();
                        switch (currentOperator)
                        {
                            case Operators.EQUAL:
                                return currentAGravity == value;
                            case Operators.LESS:
                                return currentAGravity < value;
                            case Operators.MORE:
                                return currentAGravity > value;
                        }
                        break;
                    case Conditions.SPEED:
                        double currentSpeed = control.GetShipSpeed();
                        switch (currentOperator)
                        {
                            case Operators.EQUAL:
                                return currentSpeed == value;
                            case Operators.LESS:
                                return currentSpeed < value;
                            case Operators.MORE:
                                return currentSpeed > value;
                        }
                        break;
                    case Conditions.TIME:
                        if (time == -1)
                        {
                            time = DateTime.Now.Millisecond;
                        }
                        if (DateTime.Now.Millisecond - time > value * 1000)
                        {
                            time = -1;
                            return true;
                        }
                        return false;
                    case Conditions.LOCATION:
                        BoundingSphereD boundaries = control.CubeGrid.WorldVolume;
                        return (boundaries.Contains(conditionGPS) != ContainmentType.Disjoint);
                }
                return false;
            }
            
            private void ModifyCode (bool insert)
            {
                String customData = mainProgram.Me.CustomData;
                String[] columns = customData.Split('\n');
                bool first = false;
                int counter = 0;
                for (int i = 0; i < columns.Length; i++)
                {
                    if (columns[i] == "{")
                    {
                        if (!first)
                        {
                            columns[i] = "";
                            first = true;
                        }
                        else
                        {
                            counter++;
                        }
                        
                    }
                    if (columns[i] == "}")
                    {
                        if (counter == 0)
                        {
                            columns[i] = "";
                            break; ;
                        }
                        else
                        {
                            counter--;
                        }
                    }
                    if (first && !insert)
                    {
                        columns[i] = "";
                    }
                }
                mainProgram.Me.CustomData = "";
                foreach (String column in columns)
                {
                    if (column != "")
                    {
                        mainProgram.Me.CustomData = mainProgram.Me.CustomData + column + "\n";
                    }
                }

            }
            
            public bool ParseLaunchOrder(string order)
            {
                try
                {
                    if (order == "{" || order == "}")
                    {
                        return true;
                    }
                    string[] parts = order.Split(' ');
                    currentCommand = (LaunchOrders)Enum.Parse(typeof(LaunchOrders), parts[0]);
                    switch (currentCommand)
                    {
                        case LaunchOrders.BURN:
                            ParseBlockGroup(order);
                            if (parts.Length == 2)
                            {
                                value = 100;
                            }
                            else
                            {
                                value = float.Parse(parts[2]);
                            }
                            break;
                        case LaunchOrders.LOCK:
                            throw new Exception("You got shit to do, case LOCK @ ParseLaunchOrder");
                            break;
                        case LaunchOrders.SET:
                            ParseBlockGroup(order);
                            setOn = order.ElementAt(order.Length - 1) == 'N';
                            break;
                        case LaunchOrders.TURN:
                            ParseDegree(order);
                            break;
                        case LaunchOrders.WAIT:
                            ParseCondition(order);
                            break;
                        case LaunchOrders.WHEN:
                            ParseCondition(order);
                            break;
                        case LaunchOrders.STOP:
                            break;
                    }
                }
                catch (Exception e)
                {
                    //Echo(e.TargetSite.Name);
                    //Echo(e.Message);
                    return false;
                }
                return true;

            }

            void ParseCondition(string order)
            {
                char[] chars = order.ToCharArray();
                string condition = "";
                bool inSequence = false;
                for (int i = 0; i < chars.Length; i++)
                {
                    if (inSequence && chars[i] == '}')
                    {
                        inSequence = false;
                        break;
                    }
                    if (inSequence)
                    {
                        condition = condition + chars[i];
                    }
                    if (chars[i] == '{')
                    {
                        inSequence = true;
                    }
                }
                if (condition == "")
                {
                    currentCondition = Conditions.NONE;
                }
                string[] conditionParts = condition.Split(' ');
                currentCondition = (Conditions)Enum.Parse(typeof(Conditions), conditionParts[0]);
                switch (currentCondition)
                {
                    case Conditions.TIME:
                        value = float.Parse(conditionParts[1]);
                        break;
                    case Conditions.SPEED:
                        value = float.Parse(conditionParts[1]);
                        if (conditionParts.Length == 2)
                        {
                            currentOperator = Operators.EQUAL;
                        }
                        currentOperator = (Operators)Enum.Parse(typeof(Operators), conditionParts[2]);
                        break;
                    case Conditions.PGRA:
                        value = float.Parse(conditionParts[1]);
                        if (conditionParts.Length == 2)
                        {
                            currentOperator = Operators.EQUAL;
                        }
                        currentOperator = (Operators)Enum.Parse(typeof(Operators), conditionParts[2]);
                        break;
                    case Conditions.AGRA:
                        value = float.Parse(conditionParts[1]);
                        if (conditionParts.Length == 2)
                        {
                            currentOperator = Operators.EQUAL;
                        }
                        currentOperator = (Operators)Enum.Parse(typeof(Operators), conditionParts[2]);
                        break;
                    case Conditions.LOCATION:
                        char[] gps = conditionParts[2].ToCharArray();
                        short coordinate = 0;
                        string number = "";
                        //Redundant mit method vereinen 
                        for (int i = 0; i < gps.Length; i++)
                        {
                            if (gps[i] != '(' && gps[i] != ')' && gps[i] != ' ' && gps[i] != ':')
                            {
                                number = number + gps[i];
                            }
                            if (gps[i] == ':' || gps[i] == ')')
                            {
                                if (coordinate == 0)
                                {
                                    conditionGPS.X = double.Parse(number);
                                    coordinate++;
                                }
                                else if (coordinate == 1)
                                {
                                    conditionGPS.Y = double.Parse(number);
                                    coordinate++;
                                }
                                else
                                {
                                    conditionGPS.Z = double.Parse(number);
                                }
                                number = "";
                            }
                        }
                        break;
                }

            }

            void ParseBlockGroup(string order)
            {
                bool inSequence = false;
                bool inWord = false;
                List<string> names = new List<string>();
                string current = "";
                char[] orderInChars = order.ToCharArray();
                for (int i = 0; i < orderInChars.Length; i++)
                {
                    if (inWord && orderInChars[i] != ',' && orderInChars[i] != '}')
                    {
                        current = current + orderInChars[i];
                    }
                    else if (inWord && orderInChars[i] == ',')
                    {
                        inWord = false;
                        current = ExtractWord(current);
                        names.Add(current);
                    }
                    if (inSequence && !inWord && orderInChars[i] != ' ' && orderInChars[i] != ',')
                    {
                        inWord = true;
                        current = orderInChars[i].ToString();
                    }
                    if (orderInChars[i] == '{')
                    {
                        inSequence = true;
                    }
                    if (orderInChars[i] == '}')
                    {
                        current = ExtractWord(current);
                        names.Add(current);
                        inSequence = false;
                        inWord = false;
                        //End of block sequence reached, all names extracted
                        break;
                    }
                }
                if (names.Count == 0)
                {
                    throw new Exception("Names in Launch sequence could not be extracted");
                }
                FindBlocksWithNames(names);
            }

            private void ParseDegree(string order)
            {
                order = order.Substring(5);
                if (order.StartsWith("DEG") || order.StartsWith("GPS"))
                {
                    char[] gps = order.Substring(4).ToCharArray();
                    short coordinate = 0;
                    string number = "";
                    for (int i = 0; i < gps.Length; i++)
                    {
                        if (gps[i] != '(' && gps[i] != ')' && gps[i] != ' ' && gps[i] != ':')
                        {
                            number = number + gps[i];
                        }
                        if (gps[i] == ':' || gps[i] == ')')
                        {
                            if (coordinate == 0)
                            {
                                degree.X = double.Parse(number);
                                coordinate++;
                            }
                            else if (coordinate == 1)
                            {
                                degree.Y = double.Parse(number);
                                coordinate++;
                            }
                            else
                            {
                                degree.Z = double.Parse(number);
                            }
                            number = "";
                        }
                    }
                }
                else
                {
                    string side = order.Split(' ')[0];
                    currentSide = (Side)Enum.Parse(typeof(Side), side);
                    value = float.Parse(order.Split(' ')[1]);
                }
            }

            private void FindBlocksWithNames(List<string> names)
            {
                currentBlocks.Clear();
                foreach (string name in names)
                {
                    if (name.StartsWith("GR:"))
                    {
                        string nam = name.Substring(3);
                        IMyBlockGroup temp = mainProgram.GridTerminalSystem.GetBlockGroupWithName(nam);
                        List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
                        if (temp == null)
                        {
                            throw new Exception("Group '" + nam + "' could not be found");
                        }
                        temp.GetBlocks(list);
                        foreach (IMyTerminalBlock term in list)
                        {
                            currentBlocks.Add(term);
                        }
                    }
                    else if (name.StartsWith("DIR:"))
                    {
                        string nam = name.Substring(4);
                        Side neededSite = (Side)Enum.Parse(typeof(Side), nam);
                        //Get all thruster out of dict
                    }
                    else
                    {
                        IMyTerminalBlock temp = mainProgram.GridTerminalSystem.GetBlockWithName(name);
                        if (temp == null)
                        {
                            throw new Exception("Block '" + name + "' could not be found");
                        }
                        currentBlocks.Add(temp);
                    }
                }

            }

            private string ExtractWord(string word)
            {
                char[] chars = word.ToCharArray();
                int end = chars.Length - 1;
                while (chars[end] == ' ')
                {
                    end--;
                }
                end = end + 1;
                return word.Substring(0, end);
            }

            private bool ExtractNextLaunchOrdner()
            {
                string[] orders = mainProgram.Me.CustomData.Split('\n');
                if (orders.Length == 1 && orders[0] == "")
                {
                    return true;
                }
                currentLaunchOrder = orders[0];
                mainProgram.Me.CustomData = "";
                for (int i = 1; i < orders.Length; i++)
                {
                    mainProgram.Me.CustomData = mainProgram.Me.CustomData + orders[i] + "\n";
                }
                return false;
            }

        }
    }
}
