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
        List<IMyGravityGeneratorBase> FrBa = new List<IMyGravityGeneratorBase>();
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
        GeneratorsUni GGRL;
        GeneratorsUni GGUuDo;
        GeneratorsDiv GGFr;
        GeneratorsDiv GGBa;

        IMySensorBlock HangarSensor;



        public Program()
        {
            scriptTimer = GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;

            debugPanel = GridTerminalSystem.GetBlockWithName("Debug Panel") as IMyTextPanel;

            mainStateMaschine = CreateStateMaschine(debugPanel);
            debugPanel.WritePublicText("Main is ready \n", true);
            currentState = State.Idle;
            currentMState = MState.Working;

            List<IMyGravityGenerator> Dis = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(Dis, x => x.CustomName == "Gravity Generator Dis");
            GGDis = new GeneratorsUni(Dis, 20, 15, 10);

            List<IMyGravityGenerator> RL = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(RL, x => x.CustomName == "Gravity Generator le/ri");
            GGRL = new GeneratorsUni(RL, 30, 13.6f, 10);

            UpDown[0, 0] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FR")as IMyGravityGenerator;
            UpDown[0, 1] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FL") as IMyGravityGenerator;
            UpDown[1, 0] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BR") as IMyGravityGenerator;
            UpDown[1, 1] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BL") as IMyGravityGenerator;
            List<IMyGravityGenerator> UpDo = new List<IMyGravityGenerator>();
            foreach (IMyGravityGenerator gg in UpDown)
                UpDo.Add(gg);
            GGUuDo = new GeneratorsUni(Dis, 10, 30, 7.5f);

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
    }
}