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
    partial class Program : MyGridProgram
    {
        private int group = 0;
        private int volley = 0;
        private int id = 0;
        private String comPartner = "";
        private String key = "";

        //Launch orders
        enum Side {UP, DOWN, LEFT, RIGHT, FORWARD, BACKWARD };
        enum LaunchOrders {BURN, TURN, WAIT, LOCK, WHEN, SET };
        enum Conditions {TIME, PGRA, AGRA, SPEED, LOCATION, NONE };
        enum Operators {EQUAL, MORE, LESS };
        LaunchOrders currentCommand;
        Conditions currentCondition;
        Operators currentOperator;
        Side currentSide;
        List<IMyTerminalBlock> currentBlocks = new List<IMyTerminalBlock>();
        float conditionValue = 0;
        Vector3D conditionGPS = new Vector3D();
        Vector3D degree = new Vector3D();
        bool setOn;


        //int lengthSave;
        //Maybe save some amount of raycast distance for emergency use

        IMyProgrammableBlock programmableBlock;
        IMyCameraBlock visor;
        IMyRemoteControl control;
        IMyShipMergeBlock merge;
        IMyRadioAntenna antenna;
        IMyBlockGroup starterBlocks;

        TargetFuncs funcs;

        Vector3 directions;

        List<IMyWarhead> warheads = new List<IMyWarhead>();
        List<Gyroscope> gyros = new List<Gyroscope>();

        bool setupSuccess = false;

        bool launchSequence = false;
        bool launched = false;
        bool init = false;

        string currentLaunchOrder = "";

        public Program()
        {
            programmableBlock = Me;
            //Search for starter Group with this pb in it
            /*
            List<IMyBlockGroup> allGroups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(allGroups);
            bool found = false;
            foreach (IMyBlockGroup group in allGroups)
            {
                List<IMyProgrammableBlock> pbsInList = new List<IMyProgrammableBlock>();
                group.GetBlocksOfType(pbsInList);
                foreach (IMyProgrammableBlock pb in pbsInList)
                {
                    if (pb.Equals(programmableBlock))
                    {
                        starterBlocks = group;
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    break;
                }
            }
            if (!found)
            {
                Echo("Starter Group not found, is this Programmable Block in it?");
                return;
            }

            //Starter group is found, trying to acquire the needed compoents
            List<IMyTerminalBlock> tempBlocks = new List<IMyTerminalBlock>();
            try
            {
                starterBlocks.GetBlocksOfType<IMyCameraBlock>(tempBlocks);
                visor = tempBlocks[0] as IMyCameraBlock;
                visor.EnableRaycast = true;
                Echo("Camera found and activ");
            }
            catch (Exception)
            {
                Echo("Camera could not be found in starter group");
                return;
            }
            try
            {
                starterBlocks.GetBlocksOfType<IMyShipMergeBlock>(tempBlocks);
                merge = tempBlocks[0] as IMyShipMergeBlock;
                Echo("Merge Block found and activ");
            }
            catch (Exception)
            {
                Echo("Merge Block could not be found in starter group");
                return;
            }
            try
            {
                starterBlocks.GetBlocksOfType<IMyRemoteControl>(tempBlocks);
                control = tempBlocks[0] as IMyRemoteControl;
                Echo("Remote Control found and activ");
            }
            catch (Exception)
            {
                Echo("Remote Control could not be found in starter group");
                return;
            }
            try
            {
                starterBlocks.GetBlocksOfType<IMyRadioAntenna>(tempBlocks);
                antenna = tempBlocks[0] as IMyRadioAntenna;
                Echo("Radio Antenna found and activ");
            }
            catch (Exception)
            {
                Echo("Antenna could not be found in starter group");
                return;
            }*/
            setupSuccess = true;
            //funcs = new TargetFuncs(this, visor);
            Echo("Setup completed, Missile ready to fire");
            //Components found, setup complete, missile ready to fire 
        }

        public void Main(string argument)
        {
            if (!setupSuccess)
            {
                return;
            }
            if (argument == "Parse")
            {
                string[] orders = Me.CustomData.Split('\n');
                short errors = 0;
                foreach (string order in orders)
                {
                    if (!ParseLaunchOrder(order))
                    {
                        Echo("Parsing of line " + order + " failed!");
                        errors++;
                    }
                }
                Echo("Finished parsing with " + errors + " incorrect lines");
                Echo("Remenber that type errors will occur only at runtime");
            }
            if (launched)
            {
                if (!init)
                {
                    initMissile();
                    init = true;
                    Echo("Init completed");
                }
                else
                {
                    directions = funcs.run(control);
                    SetGyros(directions.Z, directions.Y, 0);
                    if (directions.Y == 0 && directions.Z == 0)
                    {
                        Echo("Target lost");
                    }
                    else
                    {
                        Echo("Tracking target");
                    }
                }
                //TODO currently only one argument is needed, if more are needed delete the following return
                return;
            }
            if (argument == "Fire")
            {
                if (visor.AvailableScanRange < 5000)
                {
                    Echo("Scanning is recharging");
                    return;
                }
                MyDetectedEntityInfo target;
                target = visor.Raycast(5000);
                if (target.IsEmpty())
                {
                    //Search in cone form
                    //TODO launch sequence without direct line of sight possible, redirecting missile per antenna
                    Echo("No target found");
                    return;
                }
                funcs.LockTarget(target, control);
                merge.Enabled = false;
                launched = true;
                Echo("Launched");
                return;
            }
        }

        void initMissile()
        {
            //TODO arm warheads at launch? or better to arm them after time/correct launch?
            GridTerminalSystem.GetBlocksOfType(warheads);
            foreach (IMyWarhead boom in warheads)
            {
                if (!boom.GetValueBool("Safety"))
                {
                    boom.SetValueBool("Safety", true);
                }
            }

            List<IMyGyro> GyrosList = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(GyrosList);
            foreach (IMyGyro gyro in GyrosList)
            {
                gyro.GyroOverride = true;
                gyros.Add(new Gyroscope(gyro, control));
            }
            //thrust.SetValueFloat("Override", 100);
            //Method used to set override, should be in target function class
        }

        void executeBootSequence()
        {
            /*
            * ACCEPTED INPUT:
            * 
            * BURN <Block>
            * TURN <Degree>
            * WAIT <Condition>
            * LOCK <Target>/<Method>
            * WHEN <Condition>
            * SET <Block> <ON/OFF>
            * 
            * <Conditions> :
            * - TIME <time>(<time> is float)
            * - PGRA <amount> <op> (<amount> is float, <op> one of: EQUAL, MORE, LESS (default EQUAL))
            * - AGRA <amount> <op> (see above)
            * - SPEED <velocity>(<velocity> is float)
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
            if (currentLaunchOrder == "")
            {
                if (ExtractNextLaunchOrdner())
                {
                    //End of launch sequence
                    //Switch to flight mode
                    return;
                }
                //TODO parse methode ist vllt besser
                if (!ParseLaunchOrder(currentLaunchOrder))
                {
                    //Could not be parsed
                    currentLaunchOrder = "";
                }
                
                    
                
               
            }
        }

        bool ParseLaunchOrder(string order)
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
                    conditionValue = float.Parse(conditionParts[1]);
                    break;
                case Conditions.SPEED:
                    conditionValue = float.Parse(conditionParts[1]);
                    break;
                case Conditions.PGRA:
                    conditionValue = float.Parse(conditionParts[1]);
                    if (conditionParts.Length == 2)
                    {
                        currentOperator = Operators.EQUAL;
                    }
                    currentOperator = (Operators)Enum.Parse(typeof(Operators), conditionParts[2]);
                    break;
                case Conditions.AGRA:
                    conditionValue = float.Parse(conditionParts[1]);
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

        void ParseDegree(string order)
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
                conditionValue = float.Parse(order.Split(' ')[1]);
            }
        }

        void FindBlocksWithNames(List<string> names)
        {
            currentBlocks.Clear();
            foreach (string name in names)
            {
                if (name.StartsWith("GR:"))
                {
                    string nam = name.Substring(3);
                    IMyBlockGroup temp = GridTerminalSystem.GetBlockGroupWithName(nam);
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
                    IMyTerminalBlock temp = GridTerminalSystem.GetBlockWithName(name);
                    if (temp == null)
                    {
                        throw new Exception("Block '" + name + "' could not be found");
                    }
                    currentBlocks.Add(temp);
                }
            }

        }

        string ExtractWord(string word)
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

        bool ExtractNextLaunchOrdner()
        {
            string[] orders = Me.CustomData.Split('\n');
            if (orders.Length == 1 && orders[0] == "")
            {
                return true;
            }
            currentLaunchOrder = orders[0];
            Me.CustomData = "";
            for (int i = 1; i < orders.Length; i++)
            {
                Me.CustomData = Me.CustomData + orders[i] + "\n";
            }
            return false;
        }

        void SetGyros(float pitch, float yaw, float roll)
        {
            float[] controls = new float[] { -pitch, yaw, -roll, pitch, -yaw, roll };
            foreach (Gyroscope g in gyros)
                g.SetRotation(controls);
        }
    }
}