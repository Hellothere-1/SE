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
        bool GGEnabled = false;

        //Logic of StateMaschine------------------------------
        LCDClass lcdHandler;
        bool statusHangarDoors = false;
        bool running;
        IMyTimerBlock scriptTimer;
        IMyTimerBlock CodeTriggerTimer;
        StateMaschine[] mainStateMaschine;
        State currentState;
        //-----------------------------------------------------

        //Groups of GG-----------------------------------------
        public List<IMyGravityGeneratorBase> FrBa = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> LeRi = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> UpDo = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> Shield = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> Rota1 = new List<IMyGravityGeneratorBase>();
        List<IMyGravityGeneratorBase> Rota2 = new List<IMyGravityGeneratorBase>();
        IMyGravityGenerator[,] UpDown=new IMyGravityGenerator[2,2];
        GeneratorsUni GGDis;
        GeneratorsUni GGRL;
        GeneratorsUni GGUuDo;
        GeneratorsDiv GGFr;
        GeneratorsDiv GGBa;
        //------------------------------------------------------


        IMySensorBlock HangarSensor;
        IMyShipController Reference;


        public Program()
        {
            //Initialize logic for state maschine ------------------------------------
            scriptTimer = GridTerminalSystem.GetBlockWithName("Script Timer") as IMyTimerBlock;
            CodeTriggerTimer = GridTerminalSystem.GetBlockWithName("StateMaschine Timer") as IMyTimerBlock;
            List<IMyTextPanel> outputPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(outputPanels, x => x.CustomName.Contains("Output"));
            lcdHandler = new LCDClass(outputPanels, this);
            mainStateMaschine = CreateStateMaschine();
            currentState = State.Idle;
            running = true;
            //------------------------------------------------------------------------


            //Check if logic part is missing------------------------------------------
            if (scriptTimer == null || CodeTriggerTimer == null || mainStateMaschine == null)
            {
                lcdHandler.logMessage("State Maschine or/and Timers not found, are the names correct?", Labels.ERROR);
            }
            else
            {
                lcdHandler.logMessage("State Maschine and Timers found and activ", Labels.BOOTUP);
            }
            //---------------------------------------------------------------------------
            

            //Getting all GG at once to check if they are there--------------------------
            List<IMyGravityGenerator> Dis = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(Dis, x => x.CustomName.Contains("Gravity Generator DIS"));
            List<IMyGravityGenerator> RL = new List<IMyGravityGenerator>();
            GridTerminalSystem.GetBlocksOfType(RL, x => x.CustomName.Contains("Gravity Generator le/ri"));
            UpDown[0, 0] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FR") as IMyGravityGenerator;
            UpDown[0, 1] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do FL") as IMyGravityGenerator;
            UpDown[1, 0] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BR") as IMyGravityGenerator;
            UpDown[1, 1] = GridTerminalSystem.GetBlockWithName("Gravity Generator up/do BL") as IMyGravityGenerator;
            List<Gravity> Fr = new List<Gravity>();
            List<Gravity> Ba = new List<Gravity>();
            try
            {
                Fr.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator fr 1 +") as IMyGravityGenerator, 6, 16, 35));
                Ba.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator ba 1 +") as IMyGravityGenerator, 6, 16, 35));
                Fr.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator fr 2 -") as IMyGravityGenerator, 3, 17, 30));
                Ba.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator ba 2 -") as IMyGravityGenerator, 3, 17, 30));
                Fr.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator fr 3 +") as IMyGravityGenerator, 8, 11, 30));
                Ba.Add(new Gravity(GridTerminalSystem.GetBlockWithName("Gravity Generator ba 3 +") as IMyGravityGenerator, 8, 11, 30));
                GGDis = new GeneratorsUni(Dis, 20, 15, 10);
                GGRL = new GeneratorsUni(RL, 30, 13.6f, 10);

                //TODO neuinitierung der liste nötig??
                List<IMyGravityGenerator> UpDo = new List<IMyGravityGenerator>();
                //------------------------------------
                foreach (IMyGravityGenerator gg in UpDown)
                {
                    UpDo.Add(gg);
                }
                GGUuDo = new GeneratorsUni(Dis, 10, 30, 7.5f);
                GGFr = new GeneratorsDiv(Fr);
                GGBa = new GeneratorsDiv(Ba);
                lcdHandler.logMessage("GGs found and activ", Labels.BOOTUP);
                GGEnabled = true;
            }
            catch (NullReferenceException)
            {
                lcdHandler.logMessage("GGs could not be enabled, GG missing", Labels.ERROR);
            }
            //-------------------------------------------------------------------------------
            


            //Get control elements and check if they are missing------------------------------
            HangarSensor = GridTerminalSystem.GetBlockWithName("Sensor Hangar") as IMySensorBlock;
            Reference = GridTerminalSystem.GetBlockWithName("Hangar Reference") as IMyShipController;
            if (HangarSensor == null || Reference == null)
            {
                lcdHandler.logMessage("GGs could not be enabled, Sensor or/and Hangar Reference missing", Labels.ERROR);
            }
            //--------------------------------------------------------------------------------

            //Start the main code
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

                    lcdHandler.logMessage("Changing into waiting mode");
                }
                else if (mainStateMaschine[(int)currentState - 1].nextState == State.None)
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
                    currentState = mainStateMaschine[(int)currentState - 1].nextState;
                    mainStateMaschine[(int)State.OpenHangar - 1].nextState = State.None;
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
                    if (parts[0] == mainStateMaschine[(int)currentState - 1].currentState.ToString())
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
                            mainStateMaschine[(int)currentState - 1].nextState = (State)Enum.Parse(typeof(State), parts[2]);
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




        
            
        public void Capture()
        {
            Vector3D positon = HangarSensor.LastDetectedEntity.Position-Reference.GetPosition();
            Echo(Convert.ToString(positon.X));
            Echo(Convert.ToString(positon.Y));
            Echo(Convert.ToString(positon.Z));
        }


        


        

        
    }
}