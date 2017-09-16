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
        
        
        

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
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
            if (scriptTimer == null)
            {
                scriptTimer = GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;
            }
            if (debugPanel == null)
            {
                debugPanel = GridTerminalSystem.GetBlockWithName("Debug Panel") as IMyTextPanel;
            }
            if (mainStateMaschine == null)
            {
                mainStateMaschine = CreateStateMaschine(debugPanel);
                debugPanel.WritePublicText("Main is ready \n", true);
                currentState = State.Idle;
                currentMState = MState.Working;
            }
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