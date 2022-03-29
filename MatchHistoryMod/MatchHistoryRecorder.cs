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
using System.Collections;
using LitJson;

using System.Net;
using System.Text;
using System.IO;

namespace MatchHistoryMod
{
    [HarmonyPatch]
    public static class MatchHistoryRecorder
    {

        private const string _UploadURL = "http://127.0.0.1:80";


        static MatchHistoryRecorder()
        {
        }


        public static void SaveMatchDataLocal(LobbyData record)
        {

        }

        public static void UploadMatchData(LobbyData record)
        {
            string recordJSON = JsonConvert.SerializeObject(record);



            var httpWebRequest = (HttpWebRequest)WebRequest.Create(_UploadURL);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(recordJSON);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }


        }
        //public static void SaveMatchData(MatchData matchData)
        //{
        //    //FileLog.Log(JsonConvert.SerializeObject(matchData));
        //    // Append serialized match data to match history file.
        //    // TODO: limit size of file?
        //    using (StreamWriter w = File.AppendText("MatchHistory.log"))
        //    {
        //        w.WriteLine(JsonConvert.SerializeObject(matchData));
        //    }
        //}

        //public static void UploadMatchData(MatchData matchData)
        //{
            
        //}

        // Called when match ends and post game screen is shown.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            FileLog.Log("NEW LOBBY DATA");
            //var v = MatchLobbyView.Instance.GetPlayerCrew(NetworkedPlayer.Local.UserId).Match;
            FileLog.Log("Starting Lobby data load");
            LobbyData d = new LobbyData(MatchLobbyView.Instance, Mission.Instance);
            FileLog.Log($"Lobby data loaded successfully");
            FileLog.Log($"\n{JsonConvert.SerializeObject(d)}\n");


            // Use to get time of match and some other stats
            //FileLog.Log("Requesting match stats pre");
            //MatchActions.GetMatchStats(new Muse.Networking.ExtensionResponseDelegate(GetMatchStats));
            //FileLog.Log("Requesting match stats after");
        }


        public static void GetMatchStats(Muse.Networking.ExtensionResponse resp)
        {
            JsonData jsonData = resp.JsonData;
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            IDictionary dictionary2 = jsonData["stats"];
            if (dictionary2 != null)
            {
                IDictionaryEnumerator enumerator = dictionary2.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        object obj = enumerator.Current;
                        DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
                        dictionary[dictionaryEntry.Key.ToString()] = (int)((JsonData)dictionaryEntry.Value);
                    }
                }
                finally
                {
                    IDisposable disposable;
                    if ((disposable = (enumerator as IDisposable)) != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            else
            {
                MuseLog.InfoFormat("no statsData", new object[0]);
            }
            Dictionary<int, float> dictionary3 = new Dictionary<int, float>();
            IDictionary dictionary4 = jsonData["quests"];
            if (dictionary4 != null)
            {
                IDictionaryEnumerator enumerator2 = dictionary4.GetEnumerator();
                try
                {
                    while (enumerator2.MoveNext())
                    {
                        object obj2 = enumerator2.Current;
                        DictionaryEntry dictionaryEntry2 = (DictionaryEntry)obj2;
                        int num = -1;
                        if (int.TryParse(dictionaryEntry2.Key.ToString(), out num) && num != -1)
                        {
                            dictionary3[num] = (float)((JsonData)dictionaryEntry2.Value);
                        }
                    }
                }
                finally
                {
                    IDisposable disposable2;
                    if ((disposable2 = (enumerator2 as IDisposable)) != null)
                    {
                        disposable2.Dispose();
                    }
                }
            }
            else
            {
                MuseLog.InfoFormat("no quests data", new object[0]);
            }
            Dictionary<string, int> dictionary5 = new Dictionary<string, int>();
            IDictionary dictionary6 = jsonData["crewStats"];
            if (dictionary6 != null)
            {
                IDictionaryEnumerator enumerator3 = dictionary6.GetEnumerator();
                try
                {
                    while (enumerator3.MoveNext())
                    {
                        object obj3 = enumerator3.Current;
                        DictionaryEntry dictionaryEntry3 = (DictionaryEntry)obj3;
                        string key = dictionaryEntry3.Key.ToString();
                        dictionary5[key] = (int)((JsonData)dictionaryEntry3.Value);
                    }
                }
                finally
                {
                    IDisposable disposable3;
                    if ((disposable3 = (enumerator3 as IDisposable)) != null)
                    {
                        disposable3.Dispose();
                    }
                }
                UIMatchEndCrewPanel.instance.SetStats(dictionary5);
            }
            else
            {
                MuseLog.InfoFormat("no crewStats data", new object[0]);
            }

            FileLog.Log($"STAT DICTIONARY");
            foreach (KeyValuePair<string, int> entry in dictionary)
            {
                FileLog.Log($"{entry.Key}: {entry.Value}");
            }

            FileLog.Log($"CREW DICTIONARY");
            foreach (KeyValuePair<string, int> entry in dictionary5)
            {
                FileLog.Log($"{entry.Key}: {entry.Value}");
            }
            FileLog.Log($"goodbye");
        }

        public static void LogStats(IDictionary<string, int> statsData)
        {
            FileLog.Log("__MATCH END STATS__");
            for (int i=0; i<statsData.Count; ++i)
            {
                FileLog.Log($"{statsData.ElementAt(i).Key}: {statsData.ElementAt(i).Value}\n");
            }
        }
    }

    //public class MatchData
    //{
    //    public string MatchId;
    //    public int GameMode;
    //    public int MapId;

    //    public int TeamCount;
    //    public int TeamSize;

    //    public int[] Scores;
    //    public int MissionTime;
    //    public DateTime TimeStarted;

    //    public List<ShipData> Ships;
    //    public MatchData() { }
    //    public MatchData(MatchLobbyView matchLobbyView, Mission mission)
    //    {

    //        MatchId = matchLobbyView.MatchId;
    //        GameMode = (int)matchLobbyView.Map.GameMode;
    //        MapId = matchLobbyView.MapId;
    //        TeamSize = mission.shipsPerTeam;
    //        TeamCount = mission.numberOfTeams;
    //        MissionTime = matchLobbyView.ElapsedTime.Seconds;
    //        TimeStarted = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(MissionTime));

    //        // Load scores from currently active mission.
    //        int[] Scores = new int[mission.numberOfTeams];
    //        for (int i = 0; i < mission.numberOfTeams; ++i) Scores[i] = mission.TeamScore(i);

    //        // Load all the ships.
    //        Ships = new List<ShipData>();
    //        for (int i = 0; i < matchLobbyView.CrewCount; ++i)
    //        {
    //            Muse.Goi2.Entity.Vo.CrewShipVO crewShip = matchLobbyView.CrewShips[i];
    //            CrewEntity crewEntity = matchLobbyView.FlatCrews[i];
    //            //matchLobbyView.CrewShips

    //            ShipData shipData = new ShipData(crewShip, crewEntity);
    //            Ships.Add(shipData);
    //        }
    //    }
    //}

    //public class ShipData
    //{
    //    public int ShipId;
    //    public string ShipName;

    //    public int ShipModel;
    //    public List<int> Guns;

    //    public int TeamIdx;
    //    public int ShipIdx;

    //    public List<PlayerData> Players;

    //    public ShipData(Muse.Goi2.Entity.Vo.CrewShipVO ship, CrewEntity crew)
    //    {
    //        ShipId = ship.Id;
    //        ShipName = ship.Name;
    //        ShipModel = ship.Model.Id;
    //        TeamIdx = ship.Side;
    //        ShipIdx = crew.SequenceInMatch;

    //        Players = new List<PlayerData>();
    //        foreach (UserAvatarEntity player in crew.CrewMembers)
    //        {
    //            PlayerData playerData = new PlayerData(player);
    //            Players.Add(playerData);
    //        }

    //        // Load the guns from the ship.
    //        // Use the guns slot names to make sure guns are added in the correct order.
    //        string[] gunSlotNames = new string[8]
    //        {
    //            "gun-slot-1", "gun-slot-2", "gun-slot-3", "gun-slot-4",
    //            "gun-slot-5", "gun-slot-6", "gun-slot-7", "gun-slot-8"
    //        };
    //        Guns = new List<int>();
    //        for (int i = 0; i < ship.GetGuns().Count; ++i)
    //        {
    //            Guns.Add(ship.GetGuns()[gunSlotNames[i]].Id);
    //            //Loadout.Add($"{ship.GetGuns()[gunSlotNames[i]].Id} {ship.GetGuns()[gunSlotNames[i]].NameText}");
    //        }

    //        //foreach (var item in crew.MatchStats)
    //        //{
    //        //    FileLog.Log($"Crew stat: {item.Key} {item.Value}");
    //        //}
    //    }
    //}

    //public class PlayerData
    //{
    //    public int PlayerId;
    //    public string PlayerName;
    //    public string PlayerNameRaw;
    //    //public int? ClanId;
    //    public string ClanTag;

    //    public int CrewClass;
    //    public int[] Levels;
    //    //public int AvgLevel;
    //    //public int MatchesPlayed;

    //    public Dictionary<string, List<string>> Loadout;

    //    public PlayerData(UserAvatarEntity user)
    //    {
    //        PlayerId = user.UserId;
    //        PlayerName = user.Name;
    //        PlayerNameRaw = user.RawName;
    //        //ClanId = user.ClanId; // Null in this state idk why.
    //        ClanTag = user.ClanTag;

    //        Levels = new int[3] {
    //            user.GetLevel(AvatarClass.Pilot) + user.GetPrestigeLevel(AvatarClass.Pilot) * 45,
    //            user.GetLevel(AvatarClass.Gunner) + user.GetPrestigeLevel(AvatarClass.Gunner) * 45,
    //            user.GetLevel(AvatarClass.Engineer) + user.GetPrestigeLevel(AvatarClass.Engineer) * 45 };

    //        //AvgLevel = user.AverageLevel; // Always 0 in this state
    //        //MatchesPlayed = NetworkedPlayer.ByUserId[user.UserId].MatchesPlayed; // Always 0 in this state

    //        CrewClass = (int)user.CurrentClass;
    //        Loadout = new Dictionary<string, List<string>>();
    //        Loadout.Add("Pilot", new List<string>());
    //        Loadout.Add("Gunner", new List<string>());
    //        Loadout.Add("Engineer", new List<string>());
    //        foreach (var skill in user.CurrentSkills)
    //        {
    //            var sc = CachedRepository.Instance.Get<SkillConfig>(skill);
    //            if (sc.Type == SkillType.Helm) Loadout["Pilot"].Add($"{sc.Name} {skill}");
    //            if (sc.Type == SkillType.Gun) Loadout["Gunner"].Add($"{sc.Name} {skill}");
    //            if (sc.Type == SkillType.Repair) Loadout["Engineer"].Add($"{sc.Name} {skill}");
    //        }
    //    }
    //}

}
