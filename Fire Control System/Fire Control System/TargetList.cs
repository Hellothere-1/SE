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
        public class TargetList
        {
            private struct EntityContainer
            {
                public MyDetectedEntityInfo entityInfo;
                public int timeStamp;

                public EntityContainer(MyDetectedEntityInfo myDetectedEntityInfo, int time)
                {
                    entityInfo = myDetectedEntityInfo;
                    timeStamp = time;
                }
            }

            private Dictionary<long, EntityContainer> dict = new Dictionary<long, EntityContainer>();

            private HashSet<long> friendlies = new HashSet<long>();

            private int currentTime = 0;
            private int cleanupTimer = 0;

            public TargetList(Program program)
            {
                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                program.GridTerminalSystem.GetBlocksOfType(connectors);

                foreach (IMyShipConnector connector in connectors)
                    friendlies.Add(connector.CubeGrid.EntityId);

                List<IMyMotorStator> rotors = new List<IMyMotorStator>();
                program.GridTerminalSystem.GetBlocksOfType(rotors);

                foreach (IMyMotorStator rotor in rotors)
                {
                    friendlies.Add(rotor.CubeGrid.EntityId);
                    if (rotor.IsAttached)
                        friendlies.Add(rotor.TopGrid.EntityId);
                }

                List<IMyPistonBase> pistons = new List<IMyPistonBase>();
                program.GridTerminalSystem.GetBlocksOfType(pistons);

                foreach (IMyPistonBase piston in pistons)
                {
                    friendlies.Add(piston.CubeGrid.EntityId);
                    if (piston.IsAttached)
                        friendlies.Add(piston.TopGrid.EntityId);
                }
            }

            public void Add (MyDetectedEntityInfo entityInfo)
            {
                dict[entityInfo.EntityId]=new EntityContainer(entityInfo,currentTime);
            }

            public void tick()
            {
                currentTime++;
                
                if (cleanupTimer >= 5)
                    cleanup();
                else
                    cleanupTimer++;
            }

            public void cleanup()
            {
                cleanupTimer = 0;
               
                foreach (KeyValuePair<long, EntityContainer> item in dict)
                {
                    if (currentTime - item.Value.timeStamp > 3)
                    {
                        dict.Remove(item.Key);
                        return;
                    }
                }
            }

            public int Count()
            {
                return dict.Count;
            }

            public Vector3D ReturnTargetPosition()
            {
                EntityContainer item = dict.First().Value;
                MyDetectedEntityInfo entity = item.entityInfo;

                return entity.Position+entity.Velocity*(currentTime-item.timeStamp)/60;
            }

            public Vector3D ReturnTargetVelocity()
            {
                EntityContainer item = dict.First().Value;
                MyDetectedEntityInfo entity = item.entityInfo;

                return  entity.Velocity;
            }

            public bool IsFriendly(MyDetectedEntityInfo entityInfo)
            {
                return friendlies.Contains(entityInfo.EntityId);
            }
        }
    }
}
