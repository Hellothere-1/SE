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
        public class OxygenControl
        {
            LCDClass lcdHandler;
            List<IMyGasTank> HangarOxyTanks = new List<IMyGasTank>();
            List<IMyGasTank> ShipGasTanks = new List<IMyGasTank>();
            List<IMyGasGenerator> oxygenGenerators = new List<IMyGasGenerator>();
            List<IMyAirVent> ventsHangar = new List<IMyAirVent>();
            List<IMyAirtightHangarDoor> hangarDoors = new List<IMyAirtightHangarDoor>();
            enum AIR_STATE {Error = -1, Empty, Depressurizing, Pressurizing, Full }
            AIR_STATE hangarState;
            Program parent;
            float currentAirLevel;
            bool OxyOverflow = false;
            bool AwaitHangarDoors = false;
            bool AlreadyReguided = false;

            public OxygenControl(LCDClass lcd ,Program par)
            {
                lcdHandler = lcd;
                parent = par;

                parent.GridTerminalSystem.GetBlocksOfType(ventsHangar, x => x.CustomName.Contains("Hangar"));
                parent.GridTerminalSystem.GetBlocksOfType(HangarOxyTanks, x => x.CustomName.Contains("Hangar"));
                parent.GridTerminalSystem.GetBlocksOfType(ShipGasTanks, x => !x.CustomName.Contains("Hangar"));
                IMyBlockGroup doors = parent.GridTerminalSystem.GetBlockGroupWithName("Hangar Doors");
                parent.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);
                try
                {
                    doors.GetBlocksOfType(hangarDoors);
                    hangarState = ventsHangar[0].GetOxygenLevel() == 1 ? AIR_STATE.Full : AIR_STATE.Empty;
                    EnableGasGenerator(false);
                    EnableGasTank(true, ShipGasTanks);
                    EnableGasTank(false, HangarOxyTanks);
                    lcdHandler.logMessage("Oxygen Control operational", Tags.OXY, Labels.BOOT);
                }
                catch (NullReferenceException)
                {
                    lcd.logMessage("Oxygen Control not operational, something is missing", Tags.OXY, Labels.cERR);
                    hangarState = AIR_STATE.Error;
                }
                
            }

            public bool isOperational()
            {
                if (hangarState == AIR_STATE.Error)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }


            public void run(string argument)
            {
                if (hangarState == AIR_STATE.Error)
                {
                    return;
                }

                if (argument == "Air_check")
                {
                    if (checkGasTanks())
                    {
                        lcdHandler.logMessage("Oxygen or Hydrogen levels low, start refilling", Tags.OXY);
                    }
                    else
                    {
                        lcdHandler.logMessage("Oxygen and Hydrogen level stable", Tags.OXY);
                    }
                    
                }

                if (argument == "Air_depressurize" && (hangarState == AIR_STATE.Full || hangarState == AIR_STATE.Pressurizing))
                {
                    SetVentDepressurize(true);
                    currentAirLevel = ventsHangar[0].GetOxygenLevel() * 100;
                    hangarState = AIR_STATE.Depressurizing;
                    lcdHandler.logMessage("Hangar is now depressurizing", Tags.OXY);
                    return;
                }


                if (argument == "Air_pressurize" && (hangarState == AIR_STATE.Empty || hangarState == AIR_STATE.Depressurizing))
                {
                    currentAirLevel = ventsHangar[0].GetOxygenLevel() * 100;
                    hangarState = AIR_STATE.Pressurizing;
                    lcdHandler.logMessage("Hangar is now pressurizing", Tags.OXY);
                    SetHangarDoors(false);
                    EnableGasTank(true, HangarOxyTanks);
                    EnableGasTank(false, ShipGasTanks);
                    SetVentDepressurize(false);
                    return;
                }

                if (currentAirLevel == 100 && hangarState == AIR_STATE.Pressurizing)
                {
                    hangarState = AIR_STATE.Full;
                    lcdHandler.logMessage("Current O² Level: 100%", Tags.OXY);
                    lcdHandler.logMessage("Hangar fully pressurized", Tags.OXY);
                    EnableGasTank(false, HangarOxyTanks);
                    EnableGasTank(true, ShipGasTanks);
                    return;

                }

                if (currentAirLevel == 0 && hangarState == AIR_STATE.Depressurizing)
                {
                    lcdHandler.logMessage("Current O² Level: 0%", Tags.OXY);
                    hangarState = AIR_STATE.Empty;
                    lcdHandler.logMessage("Hangar fully depressurized", Tags.OXY);
                    AlreadyReguided = false;
                    return;
                }


                if (hangarState == AIR_STATE.Depressurizing || hangarState == AIR_STATE.Pressurizing)
                {
                    if (currentAirLevel == (ventsHangar[0].GetOxygenLevel() * 100))
                    {
                        if (hangarState == AIR_STATE.Depressurizing)
                        {
                            if (OxyOverflow)
                            {
                                lcdHandler.logMessage("Cannot transfer O², emergency opening of hangar doors", Tags.OXY, Labels.cERR);
                                SetHangarDoors(true);
                                EnableGasTank(false, HangarOxyTanks);
                            }
                            else if(!AlreadyReguided)
                            {
                                lcdHandler.logMessage("Tanks full, reguiding O²", Tags.OXY, Labels.WARN);
                                EnableGasTank(true, HangarOxyTanks);
                                AlreadyReguided = true;
                            }
                            OxyOverflow = true;
                        }
                        else
                        {
                            if (currentAirLevel != 100)
                            {
                                if (hangarDoors[0].Status == DoorStatus.Closed && !AwaitHangarDoors)
                                {
                                    lcdHandler.logMessage("Dedicated tanks empty, activating main tanks", Tags.OXY);
                                    EnableGasTank(false, HangarOxyTanks);
                                    EnableGasTank(true, ShipGasTanks);
                                    AwaitHangarDoors = false;
                                }
                                else if (hangarDoors[0].Status == DoorStatus.Closed)
                                {
                                    lcdHandler.logMessage("Hangar Doors closed", Tags.OXY);
                                    AwaitHangarDoors = false;
                                }
                                else
                                {
                                    if (!AwaitHangarDoors)
                                    {
                                        lcdHandler.logMessage("Waiting for hangar doors to close", Tags.OXY);
                                    }
                                    AwaitHangarDoors = true;
                                    SetHangarDoors(false);
                                }
                            }
                        }
                    }
                    else
                    {
                        OxyOverflow = false;
                        lcdHandler.logMessage("Current O² Level: " + (currentAirLevel).ToString() + "%", Tags.OXY);
                    }
                    currentAirLevel = ventsHangar[0].GetOxygenLevel() * 100;
                }
            }


            bool checkGasTanks()
            {
                foreach(IMyGasTank oxyTan in ShipGasTanks)
                {
                    if (oxyTan.DetailedInfo.Split('\n')[0] == "Type: Oxygen Tank" && oxyTan.FilledRatio <= 0.5)
                    {
                        return true;
                    }
                    else if (oxyTan.FilledRatio <= 0.75)
                    {
                        return true;
                    }
                }
                return false;
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
                    if (oxyTan.DetailedInfo.Split('\n')[0] == "Type: Oxygen Tank")
                    {
                        oxyTan.Enabled = enable;
                    }
                    
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
