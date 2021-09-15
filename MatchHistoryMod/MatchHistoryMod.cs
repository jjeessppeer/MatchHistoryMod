using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;


using Newtonsoft.Json;

namespace MatchHistoryMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MatchHistoryMod : BaseUnityPlugin
    {
        public const string pluginGuid = "whereami.matchhistory.mod";
        public const string pluginName = "Match History Mod";
        public const string pluginVersion = "0.1";

        public void Awake()
        {
            Logger.LogInfo("Match history initializing.");
            FileLog.Log("Match history initializing");
            var harmony = new Harmony("testPatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class MatchHistoryRecorder
    {
        //private List<ShipData> 

        private static bool _MissionActive = false;
        private static List<ShipData> _ShipDatas = new List<ShipData>();

        static MatchHistoryRecorder()
        {
            FileLog.Log("Match recorder hello");
        }


        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(MatchView), "CheckCurrentMatchStatus")]
        //private static void OnMissionEnd()
        //{

        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Mission), "OnRemoteUpdate")]
        //private static void OnRemoteUpdatePrefix(Mission __instance)
        //{
        //    FileLog.Log($"Mission OnRemoteUpdatePre: {__instance.MatchInProgress}");
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Mission), "OnRemoteUpdate")]
        //private static void OnRemoteUpdatePostfix(Mission __instance)
        //{
        //    FileLog.Log($"Mission OnRemoteUpdatePst: {__instance.MatchInProgress}");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Ship), "OnDisable")]
        //private static void OnShipDisable()
        //{
        //    FileLog.Log("Ship OnDisable");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(MuseBase.Multiplayer.Unity.MuseWorldClient), "CleanupCurrentRegion")]
        //private static void MuseWorldClientCleanup()
        //{
        //    FileLog.Log("MuseWorldClient CleanupCurrentRegion");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIManager), "CheckCurrentMatchStatus")]
        //private static void OnMissionEnd()
        //{
        //    Mission mission = Mission.Instance;
        //    // Check if match is ending.
        //    if (!(mission != null && mission.initialized && mission.winningTeam != -1 && !mission.HadEnded))
        //    {
        //        return;
        //    }
        //    FileLog.Log("UIManager CheckCurrentMatchStatus Ended");
        //}

        // A new mission was initialized.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mission), "Awake")]
        private static void OnMatchStart()
        {
            FileLog.Log("Mission Awake");
            _MissionActive = true;
        }
        
        // Mission ended.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mission), "OnDisable")]
        private static void OnMissionDisable()
        {
            FileLog.Log("Mission OnDisable");
            // Reset state when mission is ended.
            _MissionActive = false;
            _ShipDatas.Clear();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipRegistry), "Register")]
        private static void ShipRegisteredPrefix(Ship ship, out bool __state)
        {
            // All loaded ships are registered on every tick.
            // Use state to check if a new ship is being registered.
            __state = !ShipRegistry.All.Contains(ship);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipRegistry), "Register")]
        private static void ShipRegisteredPostFix(Ship ship, bool __state)
        {
            // If __state is true ship had already been registered.
            if (!__state) return;

            FileLog.Log("Ship registered");

            ShipData shipData = new ShipData(ship);
            _ShipDatas.Add(shipData);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "AddPlayer")]
        private static void ShipPlayerAdded(Ship __instance, NetworkedPlayer player)
        {
            FileLog.Log($"Ship AddPlayer {player.UserId}");
            // Reload the players of ship data object when player is added.
            _ShipDatas.Find(shipData => shipData.ShipId == __instance.ShipId).ReloadPlayers(__instance);
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            FileLog.Log("UIMatchCompleteState Enter");
            FileLog.Log("Crew from match lobby view");
            foreach (Muse.Goi2.Entity.Vo.CrewShipVO crewShip in MatchLobbyView.Instance.CrewShips)
            {   
                FileLog.Log($"Ship: {crewShip.Name} {crewShip.Model.Name}");
                
            }
            foreach (Muse.Goi2.Entity.CrewEntity crewEntity in MatchLobbyView.Instance.FlatCrews)
            {
                foreach (var crewMember in crewEntity.CrewMembers)
                {
                    FileLog.Log($"Player: {crewMember.Name} {crewMember.Id}");
                }
            }

            Mission mission = Mission.Instance;

            MatchHistoryRecord record = new MatchHistoryRecord(mission, _ShipDatas);
            FileLog.Log(JsonConvert.SerializeObject(record));
        }
    }



    public class MatchHistoryRecord
    {
        public int GameMode;
        public int TeamSize;
        public int MapId;
        public int[] Scores;

        public int MissionTime;

        public string MatchId;

        public List<ShipData> Ships;


        //public int TimeStarted;
        //public int TimeLength;
        public long TimeEnded;

        public MatchHistoryRecord(Mission mission, List<ShipData> ships)
        {
            //if (!__instance.IsSkirmish) throw new Exception("Not a PvP match. Match history not supported.");
            int gameMode = -1;
            if (mission is Deathmatch) gameMode = 0;
            if (mission is CaptureTheFlag) gameMode = 1;
            if (mission is CrazyKing) gameMode = 2;

            int[] scores = new int[mission.numberOfTeams];
            for (int i = 0; i < mission.numberOfTeams; ++i) scores[i] = mission.TeamScore(i);

            GameMode = gameMode;
            TeamSize = mission.shipsPerTeam;
            MapId = mission.mapId;
            Scores = scores;
            Ships = new List<ShipData>(ships);
            TimeEnded = DateTime.UtcNow.Ticks;

            MatchId = MatchLobbyView.Instance.MatchId;
            MissionTime = MatchLobbyView.Instance.ElapsedTime.Seconds;
        }
        

        //public int GameMode;


    }

    public class ShipData
    {
        public int ShipId;
        public string ShipName;
        public int ShipModel;
        //public List<int> Guns;

        public int TeamIdx;
        //public int ShipIdx;
    
        public List<PlayerData> Players;

        public ShipData(Ship ship)
        {
            ShipId = ship.ShipId;
            ShipName = ship.name;
            ShipModel = ship.ShipModelId;

            TeamIdx = ship.Side;

            Players = new List<PlayerData>();
            ReloadPlayers(ship);
        }

        // Realoads the players from a provided ship
        public void ReloadPlayers(Ship ship)
        {
            Players.Clear();
            foreach (NetworkedPlayer player in ship.Players)
            {
                if (player.UserId == 0) continue; // Player is AI

                PlayerData playerData = new PlayerData(player);
                Players.Add(playerData);
            }
        }

    }

    public class PlayerData
    {
        public int PlayerId;
        public string PlayerName;

        //public int? ClanId;
        //public string? ClanName;

        public int CrewClass;
        public int[] Levels;

        public PlayerData(NetworkedPlayer player)
        {
            PlayerId = player.UserId;
            PlayerName = player.name;
            CrewClass = (int)player.PlayerType;
            Levels = new int[3] {
                player.GetLevel(Muse.Goi2.Entity.AvatarClass.Pilot),
                player.GetLevel(Muse.Goi2.Entity.AvatarClass.Gunner),
                player.GetLevel(Muse.Goi2.Entity.AvatarClass.Engineer) };
            //ClanId = player.User.ClanId,
            //ClanName = player.User.ClanTag
        }
        //public int Matches;

        //public List<int> PilotTools;
        //public List<int> GunnerTools;
        //public List<int> EngineerTools;

        //int ClanId;
    }
}
