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
        public const float ShipUpdateInterval = 2;

        public static MatchRecorder CurrentMatchRecorder;
        public static MatchRecorder InitializingMatchRecorder;

        public readonly AcmiFile AcmiFile;
        private bool ShipsRegistered = false;

        readonly long GameStartTimestamp;
        readonly Dictionary<string, float> ShipLastTimestamp = new Dictionary<string, float>();
        readonly Dictionary<string, bool> ShipLastDead = new Dictionary<string, bool>();
        readonly HashSet<string> RegisteredShips = new HashSet<string>();


        readonly Dictionary<int, ShellInfo> ActiveShells = new Dictionary<int, ShellInfo>();
        readonly Dictionary<int, RepairableState> RepairableStates = new Dictionary<int, RepairableState>();

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
            
            if (!RegisteredShips.Contains(id))
            {
                AcmiFile.AddShipInfo(ship);

                RegisteredShips.Add(id);
                ShipLastDead[id] = false;
                ShipLastTimestamp[id] = int.MinValue;
            }

            // TODO: Check for movement. If angular or positional difference is large apply update.
            // Write ship position to file if time has passed.
            if (timestamp - ShipLastTimestamp[id] >= ShipUpdateInterval ||
                ShipLastDead[id] != ship.IsDead)
            {
                AcmiFile.AddShipPosition(ship, timestamp);

                if (ship.IsDead && !ShipLastDead[id])
                {
                    AcmiFile.AddShipDeath(ship, timestamp);
                    AcmiFile.Flush();
                }
                ShipLastTimestamp[id] = timestamp;
                ShipLastDead[id] = ship.IsDead;
            }
        }

        public void ShellFired(BaseShell shell)
        {
            if (ActiveShells.ContainsKey(shell.GetInstanceID()))
            {
                // Make sure the instance is not active already
                ShellDetonated(shell);
            }
            Console.WriteLine($"SHELL FIRED: {shell.GetInstanceID()}");
            float timestamp = GetTimestampSeconds();
            ActiveShells[shell.GetInstanceID()] = new ShellInfo(shell, timestamp);
            //ActiveShells.Add(shell.GetInstanceID(), new ShellInfo(shell, timestamp));
            AcmiFile.AddShell(shell, timestamp);
        }

        public void ShellDetonated(BaseShell shell)
        {
            Console.WriteLine($"SHELL DETONATED: {shell.GetInstanceID()}");
            float timestamp = GetTimestampSeconds();
            AcmiFile.AddShellDetonation(shell, timestamp, ActiveShells[shell.GetInstanceID()]);
            ActiveShells.Remove(shell.GetInstanceID());
        }

        public void RepairableUpdate(Repairable repairable)
        {
            if (repairable.Ship == null) return;
            if (!RegisteredShips.Contains(AcmiFile.GetShipACMIId(repairable.Ship))) return;

            int networkId = repairable.NetworkId;
            RepairableState newState = new RepairableState(repairable);
            if (!RepairableStates.ContainsKey(networkId) ||
                !RepairableStates[networkId].Equals(newState))
            {
                AcmiFile.AddRepairableUpdate(repairable, GetTimestampSeconds(), newState);
                RepairableStates[networkId] = newState;
            }

        }


        public static void InitializeRecorder(Mission mission)
        {
            InitializingMatchRecorder = new MatchRecorder(mission);
        }

        public static void StartRecorder()
        {
            CurrentMatchRecorder = InitializingMatchRecorder;
            InitializingMatchRecorder = null;
        }

        public static void StopRecorder()
        {
            if (CurrentMatchRecorder == null) return;
            CurrentMatchRecorder?.AcmiFile.Flush();
            CurrentMatchRecorder = null;
            MuseWorldClient.Instance.ChatHandler.AddMessage(ChatMessage.Console("Replay saved."));
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

    struct RepairableState
    {
        public int Health;
        public int MaxHealth;
        public bool Broken;
        public int RebuildProgress;
        public bool OnCooldown;
        public RepairableState(Repairable repairable)
        {
            Health = (int)repairable.Health;
            MaxHealth = (int)repairable.MaxHealth;
            Broken = repairable.NoHealth;
            if (Broken && repairable.RepairProgress == 1) RebuildProgress = 0;
            else if (Broken) RebuildProgress = ((int)(repairable.RepairProgress * 100));
            else RebuildProgress = 100;
            OnCooldown = Broken ? false : repairable.RepairProgress != 1;
        }
    }

    struct ShipState
    {
        public Vector3 Position;
        public Vector3 Forward;
        public bool IsDead;
        public float Timestamp;
        public ShipState(Ship ship, float timestamp)
        {
            Position = ship.Position;
            Forward = ship.Forward;
            IsDead = ship.IsDead;
            Timestamp = timestamp;
        }
        //public bool PositionUpdateNeeded(ShipState newState)
        //{
        //    if (Timestamp - newState.Timestamp >= MatchRecorder.ShipUpdateInterval) return true;
        //    if (IsDead != newState.IsDead) return true;
        //    //if (SHIP_MOVED_TOO_MUCH) return true;
        //    return false;
        //}
    }
}
