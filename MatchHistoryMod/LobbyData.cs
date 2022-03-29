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
        public int TeamSize;
        public int TeamCount;

        //public int CurrentPlayers;
        //public int CurrentPilots;

        public ShipData[] Ships;
        //public PlayerData[] Spectators = new PlayerData[4];
        //public PlayerData[] AllPlayers = new PlayerData[20]; // 16 players + 4 spectators.

        public int[] Scores = new int[2];

        public int Status; // Lobby, Loading, Running, Ended
        //public int? EndReason;
        //public long StartDate;
        public long? EndDate = null;


        public LobbyData(MatchLobbyView mlv, Mission mission)
        {
            FileLog.Log($"Loading lobby data");
            MatchId = mlv.MatchId;
            MapName = mlv.Map.NameText.En;
            MapId = mlv.Map.Id;
            TeamSize = mission.shipsPerTeam;
            TeamCount = mission.numberOfTeams;
            Ships = new ShipData[TeamSize * TeamCount];

            //CurrentPlayers = matchEntity.PlayerCount;
            //CurrentPilots = matchEntity.CaptainCount;

            Status = 0;
            if (mlv.Loading) Status = 1;
            if (mlv.Running) Status = 2;
            if (mission.winningTeam != -1) Status = 3;



            //EndReason = (int?)matchEntity.EndReason;
            //StartDate = matchEntity.StartDate.Ticks;
            //if (matchEntity.EndDate.HasValue) EndDate = matchEntity.EndDate.Value.Ticks;

            // Load all the crews

            FileLog.Log($"Crews: {mlv.CrewShips.Count}");
            //Crews = new ShipData[mlv.CrewShips.Count];
            for (int i = 0; i < mlv.CrewShips.Count; ++i)
            {
                FileLog.Log($"Trying to load ship {i}");

                //if (mlv.CrewShips[i] == null || mlv.Crews[i] == null) ;
                if (mlv.CrewShips[i] == null)
                {
                    FileLog.Log($"Null ship {i}");
                    continue;
                }
                if (mlv.FlatCrews[i] == null)
                {
                    FileLog.Log($"Null crew {i}");
                    continue;
                }

                FileLog.Log($"Loading ship");
                Ships[i] = new ShipData(mlv.CrewShips[i], mlv.FlatCrews[i]);
                FileLog.Log($"Ship loaded");
            }
            FileLog.Log($"All ships loaded");
        }
    }

    public class ShipData
    {
        public int ShipModel;
        public int[] ShipLoadout = new int[6];
        public string ShipName;

        public PlayerData[] Players = new PlayerData[4];

        public ShipData(Muse.Goi2.Entity.Vo.CrewShipVO ship, CrewEntity crew)
        {
            FileLog.Log($"Loading ship data {ship.ToString()}");

            ShipModel = ship.ModelId;
            ShipName = ship.Name;
            
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
                    FileLog.Log($"Null crew slot {i}");
                    continue;
                }
                if (crew.Slots[i].PlayerEntity == null)
                {
                    FileLog.Log($"Null player {i}");
                    continue;
                }
                Players[i] = new PlayerData(crew.Slots[i]);
            }
            FileLog.Log($"Ship complete");
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
        //public int[] PilotEquipment = new int[3];
        //public int[] GunnerEquipment = new int[3];
        //public int[] EngiEquipment = new int[3];

        public int MatchCount;
        public int MatchCountRecent;

        public PlayerData(CrewSlotData playerData)
        {
            UserAvatarEntity playerEntity = playerData.PlayerEntity;
            FileLog.Log($"Loading player data {playerData.ToString()}");
            
            Level = playerEntity.CurrentPrestigeRank;
            UserId = playerEntity.Id;
            Name = playerEntity.Name;
            Clan = playerEntity.ClanTag;
            Class = (int)playerEntity.CurrentClass;

            //playerEntity.Loadouts[playerEntity.CurrentClass].Equipments;
            Skills = playerEntity.CurrentSkills;
            MatchCountRecent = playerEntity.MatchCountInThirtyDays;
            //playerEntity.count

        }
    }
}
