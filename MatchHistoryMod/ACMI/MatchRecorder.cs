using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MuseBase.Multiplayer.Unity;
using MuseBase.Multiplayer;
using UnityEngine;
using HarmonyLib;

namespace MatchHistoryMod.ACMI
{
    class MatchRecorder
    {
        const float ShipUpdateInterval = 2;

        public static MatchRecorder CurrentMatchRecorder;

        public readonly AcmiFile AcmiFile;

        readonly long GameStartTimestamp;
        readonly Dictionary<string, float> ShipLastTimestamp = new Dictionary<string, float>();
        readonly Dictionary<string, bool> ShipLastDead = new Dictionary<string, bool>();
        readonly Dictionary<int, ShellInfo> ActiveShells = new Dictionary<int, ShellInfo>();

        public MatchRecorder(Mission mission)
        {
            //int mapId = MatchLobbyView.Instance.Map.Id;
            //string mapName = MatchLobbyView.Instance.Map.NameText.En;
            int mapId = mission.Map.Id;
            string mapName = mission.Map.NameText.En;
            FileLog.Log($"MAP LOADED: {mapId} {mapName}");

            var date = DateTime.Now.ToUniversalTime();

            AcmiFile = new AcmiFile(mapId, mapName, date);
            AcmiFile.AddHeader(mapId, mapName, date);

            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            GameStartTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public float GetTimestampSeconds()
        {
            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond - GameStartTimestamp;
            return (float)timestamp / 1000.0f;
        }

        public void UpdateShipPosition(Ship ship)
        {
            string id = AcmiFile.GetShipACMIId(ship);
            float timestamp = GetTimestampSeconds();

            // Write ship position to file if time has passed.
            if (!ShipLastTimestamp.ContainsKey(id) || 
                timestamp - ShipLastTimestamp[id] >= ShipUpdateInterval ||
                ShipLastDead[id] != ship.IsDead)
            {
                AcmiFile.AddShipPosition(ship, timestamp);

                if (ShipLastDead.ContainsKey(id) && ship.IsDead && !ShipLastDead[id])
                {
                    AcmiFile.AddShipDeath(ship, timestamp);
                }

                ShipLastTimestamp[id] = timestamp;
                ShipLastDead[id] = ship.IsDead;
            }
        }

        public void ShellFired(BaseShell shell)
        {
            Console.WriteLine($"SHELL FIRED: {shell.GetInstanceID()}");
            float timestamp = GetTimestampSeconds();
            ActiveShells.Add(shell.GetInstanceID(), new ShellInfo(shell, timestamp));
            AcmiFile.AddShell(shell, timestamp);
        }

        public void ShellDetonated(BaseShell shell)
        {
            Console.WriteLine($"SHELL DETONATED: {shell.GetInstanceID()}");
            float timestamp = GetTimestampSeconds();
            AcmiFile.AddShellDetonation(shell, timestamp, ActiveShells[shell.GetInstanceID()]);
            ActiveShells.Remove(shell.GetInstanceID());
        }

        public static void Start(Mission mission)
        {
            CurrentMatchRecorder = new MatchRecorder(mission);
        }
        public static void Stop()
        {
            CurrentMatchRecorder?.AcmiFile.Flush();
            CurrentMatchRecorder = null;
        }
    }

    struct ShellInfo
    {
        public Vector3 LaunchPosition;
        public float LaunchTimestamp;
        public ShellInfo(BaseShell shell, float timestamp)
        {
            LaunchPosition = shell.position;
            LaunchTimestamp = timestamp;
        }
    }

    struct RepairableStatus
    {
        public float Health;
        public float MaxHealth;
        public bool Broken;
        public float RepairProgress;
        public RepairableStatus(Repairable repairable)
        {
            Health = repairable.Health;
            MaxHealth = repairable.MaxHealth;
            Broken = repairable.NoHealth;
            try
            {
                RepairProgress = (repairable.NoHealth ? repairable.Network.MyView.GetAppFixed(50 - 16) : 1);
            }
            catch
            {
                RepairProgress = 1.0f;
            }
        }
    }
}
