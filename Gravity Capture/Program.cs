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

        
        List<IMyTextPanel> outputPanels = new List<IMyTextPanel>();
        bool debugEnabled = false;
        bool statusHangarDoors = false;
        bool running;
        IMyTimerBlock scriptTimer;
        IMyTimerBlock CodeTriggerTimer;
        StateMaschine[] mainStateMaschine;
        State currentState;
        
        
        

        public Program()
        {
            scriptTimer = GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;
            CodeTriggerTimer = GridTerminalSystem.GetBlockWithName("StateMaschine Timer") as IMyTimerBlock;
            GridTerminalSystem.GetBlocksOfType(outputPanels, x => x.CustomName.Contains("Output"));
            mainStateMaschine = CreateStateMaschine();
            foreach (IMyTextPanel lcd in outputPanels)
            {
                lcd.WritePublicText("");
                updateHead(lcd, "Ready", "Closed");
            }
            if (mainStateMaschine == null)
            {
                logOnScreen("Error : StateMaschine could not be initialized");
                return;
            }
            currentState = State.Idle;
            running = true;
            startCodeTriggerTimer();
            
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
            stopCodeTriggerTimer();
            
            
            

            
            if (running)
            {
                //TODO execute the code


                




                //Switch State Block
                if (mainStateMaschine[(int)currentState - 1].waitForTrigger)
                {
                    //Get into waiting mode
                    running = false;
                    //-------------------------------

                    logOnScreen("Changing into waiting mode\n");
                    updateLogHead();
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

                    logOnScreen("Changing into waiting mode + activated Timeout(60s)\n");
                    updateLogHead();
                    return;
                }
                else
                {
                    //Getting into the next state
                    currentState = mainStateMaschine[(int)currentState - 1].nextState;
                    mainStateMaschine[(int)State.OpenHangar - 1].nextState = State.None;
                    //-------------------------------

                    logOnScreen("Switched to " + currentState.ToString() + "\n");
                    updateLogHead();
                    startCodeTriggerTimer();
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

                //Normal state change by checking if form currentState_nextState
                string[] parts = argument.Split('_');
                if (parts[0] == mainStateMaschine[(int)currentState - 1].currentState.ToString())
                {
                    try
                    {
                        currentState = (State)Enum.Parse(typeof(State), parts[1]);
                        logOnScreen("Switched to " + currentState.ToString() + "\n");
                        running = true;
                        startCodeTriggerTimer();
                        inputValid = true;
                        updateLogHead();
                    }
                    catch (ArgumentException)
                    {
                        logOnScreen("Error : " + parts[1] + " nicht als State vorhanden\n");
                        updateLogHead();
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
                        startCodeTriggerTimer();
                        inputValid = true;
                        updateLogHead();
                    }
                    catch (ArgumentException)
                    {
                        logOnScreen("Error : " + parts[2] + " nicht als State vorhanden\n");
                        updateLogHead();
                        return;
                    }
                }
                //---------------------------------------------

                //Printing error for wrong input
                if (!inputValid)
                {
                    logOnScreen("Input " + argument + " is not valid for this code\n");
                    updateLogHead();
                }
                //----------------------------------------------

            }
        }


        //Called by script to get out of CaptureShip/LaunchShip
        public void ForceNextState()
        {
            currentState = mainStateMaschine[(int)currentState - 1].nextState;
        }
        //-----------------------------------------------------


        //Called by script to stop the timer to avoid double calls
        public void stopCodeTriggerTimer()
        {
            CodeTriggerTimer.StopCountdown();
        }
        //----------------------------------------------------


        //Called by script to reset timerdelay and start it
        public void startCodeTriggerTimer()
        {
            CodeTriggerTimer.Trigger();
            CodeTriggerTimer.TriggerDelay = 1;
            CodeTriggerTimer.StartCountdown();
        }




        public void logOnScreen(string logMessage)
        {
            string currentText = outputPanels[0].GetPublicText();
            int index = findEndOfLogHead(currentText);
            currentText = currentText.Insert(index, logMessage);
            foreach (IMyTextPanel lcd in outputPanels)
            {
                lcd.WritePublicText(currentText);
            }
            
        }


        public void updateLogHead()
        {
            string hangardoors = statusHangarDoors ? "Open" : "Closed";
            string status = running ? "Running" : "Waiting";
            foreach (IMyTextPanel lcd in outputPanels)
            {
                updateHead(lcd, status, hangardoors);
            }
            
        }


        public void updateHead(IMyTextPanel lcd, string status, string hangarsOpen)
        {
            
            string currentText = lcd.GetPublicText();
            if (currentText == "")
            {
                lcd.WritePublicText("Current State : " + currentState.ToString() + "\n");
                lcd.WritePublicText("Current Status : " + status + "\n", true);
                lcd.WritePublicText("Status Hangar Doors : " + hangarsOpen + "\n\n", true);
                lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
            }
            else
            {
                string updateText = "";
                int index = findEndOfLogHead(currentText);
                updateText = currentText.Substring(index);
                lcd.WritePublicText("Current State : " + currentState.ToString() + "\n");
                lcd.WritePublicText("Current Status : " + status + "\n", true);
                lcd.WritePublicText("Status Hangar Doors : " + hangarsOpen + "\n\n", true);
                lcd.WritePublicText("==========Recent Updates========================================================================\n", true);
                lcd.WritePublicText(updateText, true);
            }

            
        }


        public int findEndOfLogHead(string currentText)
        {
            int index = currentText.IndexOf("Recent Updates");
            index = currentText.IndexOf("\n", index) + 1;
            return index;
        }

        
    }
}