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
        public class DesignatorTurret
        {
            public IMyLargeTurretBase turret;
            private List<IMyCameraBlock> Cameras=new List<IMyCameraBlock>();
            private float scanRange;
            private double currentScanRange;

            public DesignatorTurret(IMyLargeTurretBase block, List<IMyCameraBlock> ShipCameras)
            {
                turret = block;
                scanRange = turret.Range + 10;
                currentScanRange = scanRange;

                foreach (IMyCameraBlock camera in ShipCameras)
                    if (camera.CubeGrid == turret.CubeGrid)
                    {
                        camera.EnableRaycast = true;
                        Cameras.Add(camera);
                    }
            }

            public MyDetectedEntityInfo? CheckTarget(TargetList targetList)
            { 
                if (!turret.IsShooting)
                {
                    return null;
                }

                Vector3D coords = GetTargetCoords(currentScanRange);

                foreach (IMyCameraBlock c in Cameras)
                {
                    if (c.CanScan(coords))
                    {
                        MyDetectedEntityInfo temp = c.Raycast(coords);
                        if (!temp.IsEmpty())
                        {
                            if(targetList.IsFriendly(temp))
                            {
                                currentScanRange = 32;
                                return null;
                            }

                            currentScanRange = (temp.Position - turret.GetPosition()).Length();

                            targetList.Add(temp);

                            return temp;
                        }
                        currentScanRange = scanRange;
                    }
                }
                return null;


            }

            public Vector3D GetTargetCoords(double distance)
            {
                Vector3D target = turret.GetPosition();

                target += turret.WorldMatrix.Up * Math.Sin(turret.Elevation) * distance;
                double cos = Math.Cos(turret.Elevation);
                target += turret.WorldMatrix.Forward * cos * Math.Cos(turret.Azimuth) * distance;
                target += turret.WorldMatrix.Left * cos * Math.Sin(turret.Azimuth) * distance;
                return target;
            }
        }
    }
}
