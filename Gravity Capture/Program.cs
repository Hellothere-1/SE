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
        

        //Needed classes------------------------------
        LCDClass lcdHandler;
        StateMaschine stateHandler;
        Hangar hangarHandler;
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
            List<IMyTextPanel> outputPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(outputPanels, x => x.CustomName.Contains("Output"));
            lcdHandler = new LCDClass(outputPanels, this);
            stateHandler = new StateMaschine(lcdHandler, this);
            hangarHandler = new Hangar(lcdHandler, this);
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
            hangarHandler.run(argument);
            //stateHandler.run(argument);
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
