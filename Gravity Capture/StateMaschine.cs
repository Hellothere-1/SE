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
        public enum State {Error = -1, None = 0, Idle, OpenHangar, LaunchShip, CaptureShip};

        private class StateMaschinePage
        {
            public State currentState;
            public State nextState;
            public bool waitForTrigger;
        }

        public class StateMaschine
        {
            Program parent;
            IMyTimerBlock scriptTimer;
            IMyTimerBlock CodeTriggerTimer;
            StateMaschinePage[] stateMaschine;
            bool running;
            bool statusHangarDoors = false;
            State currentState = State.Error;
            LCDClass lcdHandler;

            public StateMaschine(LCDClass log, Program par)
            {
                parent = par;
                lcdHandler = log;
                stateMaschine = CreateStateMaschine();
                //Start the main code
                if (currentState != State.Error)
                {
                    running = true;
                    startCodeTriggerTimer();
                }
                
            }

            StateMaschinePage[] CreateStateMaschine()
            {

                scriptTimer = parent.GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;
                CodeTriggerTimer = parent.GridTerminalSystem.GetBlockWithName("StateMaschine Timer") as IMyTimerBlock;

                int lengthStateTable = Enum.GetNames(typeof(State)).Length - 2;
                StateMaschinePage[] stateMaschine = new StateMaschinePage[lengthStateTable];
                for (int i = 0; i < lengthStateTable; i++)
                {
                    stateMaschine[i] = new StateMaschinePage();
                }
                FillStateTable(stateMaschine);

                //Check if logic part is missing------------------------------------------
                if (scriptTimer == null || CodeTriggerTimer == null || stateMaschine == null)
                {
                    lcdHandler.logMessage("State Maschine or/and Timers not found, are the names correct?", Labels.ERROR);
                    currentState = State.Error;
                    return null;
                }
                else
                {
                    lcdHandler.logMessage("State Maschine and Timers found and activ", Labels.BOOTUP);
                    currentState = State.Idle;
                    return stateMaschine;
                }
                //---------------------------------------------------------------------------
            }

            public bool isOperational()
            {
                if (currentState == State.Error)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            void FillStateTableEntry(StateMaschinePage entry, State current, State next, bool wait)
            {
                entry.currentState = current;
                entry.nextState = next;
                entry.waitForTrigger = wait;
            }

            void FillStateTable(StateMaschinePage[] state_table)
            {   //TODO extend for all states 
                FillStateTableEntry(state_table[0], State.Idle, State.OpenHangar, true);
                FillStateTableEntry(state_table[1], State.OpenHangar, State.None, false);
                FillStateTableEntry(state_table[2], State.LaunchShip, State.OpenHangar, true);
                FillStateTableEntry(state_table[3], State.CaptureShip, State.OpenHangar, true);
            }

            public void run(string argument)
            {
                stopCodeTriggerTimer();
                if (currentState == State.Error)
                {
                    return;
                }

                if (running)
                {
                    //TODO execute the code

                    //Switch State Block
                    if (stateMaschine[(int)currentState - 1].waitForTrigger)
                    {
                        //Get into waiting mode
                        running = false;
                        //-------------------------------

                        lcdHandler.logMessage("Changing into waiting mode");
                    }
                    else if (stateMaschine[(int)currentState - 1].nextState == State.None)
                    {
                        //Start Timer for reset to Idle
                        scriptTimer.TriggerDelay = 60;
                        scriptTimer.StartCountdown();
                        //-------------------------------

                        //Get into waiting mode
                        running = false;
                        //-------------------------------

                        lcdHandler.logMessage("Changing into waiting mode + activated Timeout(60s)");
                    }
                    else
                    {
                        //Getting into the next state
                        currentState = stateMaschine[(int)currentState - 1].nextState;
                        stateMaschine[(int)State.OpenHangar - 1].nextState = State.None;
                        //-------------------------------

                        lcdHandler.logMessage("Switched to " + currentState.ToString(), Labels.STATE);
                        startCodeTriggerTimer();
                    }
                    lcdHandler.logHeadOnScreen(currentState, running, statusHangarDoors);
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
                    lcdHandler.logMessage("Input is " + argument, Labels.DEBUG);
                    //Normal state change by checking if form currentState_nextState
                    string[] parts = argument.Split('_');
                    try
                    {
                        if (parts[0] == stateMaschine[(int)currentState - 1].currentState.ToString())
                        {
                            try
                            {
                                currentState = (State)Enum.Parse(typeof(State), parts[1]);
                                lcdHandler.logMessage("Switched to " + currentState.ToString(), Labels.STATE);
                                running = true;
                                startCodeTriggerTimer();
                                inputValid = true;
                            }
                            catch (ArgumentException)
                            {
                                lcdHandler.logMessage("Error : " + parts[1] + " nicht als State vorhanden", Labels.WARNING);
                                return;
                            }
                        }
                        //-----------------------------------------

                        //Special repeated state change if form *_OpenHangar_nextState
                        if (parts[1] == "OpenHangar" && currentState == State.OpenHangar && parts.Length == 3)
                        {
                            try
                            {
                                stateMaschine[(int)currentState - 1].nextState = (State)Enum.Parse(typeof(State), parts[2]);
                                scriptTimer.StopCountdown();
                                running = true;
                                startCodeTriggerTimer();
                                inputValid = true;

                            }
                            catch (ArgumentException)
                            {
                                lcdHandler.logMessage("Error : " + parts[2] + " nicht als State vorhanden", Labels.WARNING);
                                return;
                            }
                        }
                        //---------------------------------------------
                    }
                    catch (Exception e)
                    {
                        lcdHandler.logMessage(e.ToString(), Labels.ERROR);
                    }

                    //Printing error for wrong input
                    if (!inputValid)
                    {
                        lcdHandler.logMessage("Input " + argument + " is not valid for this code", Labels.WARNING);
                    }
                    else
                    {
                        lcdHandler.logHeadOnScreen(currentState, running, statusHangarDoors);
                    }
                    //----------------------------------------------
                }
            }

            //Called by script to get out of CaptureShip/LaunchShip
            public void ForceNextState()
            {
                currentState = stateMaschine[(int)currentState - 1].nextState;
            }
            //-----------------------------------------------------


            //Called by script to stop the timer to avoid double calls
            void stopCodeTriggerTimer()
            {
                CodeTriggerTimer.StopCountdown();
            }
            //----------------------------------------------------


            //Called by script to reset timerdelay and start it
            void startCodeTriggerTimer()
            {
                CodeTriggerTimer.Trigger();
                CodeTriggerTimer.TriggerDelay = 1;
                CodeTriggerTimer.StartCountdown();
            }

        }
    }
}
