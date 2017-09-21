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
        public class Hangar
        {
            LCDClass lcdHandler;
            List<IMyGasTank> oxygenHangarTanks = new List<IMyGasTank>();
            List<IMyGasTank> oxygenShipTanks = new List<IMyGasTank>();
            List<IMyGasGenerator> oxygenGenerators = new List<IMyGasGenerator>();
            List<IMyAirVent> ventsHangar = new List<IMyAirVent>();
            List<IMyAirtightHangarDoor> hangarDoors = new List<IMyAirtightHangarDoor>();
            enum AIR_STATE { Empty, Depressurizing, Pressurizing, Full }
            AIR_STATE hangarState;
            Program parent;
            float currentAirLevel;
            bool OxyOverflow = false;

            public Hangar(LCDClass lcd ,Program par)
            {
                lcdHandler = lcd;
                parent = par;
                parent.GridTerminalSystem.GetBlocksOfType(ventsHangar, x => x.CustomName.Contains("Hangar"));
                parent.GridTerminalSystem.GetBlocksOfType(oxygenHangarTanks, x => x.CustomName.Contains("Hangar"));
                parent.GridTerminalSystem.GetBlocksOfType(oxygenShipTanks, x => !x.CustomName.Contains("Hangar"));
                parent.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);
                IMyBlockGroup doors = parent.GridTerminalSystem.GetBlockGroupWithName("Hangar Doors");
                doors.GetBlocksOfType(hangarDoors);
                if (ventsHangar[0].GetOxygenLevel() == 1)
                {
                    hangarState = AIR_STATE.Full;
                }
                else
                {
                    hangarState = AIR_STATE.Empty;
                }
            }


            public void run(string argument)
            {
                if (argument == "Air_depressurize" && (hangarState == AIR_STATE.Full || hangarState == AIR_STATE.Pressurizing))
                {
                    SetVentDepressurize(true);
                    currentAirLevel = ventsHangar[0].GetOxygenLevel();
                    hangarState = AIR_STATE.Depressurizing;
                    lcdHandler.logMessage("Hangar is now depressurizing");
                }


                if (argument == "Air_pressurize" && (hangarState == AIR_STATE.Empty || hangarState == AIR_STATE.Depressurizing))
                {
                    
                    currentAirLevel = ventsHangar[0].GetOxygenLevel();
                    hangarState = AIR_STATE.Pressurizing;
                    lcdHandler.logMessage("Hangar is now pressurizing");
                    SetHangarDoors(false);
                    EnableGasGenerator(false);
                    EnableGasTank(true, oxygenHangarTanks);
                    EnableGasTank(false, oxygenShipTanks);
                    SetVentDepressurize(false);
                }


                if (hangarState == AIR_STATE.Depressurizing || hangarState == AIR_STATE.Pressurizing)
                {

                    if (currentAirLevel == (ventsHangar[0].GetOxygenLevel() * 100))
                    {
                        if (hangarState == AIR_STATE.Depressurizing)
                        {
                            lcdHandler.logMessage("Tanks full, reguiding O²", Labels.WARNING);
                            EnableGasGenerator(false);
                            EnableGasTank(true, oxygenHangarTanks);

                            if (OxyOverflow)
                            {
                                lcdHandler.logMessage("Cannot transfer O², emergency opening of hangar doors", Labels.ERROR);
                                SetHangarDoors(true);
                            }
                            OxyOverflow = true;
                        }
                        else
                        {
                            if (hangarDoors[0].Status == DoorStatus.Closed)
                            {
                                lcdHandler.logMessage("Dedicated tanks empty, activating Generators and main tanks");
                                EnableGasTank(false, oxygenHangarTanks);
                                EnableGasTank(true, oxygenShipTanks);
                                EnableGasGenerator(true);
                            }
                            
                        }
                        
                    }
                    else
                    {
                        OxyOverflow = false;
                    }


                    currentAirLevel = ventsHangar[0].GetOxygenLevel() * 100;
                    lcdHandler.logMessage("Current O² Level: " + (currentAirLevel).ToString() + "%");


                    if (currentAirLevel == 100 && hangarState == AIR_STATE.Pressurizing)
                    {
                        hangarState = AIR_STATE.Full;
                        lcdHandler.logMessage("Hangar fully pressurized");
                        EnableGasTank(false, oxygenHangarTanks);
                        EnableGasTank(true, oxygenShipTanks);
                        EnableGasGenerator(true);
                    }


                    if (currentAirLevel == 0 && hangarState == AIR_STATE.Depressurizing)
                    {
                        hangarState = AIR_STATE.Empty;
                        lcdHandler.logMessage("Hangar fully depressurized");
                    }
                }
            }


            void SetVentDepressurize(bool enable)
            {
                foreach (IMyAirVent vent in ventsHangar)
                {
                    vent.Depressurize = enable;
                }
            }

            void EnableGasTank(bool enable, List<IMyGasTank> tanks)
            {
                foreach (IMyGasTank oxyTan in tanks)
                {
                    oxyTan.Enabled = enable;
                }
            }

            void EnableGasGenerator(bool enable)
            {
                foreach (IMyGasGenerator oxyGen in oxygenGenerators)
                {
                    oxyGen.Enabled = enable;
                }
            }

            void SetHangarDoors(bool open)
            {
                if ((hangarDoors[0].Status == DoorStatus.Closed || hangarDoors[0].Status == DoorStatus.Closing) && !open)
                {
                    return;
                }
                if ((hangarDoors[0].Status == DoorStatus.Open || hangarDoors[0].Status == DoorStatus.Opening) && open)
                {
                    return;
                }

                foreach (IMyAirtightHangarDoor door in hangarDoors)
                {
                    door.ToggleDoor();
                }

            }
        }
    }
}
