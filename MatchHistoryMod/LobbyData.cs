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
        public int GameMode;
        public string MapName;
        public int TeamSize;
        public int TeamCount;

        public ShipData[] Ships;

        public int[] Scores = new int[2];
        public int Winner;
        public int MatchTime;

        public int Status; // Lobby, Loading, Running, Ended


        public LobbyData(MatchLobbyView mlv, Mission mission)
        {
            MatchId = mlv.MatchId;
            MapName = mlv.Map.NameText.En;
            GameMode = (int)mlv.Map.GameMode;

            MapId = mlv.Map.Id;
            TeamSize = mission.shipsPerTeam;
            TeamCount = mission.numberOfTeams;
            Ships = new ShipData[TeamSize * TeamCount];
            Status = 0;
            if (mlv.Loading) Status = 1;
            if (mlv.Running) Status = 2;
            if (mission.winningTeam != -1) Status = 3;
            Winner = mission.winningTeam;
            for (int i = 0; i < TeamCount; ++i)
            {
                Scores[i] = mission.TeamScore(i);
            }

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
                Ships[i] = new ShipData(mlv.CrewShips[i], mlv.FlatCrews[i]);
            }
        }
    }

    public class ShipData
    {
        public int ShipModel;
        public int[] ShipLoadout = new int[6];
        public string ShipName;
        public int Team;

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
               ShipLoadout[i] = ship.Equipments[i].SlotItemId;
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

        public List<int> Skills;

        public int MatchCount;
        public int MatchCountRecent;

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
