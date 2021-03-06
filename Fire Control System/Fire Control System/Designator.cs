﻿using Sandbox.Game.EntityComponents;
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
        public class Designator
        {
            public DesignatorTurret turret;
            private List<IMyMotorStator> Azimuth = new List<IMyMotorStator>();
            private List<IMyMotorStator> Elevation = new List<IMyMotorStator>();
            private List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>();

            public Designator(IMyLargeTurretBase turret)
            {
                //turret = turret;
            }
        }
    }
}
