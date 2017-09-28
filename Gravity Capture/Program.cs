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
        bool GravityControlOperational = false;
        bool StateMaschienOperational = false;
        bool OxygenControlOperational = false;

        bool GravityTriggerNeeded = false;
        bool StateTriggerNeeded = false;
        bool OxygenTriggerNeeded = false;

        short OxygenCounter = 0;
        

        //Needed classes------------------------------
        LCDClass lcdHandler;
        StateMaschine stateHandler;
        OxygenControl oxygenHandler;
        GravityControl gravityHandler;
        //-----------------------------------------------------

        public Program()
        {
            //Initialize logic for state maschine ------------------------------------
            lcdHandler = new LCDClass(this);
            stateHandler = new StateMaschine(lcdHandler, this);
            oxygenHandler = new OxygenControl(lcdHandler, this);
            gravityHandler = new GravityControl(lcdHandler, this);
            //---------------------------------------------------------------------------

            StateMaschienOperational = stateHandler.isOperational();
            OxygenControlOperational = oxygenHandler.isOperational();
            GravityControlOperational = gravityHandler.isOperational();
            //Getting all GG at once to check if they are there--------------------------
            
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
            if (GravityControlOperational && (GravityTriggerNeeded || argument.StartsWith("GRA")))
            {
                gravityHandler.Capture();
            }
            if (OxygenControlOperational && ((OxygenTriggerNeeded && OxygenCounter >= 60) || argument.StartsWith("AIR")))
            {
                oxygenHandler.run(argument);
                OxygenCounter = 0;
            }
            else
            {
                OxygenCounter++;
            }
            if (StateMaschienOperational && (StateTriggerNeeded || argument.StartsWith("STM")))
            {
                stateHandler.run(argument);
            }
        }
    }
}