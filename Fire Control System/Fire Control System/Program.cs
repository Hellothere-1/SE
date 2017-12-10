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
        //static string DisplayName = "LCD";           //Name of display used for output                                  
        static string Controller = "Remote";  //Name of a forward facing ship control block                                 
        static string Cockpit = "Command";
        static string PodDesignator = "Designator Command";

        static string Azimuth = "Pod_Azimuth";
        static string Elevation = "Pod_Elevation";

        static string CoreName = "T_Turret";
        static string DesignatorName = "Designator";

        static string Gyro = "Gyro";

        static float GyroFactor = 2.6f;


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////                              

        GyroscopeSystem Gyroscopes;
        List<IMyTerminalBlock> Cockpits = new List<IMyTerminalBlock>();

        IMyShipController controller;
        IMyShipController designator;
        DesignatorTurret designatorTurret;

        IMyMotorStator r_azimuth;
        IMyMotorStator r_elevation;

        int initcounter;


        List<IMyThrust> TForward = new List<IMyThrust>();
        List<IMyThrust> TBackward = new List<IMyThrust>();
        List<IMyThrust> TRight = new List<IMyThrust>();
        List<IMyThrust> TLeft = new List<IMyThrust>();
        List<IMyThrust> TUp = new List<IMyThrust>();
        List<IMyThrust> TDown = new List<IMyThrust>();

        List<Turret> Turrets = new List<Turret>();
        List<IMyLargeTurretBase> Designators = new List<IMyLargeTurretBase>();

        TargetList targetList;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////                             


        void Init()
        {
            initcounter--;
            var Rotors = new List<IMyMotorStator>();
            GridTerminalSystem.GetBlocksOfType(Rotors);
            Turrets[initcounter].Setup(this,Rotors,targetList);
            
        }


        public Program()
        {
            {
                targetList = new TargetList(this);

                controller = (IMyShipController)(GridTerminalSystem.GetBlockWithName(Controller));
                designator = (IMyShipController)(GridTerminalSystem.GetBlockWithName(PodDesignator));

                GridTerminalSystem.SearchBlocksOfName(Cockpit, Cockpits, x => x is IMyShipController);

                r_azimuth = (IMyMotorStator)(GridTerminalSystem.GetBlockWithName(Azimuth));
                r_elevation = (IMyMotorStator)(GridTerminalSystem.GetBlockWithName(Elevation));

                var Gyros = new List<IMyGyro>();
                GridTerminalSystem.GetBlocksOfType(Gyros,x=>(x.CustomName.Contains(Gyro)&&x.CubeGrid==controller.CubeGrid));

                Gyroscopes = new GyroscopeSystem(Gyros, controller);

                GridTerminalSystem.GetBlocksOfType(TForward, (x => (x.CubeGrid == controller.CubeGrid || x.CustomName.Contains("S_Thruster")) && (controller.WorldMatrix.Forward.Dot(x.WorldMatrix.Backward) > 0.65)));
                GridTerminalSystem.GetBlocksOfType(TBackward, (x => (x.CubeGrid == controller.CubeGrid || x.CustomName.Contains("S_Thruster")) && (controller.WorldMatrix.Backward.Dot(x.WorldMatrix.Backward) > 0.65)));
                GridTerminalSystem.GetBlocksOfType(TRight, (x => (x.CubeGrid == controller.CubeGrid || x.CustomName.Contains("S_Thruster")) && (controller.WorldMatrix.Right.Dot(x.WorldMatrix.Backward) > 0.65)));
                GridTerminalSystem.GetBlocksOfType(TLeft, (x => (x.CubeGrid == controller.CubeGrid || x.CustomName.Contains("S_Thruster")) && (controller.WorldMatrix.Left.Dot(x.WorldMatrix.Backward) > 0.65)));
                GridTerminalSystem.GetBlocksOfType(TUp, (x => (x.CubeGrid == controller.CubeGrid || x.CustomName.Contains("S_Thruster")) && (controller.WorldMatrix.Up.Dot(x.WorldMatrix.Backward) > 0.65)));
                GridTerminalSystem.GetBlocksOfType(TDown, (x => (x.CubeGrid == controller.CubeGrid || x.CustomName.Contains("S_Thruster")) && (controller.WorldMatrix.Down.Dot(x.WorldMatrix.Backward) > 0.65)));

                var TurretCores = new List<IMyTerminalBlock>();

                GridTerminalSystem.SearchBlocksOfName(CoreName, TurretCores);

                foreach (IMyTerminalBlock core in TurretCores)
                {
                    Turret turret = new Turret(core,controller);
                    Turrets.Add(turret);
                }
                initcounter = Turrets.Count;

                GridTerminalSystem.GetBlocksOfType(Designators);

                initcounter = Turrets.Count;
                
            }
        }

        void Main(string arg)
        {

            if (initcounter > 0)
            {
                Init();
                return;
            }

            bool dampeners = designator.DampenersOverride;

            Vector3D v_lin = GetShipVelocity(controller);
            Vector3D v_ang = GetShipAngularVelocity(controller);

            Vector3D gravity = GetGravity(controller);

            Vector3D input_lin = new Vector3D(0, 0, 0);
            Vector3D input_ang = new Vector3D(0, 0, 0);

            foreach (IMyShipController cockpit in Cockpits)
            {
                input_lin += cockpit.MoveIndicator;
                Vector2 input_mouse = cockpit.RotationIndicator / -10;
                input_ang += new Vector3D(input_mouse.X, input_mouse.Y, -cockpit.RollIndicator);
            }

            float azimuth = GetAngle(r_azimuth);
            float elevation = GetAngle(r_elevation);

            double v_az = input_ang.Y * 0.4 - (v_ang.Y + (Math.Sin(azimuth) * v_ang.X + Math.Cos(azimuth) * v_ang.Z) * Math.Tan(elevation));
            double v_el = input_ang.X * 0.4 + (v_ang.Z * Math.Sin(azimuth) - v_ang.X * Math.Cos(azimuth));

            r_azimuth.TargetVelocityRad = Convert.ToSingle(v_az);
            r_elevation.TargetVelocityRad = Convert.ToSingle(v_el);

            if ((Math.Abs(Math.Sin(azimuth)) + 0.2) * Math.Abs(elevation) > 1.3)
                input_ang.Z -= Math.Sin(azimuth) * elevation;

           
            Gyroscopes.SetRotation( GyroFactor * Convert.ToSingle(input_lin.Y),GyroFactor * Convert.ToSingle(input_lin.X),GyroFactor * Convert.ToSingle(input_ang.Z));


            for (int j = 0; j < TForward.Count; j++)
            {
                TForward[j].SetValue<float>("Override", Convert.ToSingle(input_lin.Z) * -100);
            }
            for (int j = 0; j < TBackward.Count; j++)
            {
                TBackward[j].SetValue<float>("Override", Convert.ToSingle(input_lin.Z) * 100);
            }
            Vector3D target = designator.GetPosition() + designator.WorldMatrix.Forward * 400;

            foreach (IMyLargeTurretBase turret in Designators)
            {
                if (turret.HasTarget)
                {
                    targetList.Add(turret.GetTargetedEntity());
                }
            }
                

            targetList.tick();

            Vector3D velocity = Vector3D.Zero;
            


            if (targetList.Count() > 0)
            {
                target = targetList.ReturnTargetPosition();
                velocity = targetList.ReturnTargetVelocity();
            }


            foreach (Turret turret in Turrets)
            {
                turret.Target(target,velocity, this, targetList.Count() > 0);
            }


            Echo("Number of Targets found: " + Convert.ToString(targetList.Count()));

        }

        public Vector3D GetShipVelocity(IMyShipController dataBlock)
        {
            var worldLocalVelocities = dataBlock.GetShipVelocities().LinearVelocity;
            var worldToAnchorLocalMatrix = Matrix.Transpose(dataBlock.WorldMatrix.GetOrientation());
            return Vector3D.Transform(worldLocalVelocities, worldToAnchorLocalMatrix);
        }

        public Vector3D GetShipAngularVelocity(IMyShipController dataBlock)
        {
            var worldLocalVelocities = dataBlock.GetShipVelocities().AngularVelocity;
            var worldToAnchorLocalMatrix = Matrix.Transpose(dataBlock.WorldMatrix.GetOrientation());
            return Vector3D.Transform(worldLocalVelocities, worldToAnchorLocalMatrix);
        }

        public Vector3D GetGravity(IMyShipController dataBlock)
        {
            var worldLocalGravity = dataBlock.GetNaturalGravity();
            var worldToAnchorLocalMatrix = Matrix.Transpose(dataBlock.WorldMatrix.GetOrientation());
            return Vector3D.Transform(worldLocalGravity, worldToAnchorLocalMatrix);
        }

        float GetAngle(IMyTerminalBlock rotor)
        {
            if (rotor != null)
                return ((IMyMotorStator)rotor).Angle; ;
            return 0;
        }

        public static double ConvertToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static float ConvertToRadians(float degrees)
        {
            float radians = (Convert.ToSingle(Math.PI) / 180) * degrees;
            return radians;
        }

        public float limit(float input, float lower, float upper)
        {
            if (input > upper)
                return upper;
            if (input < lower)
                return lower;
            return input;
        }
        public float limit(float input, float limit)
        {
            if (input > limit)
                return limit;
            if (input < -limit)
                return -limit;
            return input;
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

        public Vector3D GetBurnVector(Vector3D targetPosition, Vector3D targetVelocity,IMyRemoteControl core, Vector3 offset)
        {
            Vector3D targetVector = targetPosition - core.GetPosition();
            double timeToTarget = targetVector.Length()/100;

            return targetVector + timeToTarget * (targetVelocity - 0.9 * core.GetShipVelocities().LinearVelocity);
        }
    }
}