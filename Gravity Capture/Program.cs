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
        public List<IMyGravityGeneratorBase> FrBa = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> LeRi = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> UpDo = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> Shield = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> Rota1 = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> Rota2 = new List<IMyGravityGeneratorBase>();

        //Declaring MStates (MaschineStates)
        enum MState { Working, WaitingTime, WaitingExternalEvent };


        IMyTextPanel debugPanel;
        IMyTimerBlock scriptTimer;
        StateMaschine[] mainStateMaschine;
        State currentState;
        MState currentMState;
        float waitTime;
        string awaitedTrigger;
        IMyGravityGenerator[,] UpDown=new IMyGravityGenerator[2,2];
        GeneratorsUni GGDis;
        GeneratorsUni GGR;
        GeneratorsUni GGL;
        GeneratorsUni GGUuDo;
        GeneratorsDiv GGFr;
        GeneratorsDiv GGBa;

        IMySensorBlock HangarSensor;
        IMyShipController Reference;


        public Program()
        {
            scriptTimer = GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;

            debugPanel = GridTerminalSystem.GetBlockWithName("Debug Panel") as IMyTextPanel;

            mainStateMaschine = CreateStateMaschine(debugPanel);
            debugPanel.WritePublicText("Main is ready \n", true);
            currentState = State.Idle;
            currentMState = MState.Working;

            List<IMyGravityGenerator> Dis = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(Dis, x => x.CustomName.Contains("Gravity Generator DIS"));
            GGDis = new GeneratorsUni(Dis, 20, 15, 10);
            
            List<IMyGravityGenerator> Re = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(Re, x => x.CustomName.Contains("Gravity Generator re"));
            GGR = new GeneratorsUni(Re, 30, 13.6f, 10);

            List<IMyGravityGenerator> Le = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(Le, x => x.CustomName.Contains("Gravity Generator le"));
            GGL = new GeneratorsUni(Le, 30, 13.6f, 10);

            UpDown[0, 0] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FR") as IMyGravityGenerator;
            UpDown[0, 1] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FL") as IMyGravityGenerator;
            UpDown[1, 0] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BR") as IMyGravityGenerator;
            UpDown[1, 1] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BL") as IMyGravityGenerator;
            List<IMyGravityGenerator> UpDo = new List<IMyGravityGenerator>();
            foreach (IMyGravityGenerator gg in UpDown)
                UpDo.Add(gg);
            GGUuDo = new GeneratorsUni(UpDo, 10, 30, 7.5f);
            
            List<Gravity> Fr = new List<Gravity>();
            List<Gravity> Ba = new List<Gravity>();
            Fr.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator fr 1 +") as IMyGravityGenerator, 6, 16, 35));
            Ba.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator ba 1 +") as IMyGravityGenerator, 6, 16, 35));
            Fr.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator fr 2 -") as IMyGravityGenerator, 3, 17, 30));
            Ba.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator ba 2 -") as IMyGravityGenerator, 3, 17, 30));
            Fr.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator fr 3 +") as IMyGravityGenerator, 8, 11, 30));
            Ba.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator ba 3 +") as IMyGravityGenerator, 8, 11, 30));

            GGFr = new GeneratorsDiv(Fr);
            GGBa = new GeneratorsDiv(Ba);

           

            HangarSensor = GridTerminalSystem.GetBlockWithName("Sensor Hangar") as IMySensorBlock;
            Reference = GridTerminalSystem.GetBlockWithName("Hangar Reference") as IMyShipController;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument)
        {
            Capture();
            switch (currentMState)
            {
                case MState.Working:
                    debugPanel.WritePublicText(("Current State is " + mainStateMaschine[(int)currentState].currentState + "\n"), true);
                    switch (mainStateMaschine[(int)currentState].conditionForNextState)
                    {
                        case Conditions.None:
                            debugPanel.WritePublicText(("Switching to state " + mainStateMaschine[(int)currentState].nextState + "\n"), true);
                            currentState = mainStateMaschine[(int)currentState].nextState;
                            break;
                        case Conditions.Time:
                            waitTime = mainStateMaschine[(int)currentState].ConditionTime;
                            scriptTimer.TriggerDelay = waitTime;
                            scriptTimer.StartCountdown();
                            debugPanel.WritePublicText(("Waiting for switch in " + waitTime +" seconds\n"), true);
                            currentMState = MState.WaitingTime;
                            break;
                        case Conditions.ExternalTrigger:
                            awaitedTrigger = mainStateMaschine[(int)currentState].nextState.ToString();
                            debugPanel.WritePublicText(("Waiting for switch with trigger " + awaitedTrigger + "\n"), true);
                            currentMState = MState.WaitingExternalEvent;
                            break;
                    }
                    
                    break;
                case MState.WaitingTime:
                    if (argument == "timerTrigger")
                    {
                        currentState = mainStateMaschine[(int)currentState].nextState;
                        currentMState = MState.Working;
                    }
                    break;
                case MState.WaitingExternalEvent:
                    if (argument == awaitedTrigger)
                    {
                        currentState = mainStateMaschine[(int)currentState].nextState;
                        currentMState = MState.Working;
                    }
                    break;
                default:
                    break;
            } 
        }
        public void Capture()
        {
            var worldToAnchorLocalMatrix = Matrix.Transpose(Reference.WorldMatrix.GetOrientation());

            MyDetectedEntityInfo fighter =HangarSensor.LastDetectedEntity;

            Vector3D worldPositon = (fighter.Position-Reference.GetPosition());
            Vector3D position = Vector3D.Transform(worldPositon, worldToAnchorLocalMatrix)+new Vector3D(0,10,1.25f);

            Vector3D worldVelocities = (fighter.Velocity - Reference.GetShipVelocities().LinearVelocity);
            Vector3D velocities = Vector3D.Transform(worldVelocities, worldToAnchorLocalMatrix);

            Echo(Convert.ToString(velocities.X));
            Echo(Convert.ToString(velocities.Y));
            Echo(Convert.ToString(velocities.Z));
            Echo(Convert.ToString(position.X));
            Echo(Convert.ToString(position.Y));
            Echo(Convert.ToString(position.Z));

            GGFr.SetGravity(9.81f - 5 * Convert.ToSingle(velocities.Z));
            GGBa.SetGravity(9.81f + 5 * Convert.ToSingle(velocities.Z));
            GGR.SetGravity(9.81f + 5 * Convert.ToSingle(velocities.X));
            GGL.SetGravity(9.81f - 5 * Convert.ToSingle(velocities.X));

            if (velocities.Y > 2 || Math.Abs(position.X)> 2 || Math.Abs(position.Y+20) > 2 || Math.Abs(position.Z) > 2)
            {
                GGDis.OnOff(true);
                GGDis.SetGravity(-9.81f - 5 * Convert.ToSingle(velocities.Y));
                GGUuDo.SetGravity(Convert.ToSingle(-10 * (position.Y + 20) - 5 *velocities.Y));
            }
            else
            {
                GGDis.OnOff(false);
                float upDoBase = Convert.ToSingle(-10*(position.Y + 20) - 5 * velocities.Y);
                float front = 20 * Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Forward));
                float right = 40 * Convert.ToSingle(fighter.Orientation.Forward.Dot(Reference.WorldMatrix.Right));

                UpDown[0, 0].GravityAcceleration = upDoBase - front - right;
                UpDown[0, 1].GravityAcceleration = upDoBase - front + right;
                UpDown[1, 0].GravityAcceleration = upDoBase + front - right;
                UpDown[1, 1].GravityAcceleration = upDoBase + front + right;
            }

        }
    }
}