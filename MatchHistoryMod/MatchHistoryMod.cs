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
        static MatchHistoryRecorder()
        {
            FileLog.Log("Match recorder hello");

            //// Intitialize event callback dictionary.
            //// Each event type gets a list where callback functions can be added.
            //_eventCallbacks = new Dictionary<string, List<Action<Event>>>();
            //foreach (string eventStr in _validEvents)
            //{
            //    _eventCallbacks.Add(eventStr, new List<Action<Event>>());
            //}
            //_activeMission = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mission), "OnDisable")]
        private static void OnMissionEnd(Mission __instance)
        {
            FileLog.Log("MATCH ENDED");
            //if (__instance.numberOfTeams * __instance.shipsPerTeam != ShipRegistry.AllPlayer.Count)
            //{
            //    throw new Exception("Number of loaded ships does not match expected number of ships from mission info.");
            //}

            // Load ship and player data from the ended match.
            List<ShipData> shipDatas = new List<ShipData>();
            foreach (Ship ship in ShipRegistry.AllPlayer)
            {
                if (ship == null)
                {
                    FileLog.Log("Skipping null ship");
                    continue;
                }

                FileLog.Log("Adding ship.");

                List<PlayerData> playerDatas = new List<PlayerData>();
                foreach (NetworkedPlayer player in ship.Players)
                {
                    PlayerData playerData = new PlayerData()
                    {
                        PlayerId = player.UserId,
                        PlayerName = player.name,
                        CrewClass = (int)player.PlayerType,
                        Levels = new int[3] {
                            player.GetLevel(Muse.Goi2.Entity.AvatarClass.Pilot),
                            player.GetLevel(Muse.Goi2.Entity.AvatarClass.Gunner),
                            player.GetLevel(Muse.Goi2.Entity.AvatarClass.Engineer) }
                    };
                    //ClanId = player.User.ClanId,
                    //ClanName = player.User.ClanTag
                    playerDatas.Add(playerData);
                }

                //ship.Guns[0].typ

                ShipData shipData = new ShipData() { 
                    ShipName = ship.name,
                    ShipModel = ship.ShipModelId,
                    Players = playerDatas,
                    TeamIdx = ship.Side
                };
                shipDatas.Add(shipData);
            }


            // Load match data.
            //if (!__instance.IsSkirmish) throw new Exception("Not a PvP match. Match history not supported.");
            int gameMode = -1;
            if (__instance is Deathmatch) gameMode = 0;
            if (__instance is CaptureTheFlag) gameMode = 1;
            if (__instance is CrazyKing) gameMode = 2;



            int[] scores = new int[__instance.numberOfTeams];
            for (int i = 0; i < __instance.numberOfTeams; ++i) scores[i] = __instance.TeamScore(i);

            MatchHistoryRecord record = new MatchHistoryRecord()
            {
                GameMode = gameMode,
                TeamSize = __instance.shipsPerTeam,
                MapId = __instance.mapId,
                Scores = scores,
                Ships = shipDatas,
                TimeEnded = DateTime.UtcNow.Ticks
            };

            FileLog.Log(JsonConvert.SerializeObject(record));




        }


    }



    public class MatchHistoryRecord
    {
        public int GameMode;
        public int TeamSize;
        public int MapId;
        public int[] Scores;

        public List<ShipData> Ships;


        //public int TimeStarted;
        //public int TimeLength;
        public long TimeEnded;
        

        //public int GameMode;


    }

    public class ShipData
    {
        public string ShipName;
        public int ShipModel;
        //public List<int> Guns;

        public int TeamIdx;
        //public int ShipIdx;
    
        public List<PlayerData> Players;

    }

    public class PlayerData
    {
        public int PlayerId;
        public string PlayerName;

        //public int? ClanId;
        //public string? ClanName;

        public int CrewClass;
        public int[] Levels;
        //public int Matches;

        //public List<int> PilotTools;
        //public List<int> GunnerTools;
        //public List<int> EngineerTools;

        //int ClanId;
    }
}
