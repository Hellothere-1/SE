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
        public enum MState { Working, WaitingTime, WaitingExternalEvent };


        IMyTextPanel debugPanel;
        bool debugEnabled = false;
        bool running;
        IMyTimerBlock scriptTimer;
        StateMaschine[] mainStateMaschine;
        State currentState;
        
        
        

        public Program()
        {
            scriptTimer = GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;
            debugPanel = GridTerminalSystem.GetBlockWithName("Debug Panel") as IMyTextPanel;
            mainStateMaschine = CreateStateMaschine(debugPanel);
            if (mainStateMaschine == null)
            {
                debugPanel.WritePublicText("Error : StateMaschine could not be initialized");
                return;
            }
            debugPanel.WritePublicText("StateMaschine is ready \n");
            currentState = State.Idle;
            running = true;
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
            if (running)
            {
                //TODO execute the code


                //Switch State Block
                if (mainStateMaschine[(int)currentState - 1].waitForTrigger)
                {
                    //Get into waiting mode
                    running = false;
                    //-------------------------------

                    debugPanel.WritePublicText(("Changing into waiting mode\n"), true);

                    return;
                }
                if (mainStateMaschine[(int)currentState - 1].nextState == State.None)
                {
                    //Start Timer for reset to Idle
                    scriptTimer.TriggerDelay = 60;
                    scriptTimer.StartCountdown();
                    //-------------------------------

                    //Get into waiting mode
                    running = false;
                    //-------------------------------

                    debugPanel.WritePublicText(("Changing into waiting mode + activated Timeout(60s)\n"), true);
                    return;
                }
                else
                {
                    //Getting into the next state
                    currentState = mainStateMaschine[(int)currentState - 1].nextState;
                    mainStateMaschine[(int)State.OpenHangar - 1].nextState = State.None;
                    //-------------------------------
                    debugPanel.WritePublicText(("Switched to " + currentState.ToString() + "\n"), true);
                    return;
                }
                
            }
            else
            {
                
                bool inputValid = false;
                //Returning if argument is null
                if (argument == "")
                {
                    return;
                }
                //--------------------------------
                debugPanel.WritePublicText(("Input is " + argument + "\n"), true);
                //Normal state change by checking if form currentState_nextState
                string[] parts = argument.Split('_');
                if (parts[0] == mainStateMaschine[(int)currentState - 1].currentState.ToString())
                {
                    try
                    {
                        currentState = (State)Enum.Parse(typeof(State), parts[1]);
                        running = true;
                        inputValid = true;
                    }
                    catch (ArgumentException)
                    {
                        debugPanel.WritePublicText(("Error : " + parts[1] + " nicht als State vorhanden\n"), true);
                        return;
                    }
                }
                //-----------------------------------------
                //Special repeated state change if form *_OpenHangar_nextState
                if (parts[1] == "OpenHangar" && currentState == State.OpenHangar && parts.Length == 3)
                {
                    try
                    {
                        mainStateMaschine[(int)currentState - 1].nextState = (State)Enum.Parse(typeof(State), parts[2]);
                        scriptTimer.StopCountdown();
                        running = true;
                        inputValid = true;
                    }
                    catch (ArgumentException)
                    {
                        debugPanel.WritePublicText(("Error : " + parts[2] + " nicht als State vorhanden\n"), true);
                        return;
                    }
                }
                //---------------------------------------------

                //Printing error for wrong input
                if (!inputValid)
                {
                    debugPanel.WritePublicText(("Input " + argument + " is not valid for this code\n"), true);
                }
                //----------------------------------------------

            }
        }

        public void ForceNextState()
        {
            currentState = mainStateMaschine[(int)currentState - 1].nextState;
        }
    }
}