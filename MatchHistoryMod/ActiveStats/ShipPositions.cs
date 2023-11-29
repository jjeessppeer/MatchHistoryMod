using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;

namespace MatchHistoryMod
{
    public class ShipPositionData 
    {
        public int TeamIdx;
        public int ShipIdx;
        public string ShipName;
        public List<long> Timestamp;
        public List<Vector3> Position;
        public List<Vector3> Forwards;
        public List<Vector3> Velocity;
        public List<bool> Dead;

        public ShipPositionData(int teamIdx, int shipIdx)
        {
            TeamIdx = teamIdx;
            ShipIdx = shipIdx;
            ShipName = "";
            Timestamp = new List<long>();
            Position = new List<Vector3>();
            Forwards = new List<Vector3>();
            Velocity = new List<Vector3>();
            Dead = new List<bool>();
        }

        public void AddDataPoint(Ship ship)
        {
            Timestamp.Add(MatchDataRecorder.GetActiveGameTimestamp());
            Forwards.Add(RoundVector3(ship.Forward, 1));
            Position.Add(RoundVector3(ship.Position, 0));
            Velocity.Add(RoundVector3(ship.WorldVelocity, 1));
            Dead.Add(ship.IsDead);
            if (ShipName == "") ShipName = ship.name;
        }

        private static Vector3 RoundVector3(Vector3 vec, int decimals = 3)
        {
            return new Vector3(
                (float)Math.Round(vec.x, decimals),
                (float)Math.Round(vec.y, decimals),
                (float)Math.Round(vec.z, decimals));
        }

        public static void TakeSnapshot(Ship ship, List<ShipPositionData> shipDataLists)
        {
            const long SNAPSHOT_INTERVAL = 2000;
            foreach(var pd in shipDataLists)
            {
                if (pd.TeamIdx != ship.Side || pd.ShipIdx != ship.CrewIndex) continue;
                long timestamp = MatchDataRecorder.GetActiveGameTimestamp();
                if (timestamp - pd.Timestamp.Last() >= SNAPSHOT_INTERVAL || ship.IsDead != pd.Dead.Last())
                {
                    pd.AddDataPoint(ship);
                }
                return;
            }
            ShipPositionData newShipData = new ShipPositionData(ship.Side, ship.CrewIndex);
            newShipData.AddDataPoint(ship);
            shipDataLists.Add(newShipData);
        }
    }
}
