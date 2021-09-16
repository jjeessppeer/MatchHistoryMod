using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;


using Newtonsoft.Json;

using Muse.Goi2.Entity;

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

        //private static bool _MissionActive = false;
        //private static List<ShipData> _ShipDatas = new List<ShipData>();

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

        //// A new mission was initialized.
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Mission), "Awake")]
        //private static void OnMatchStart()
        //{
        //    FileLog.Log("Mission Awake");
        //    _MissionActive = true;
        //}
        
        //// Mission ended.
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Mission), "OnDisable")]
        //private static void OnMissionDisable()
        //{
        //    FileLog.Log("Mission OnDisable");
        //    // Reset state when mission is ended.
        //    _MissionActive = false;
        //    _ShipDatas.Clear();
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(ShipRegistry), "Register")]
        //private static void ShipRegisteredPrefix(Ship ship, out bool __state)
        //{
        //    // All loaded ships are registered on every tick.
        //    // Use state to check if a new ship is being registered.
        //    __state = !ShipRegistry.All.Contains(ship);
        //}
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ShipRegistry), "Register")]
        //private static void ShipRegisteredPostFix(Ship ship, bool __state)
        //{
        //    // If __state is true ship had already been registered.
        //    if (!__state) return;

        //    FileLog.Log("Ship registered");

        //    ShipData shipData = new ShipData(ship);
        //    _ShipDatas.Add(shipData);
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Ship), "AddPlayer")]
        //private static void ShipPlayerAdded(Ship __instance, NetworkedPlayer player)
        //{
        //    FileLog.Log($"Ship AddPlayer {player.UserId}");
        //    // Reload the players of ship data object when player is added.
        //    _ShipDatas.Find(shipData => shipData.ShipId == __instance.ShipId).ReloadPlayers(__instance);
        //}

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            FileLog.Log("UIMatchCompleteState Enter");
            MatchLobbyView matchLobbyView = MatchLobbyView.Instance;
            MatchData record = new MatchData(matchLobbyView);
            FileLog.Log(JsonConvert.SerializeObject(record));
        }
    }



    public class MatchData
    {
        public string MatchId;
        public int GameMode;
        public int MapId;

        public int TeamCount;
        public int TeamSize;

        public int[] Scores;
        public int MissionTime;
        public DateTime TimeStarted;

        public List<ShipData> Ships;

        public MatchHistoryRecord(MatchLobbyView matchLobbyView)
        {
            Mission mission = Mission.Instance;

            MatchId = matchLobbyView.MatchId;
            GameMode = (int)matchLobbyView.Map.GameMode;
            MapId = matchLobbyView.MapId;
            TeamSize = mission.shipsPerTeam;
            TeamCount = mission.numberOfTeams;
            MissionTime = matchLobbyView.ElapsedTime.Seconds;
            TimeStarted = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(MissionTime));

            // Load scores from currently active mission.
            int[] Scores = new int[mission.numberOfTeams];
            for (int i = 0; i < mission.numberOfTeams; ++i) Scores[i] = mission.TeamScore(i);

            // Load all the ships.
            Ships = new List<ShipData>();
            for (int i = 0; i < matchLobbyView.CrewCount; ++i)
            {
                Muse.Goi2.Entity.Vo.CrewShipVO crewShip = matchLobbyView.CrewShips[i];
                Muse.Goi2.Entity.CrewEntity crewEntity = matchLobbyView.FlatCrews[i];
                //matchLobbyView.CrewShips

                ShipData shipData = new ShipData(crewShip, crewEntity);
                Ships.Add(shipData);
            }
        }

    }

    public class ShipData
    {
        public int ShipId;
        public string ShipName;

        public int ShipModel;
        public List<int> Guns;
        //public List<int> Guns;

        public int TeamIdx;
        public int ShipIdx;
    
        public List<PlayerData> Players;

        public ShipData(Muse.Goi2.Entity.Vo.CrewShipVO ship, CrewEntity crew)
        {
            ShipId = ship.Id;
            ShipName = ship.Name;
            ShipModel = ship.Model.Id;
            TeamIdx = ship.Side;
            ShipIdx = crew.SequenceInMatch;

            Players = new List<PlayerData>();
            foreach (UserAvatarEntity player in crew.CrewMembers)
            {
                PlayerData playerData = new PlayerData(player);
                Players.Add(playerData);
            }
           

            // Load the guns from the ship.
            // Use the guns slot names to make sure guns are added in the correct order.
            string[] gunSlotNames = new string[8]
            {
                "gun-slot-1", "gun-slot-2", "gun-slot-3", "gun-slot-4",
                "gun-slot-5", "gun-slot-6", "gun-slot-7", "gun-slot-8"
            };
            Guns = new List<int>();
            for (int i=0; i<ship.GetGuns().Count; ++i)
            {
                Guns.Add(ship.GetGuns()[gunSlotNames[i]].Id);
                //Loadout.Add($"{ship.GetGuns()[gunSlotNames[i]].Id} {ship.GetGuns()[gunSlotNames[i]].NameText}");
            }

            foreach (var item in crew.MatchStats)
            {
                FileLog.Log($"Crew stat: {item.Key} {item.Value}");
            }
        }


    }

    public class PlayerData
    {
        public int PlayerId;
        public string PlayerName;
        public string PlayerNameRaw;
        //public int? ClanId;
        public string ClanTag;

        public int CrewClass;
        public int[] Levels;
        //public int AvgLevel;
        //public int MatchesPlayed;

        //public List<int> PilotTools;
        //public List<int> GunnerTools;
        //public List<int> EngineerTools;

        public Dictionary<string, List<string>> Loadout;

        public PlayerData(UserAvatarEntity user)
        {

            
            PlayerId = user.UserId;
            PlayerName = user.Name;
            PlayerNameRaw = user.RawName;
            //ClanId = user.ClanId; // Null in this state idk why.
            ClanTag = user.ClanTag;

            Levels = new int[3] {
                user.GetLevel(AvatarClass.Pilot) + user.GetPrestigeLevel(AvatarClass.Pilot) * 45,
                user.GetLevel(AvatarClass.Gunner) + user.GetPrestigeLevel(AvatarClass.Gunner) * 45,
                user.GetLevel(AvatarClass.Engineer) + user.GetPrestigeLevel(AvatarClass.Engineer) * 45 };

            //AvgLevel = user.AverageLevel; // Always 0 in this state
            //MatchesPlayed = NetworkedPlayer.ByUserId[user.UserId].MatchesPlayed; // Always 0 in this state

            CrewClass = (int)user.CurrentClass;
            Loadout = new Dictionary<string, List<string>>();
            Loadout.Add("Pilot", new List<string>());
            Loadout.Add("Gunner", new List<string>());
            Loadout.Add("Engineer", new List<string>());
            foreach (var skill in user.CurrentSkills)
            {
                var sc = CachedRepository.Instance.Get<SkillConfig>(skill);
                if (sc.Type == SkillType.Helm) Loadout["Pilot"].Add($"{sc.Name} {skill}");
                if (sc.Type == SkillType.Gun) Loadout["Gunner"].Add($"{sc.Name} {skill}");
                if (sc.Type == SkillType.Repair) Loadout["Engineer"].Add($"{sc.Name} {skill}");
            }

            //var loadout = user.Loadouts[user.CurrentClass];
            //Console.WriteLine("Loadouts gotten");
            //var skillList = loadout.GetCurrentSkillList(GameType.Skirmish);
            //Console.WriteLine("Skill list gotten");
            //var skills = skillList.GetSkills(GameType.Skirmish);
            //Console.WriteLine("Skills gotten");
            //foreach (var playerSkill in skills)
            //{
            //    Console.WriteLine("Adding skill");
            //    if (playerSkill.Type == SkillType.Helm) { Loadout["Helm"].Add($"{playerSkill.Name} {playerSkill.AssetId} {playerSkill.ActivationId}"); }
            //}

            //user.Loadouts[user.CurrentClass].GetCurrentSkillList(Muse.Goi2.Entity.GameType.Skirmish).GetSkills(Muse.Goi2.Entity.GameType.Skirmish)[0].Type;
            //ClanId = player.User.ClanId,
            //ClanName = player.User.ClanTag
        }
    }
}
