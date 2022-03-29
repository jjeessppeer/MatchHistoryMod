using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using BepInEx;
using HarmonyLib;
using UnityEngine;

using Muse.Goi2.Entity;
using Newtonsoft.Json;

using System.IO;
using System.Net;


namespace MatchHistoryMod
{
    public class LobbyData
    {
        public string MatchId;

        public int MapId;
        public string MapName;
        public int MaxPlayers;
        public int CurrentPlayers;
        public int CurrentPilots;

        public CrewData[] Crews;
        //public PlayerData[] Spectators = new PlayerData[4];
        //public PlayerData[] AllPlayers = new PlayerData[20]; // 16 players + 4 spectators.

        public int[] Scores = new int[2];

        public int Status; // Lobby, Loading, Running, Ended
        public int? EndReason;
        public long StartDate;
        public long? EndDate = null;


        public LobbyData(MatchEntity matchEntity)
        {
            FileLog.Log($"Loading lobby data {matchEntity.ToString()}");
            MatchId = matchEntity.MatchId;
            MapName = matchEntity.Map.NameText.En;
            MapId = matchEntity.Map.Id;
            MaxPlayers = matchEntity.MaxPlayerCount;
            CurrentPlayers = matchEntity.PlayerCount;
            CurrentPilots = matchEntity.CaptainCount;

            if (matchEntity.Running) Status = 2;
            if (matchEntity.Loading) Status = 1;
            if (matchEntity.EndReason.HasValue) Status = 3;

            EndReason = (int?)matchEntity.EndReason;
            StartDate = matchEntity.StartDate.Ticks;
            if (matchEntity.EndDate.HasValue) EndDate = matchEntity.EndDate.Value.Ticks;

            // Load all the crews

            FileLog.Log($"Crews: {matchEntity.Crews.Count}");
            Crews = new CrewData[matchEntity.Crews.Count];
            for (int i = 0; i < matchEntity.Crews.Count; ++i)
            {
                Crews[i] = new CrewData(matchEntity.Crews[i]);
            }
        }
    }

    public class CrewData
    {
        public ShipData Ship;
        public PlayerData[] Players = new PlayerData[4];
        public CrewData(CrewEntity crewEntity)
        {
            FileLog.Log($"Loading crew data {crewEntity.ToString()}");
            Ship = new ShipData(crewEntity.Ship);
            for (int i = 0; i < 4; ++i)
            {
                if (crewEntity.Slots[i] == null)
                {
                    FileLog.Log($"Null crew slot {i}");
                    continue;
                }
                if (crewEntity.Slots[i].PlayerEntity == null)
                {
                    FileLog.Log($"Null player {i}");
                    continue;
                }
                Players[i] = new PlayerData(crewEntity.Slots[i].PlayerEntity);
            }
        }
    }

    public class ShipData
    {
        public int ShipType;
        public int[] Loadout = new int[6];

        public string ShipName;

        public ShipData(ShipEntity shipEntity)
        {
            FileLog.Log($"Loading ship data {shipEntity.ToString()}");
            ShipType = 0;
            ShipName = shipEntity.Name;
        }
    }

    public class PlayerData
    {
        public string Name;
        public string Clan;
        public int Matches;
        public int Level;
        public int Role;

        public PlayerData(UserAvatarEntity playerEntity)
        {
            FileLog.Log($"Loading player data {playerEntity.ToString()}");
            Name = playerEntity.RawName;
            Clan = playerEntity.ClanTag;
        }
    }
}
