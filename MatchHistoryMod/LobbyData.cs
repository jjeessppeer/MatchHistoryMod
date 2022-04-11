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
        public string ModVersion;
        public string MatchId;
        public bool Passworded;

        public int MapId;
        public string MapName;
        public int GameMode;
        public int TeamSize;
        public int TeamCount;

        public int Winner;
        public int MatchTime;
        public int Status; // Lobby, Loading, Running, Ended
        public List<int> Scores = new List<int>();

        public List<ShipData> Ships = new List<ShipData>();


        public LobbyData(MatchLobbyView mlv, Mission mission)
        {
            ModVersion = MatchHistoryMod.pluginVersion;
            MatchId = mlv.MatchId;
            Passworded = mlv.HasPassword;

            MapId = mlv.Map.Id;
            MapName = mlv.Map.NameText.En;
            GameMode = (int)mlv.Map.GameMode;
            TeamSize = mission.shipsPerTeam;
            TeamCount = mission.numberOfTeams;
            //Ships = new ShipData[TeamSize * TeamCount];
            Status = 0;
            if (mlv.Loading) Status = 1;
            if (mlv.Running) Status = 2;
            if (mission.winningTeam != -1) Status = 3;
            Winner = mission.winningTeam;

            for (int i = 0; i < TeamCount; ++i) Scores.Add(mission.TeamScore(i));

            //EndReason = (int?)matchEntity.EndReason;
            //StartDate = matchEntity.StartDate.Ticks;
            //if (matchEntity.EndDate.HasValue) EndDate = matchEntity.EndDate.Value.Ticks;

            // Load all the ships and crews
            for (int i = 0; i < mlv.CrewShips.Count; ++i)
            {
                //if (mlv.CrewShips[i] == null || mlv.Crews[i] == null) ;
                if (mlv.CrewShips[i] == null)
                {
                    continue;
                }
                if (mlv.FlatCrews[i] == null)
                {
                    continue;
                }
                Ships.Add(new ShipData(mlv.CrewShips[i], mlv.FlatCrews[i]));
                //Ships[i] = new ShipData(mlv.CrewShips[i], mlv.FlatCrews[i]);
            }
        }
    }

    public class ShipData
    {
        public int ShipModel;
        public string ShipName;
        public int Team;
        public List<int> ShipLoadout = new List<int>();
        public List<string> SlotNames = new List<string>();

        public PlayerData[] Players = new PlayerData[4];

        public ShipData(Muse.Goi2.Entity.Vo.CrewShipVO ship, CrewEntity crew)
        {
            ShipModel = ship.ModelId;
            ShipName = ship.Name;
            Team = ship.Side;
            
            // Load ship loadout
            //crewship.Equipments

            for (int i=0; i<ship.Equipments.Count; ++i)
            {
                ShipLoadout.Add(ship.Equipments[i].SlotItemId);
                SlotNames.Add(ship.Equipments[i].SlotName);
            }

            // Load player data.
            //crewship.pl

         
            for (int i = 0; i < 4; ++i)
            {
                if (crew.Slots[i] == null)
                {
                    continue;
                }
                if (crew.Slots[i].PlayerEntity == null)
                {
                    continue;
                }
                Players[i] = new PlayerData(crew.Slots[i]);
            }
        }
    }


    public class PlayerData
    {
        public int UserId;
        public string Name;
        public string Clan;

        public int Class;
        public int Level;

        public int MatchCount;
        public int MatchCountRecent;

        public List<int> Skills;

        public PlayerData(CrewSlotData playerData)
        {
            UserAvatarEntity playerEntity = playerData.PlayerEntity;
            
            Level = playerEntity.CurrentPrestigeRank;
            UserId = playerEntity.Id;
            Name = playerEntity.Name;
            Clan = playerEntity.ClanTag;
            Class = (int)playerEntity.CurrentClass;

            //playerEntity.Loadouts[playerEntity.CurrentClass].Equipments;
            Skills = playerEntity.CurrentSkills;
            MatchCountRecent = playerEntity.MatchCountInThirtyDays;

        }
    }
}
