using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx;
using HarmonyLib;
using UnityEngine;


using Newtonsoft.Json;

using Muse.Goi2.Entity;

using System.IO;
using System.Net;

namespace MatchHistoryMod
{
    [HarmonyPatch]
    public static class MatchHistoryRecorder
    {

        const string uploadServerURL = "http://127.0.0.1";


        static MatchHistoryRecorder()
        {
        }

        public static void SaveMatchData(MatchData matchData)
        {
            //FileLog.Log(JsonConvert.SerializeObject(matchData));
            // Append serialized match data to match history file.
            // TODO: limit size of file?
            using (StreamWriter w = File.AppendText("MatchHistory.log"))
            {
                w.WriteLine(JsonConvert.SerializeObject(matchData));
            }
        }

        public static void UploadMatchData(MatchData matchData)
        {
            return;
            var request = (HttpWebRequest)WebRequest.Create($"{uploadServerURL}/submit_history");
            var postData = JsonConvert.SerializeObject(matchData);
            FileLog.Log($"{postData}");
            var data = Encoding.ASCII.GetBytes(postData);
            request.Method = "POST";
            request.Timeout = 1000;
            request.ContentType = "application/json";
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                //FileLog.Log($"Response: {responseString}");
            }
            catch (System.Net.WebException e)
            {
                //Console.WriteLine("Error submitting match history.");
                //FileLog.Log(e.Message);
            }
        }

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            //FileLog.Log("UIMatchCompleteState Enter");
            MatchData record = new MatchData(MatchLobbyView.Instance, Mission.Instance);
            SaveMatchData(record);
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
        public MatchData() { }
        public MatchData(MatchLobbyView matchLobbyView, Mission mission)
        {

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
                CrewEntity crewEntity = matchLobbyView.FlatCrews[i];
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
            for (int i = 0; i < ship.GetGuns().Count; ++i)
            {
                Guns.Add(ship.GetGuns()[gunSlotNames[i]].Id);
                //Loadout.Add($"{ship.GetGuns()[gunSlotNames[i]].Id} {ship.GetGuns()[gunSlotNames[i]].NameText}");
            }

            //foreach (var item in crew.MatchStats)
            //{
            //    FileLog.Log($"Crew stat: {item.Key} {item.Value}");
            //}
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
        }
    }

}
