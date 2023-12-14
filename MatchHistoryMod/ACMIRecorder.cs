using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MatchHistoryMod.ACMI
{
    class ACMIRecorder
    {
        public string Output;
        long GameStartTimestamp;
        Dictionary<string, float> ShipLastTimestamp = new Dictionary<string, float>();
        Dictionary<string, bool> ShipLastDead = new Dictionary<string, bool>();
        const float PositionSnapshotInterval = 2;

        private static float GetMapOffset(string mapName)
        {
            switch (mapName)
            {
                case "Alleron Affray":
                    return 0;
                case "Ashen Scuffle":
                    return -1;
                case "Assault on Kinforth":
                    return -2;
                case "Batcave":
                    return -3;
                case "Battle on the Dunes":
                case "Duel at Dawn":
                case "Graveyard Rumble":
                    return -4;
                case "Canyon Ambush":
                    return -5;
                case "Clash at Blackcliff":
                    return -6;
                case "Crown Gambit":
                    return -7;
                case "Derelict Deception":
                    return -8;
                case "Fight over Firnfeld":
                    return -9;
                case "Misty Mutiny":
                    return -10;
                case "Northern Fjords":
                    return -11;
                case "Oblivion South":
                    return -12;
                case "Paritan Rumble":
                    return -13;
                case "Thornholt Throwndown":
                    return -14;
                case "Water Hazard":
                    return -15;
                default:
                    return 1;
            }
        }

        public ACMIRecorder()
        {
            FileLog.Log("RECORDER STARTED");
            int mapId = MatchLobbyView.Instance.Map.Id;
            FileLog.Log($"Mapid: {mapId}");
            string mapName = MatchLobbyView.Instance.Map.Name;
            FileLog.Log($"Mapname: {mapName}");
            float mapLongOffset = GetMapOffset(mapName) + 0.5f;
            FileLog.Log($"offset: {mapLongOffset}");

            var date = DateTime.Now.ToUniversalTime();
            string dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}";
            FileLog.Log($"date: {dateStr}");

            string header = "FileType=text/acmi/tacview\nFileVersion=2.2";
            string config = $"0,ReferenceTime={dateStr}T00:00:00Z,ReferenceLongitude={mapLongOffset},ReferenceLatitude=0.5";
            Output = $"{header}\n{config}";
            //Output = "FileType=text/acmi/tacview\nFileVersion=2.2\n0,ReferenceTime=2000-01-01T00:00:00Z,ReferenceLongitude=0,ReferenceLatitude=0";
            WriteEvent(0, $"1,T=0|0|0,Name=goio-enviro-{mapId},Color=Orange");
        }
        

        public string GetShipACMIId(Ship ship)
        {
            return "";
        }
        public string GetProjectileACMIId()
        {
            return "";
        }

        public void WriteEvent(float time, string eventString)
        {
            //FileLog.Log($"\n#{time}\n{eventString}");
            Output += $"\n#{time}\n{eventString}";
        }

        public void StartTimer()
        {
            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            GameStartTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public string vectorToTransformString(Vector3 vector, Vector3? heading = null)
        {
            const double earthCircumference = 6378137 * 2 * Math.PI; //40030173;
            const double mToDeg = 360 / earthCircumference;
            // Approximation for equator.
            double longitude = vector.x * mToDeg;
            double latitude = vector.z * mToDeg;
            double altitude = vector.y;
            string t = $"{longitude}|{latitude}|{altitude}";

            if (heading.HasValue)
            {
                double roll = 0;
                //double pitch = Math.Asin(heading.Value.y) * 360 / (2 * Math.PI);
                double pitch = 0;
                double yaw = -Math.Atan2(heading.Value.z, heading.Value.x) * 360 / (2 * Math.PI) + 90;
                t = $"{t}|{roll}|{pitch}|{yaw}";
            }
            return t;
        }

        public float GetTimestampSeconds()
        {
            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
            timestamp -= GameStartTimestamp;
            return (float)timestamp/1000.0f;
        }


        //private (int diameter, double circumference, double area) GetCircle(int radius)
        //{
        //    var diameter = radius * 2;
        //    var circumference = Math.PI * diameter;
        //    var area = Math.PI * Math.Pow(radius, 2);
        //    return (diameter, circumference, area);
        //}

        //private void GetGunNumbers(Turret turret, 
        //    out float muzzleSpeed, 
        //    out float projectileLifetime,
        //    out float bulletGravity)
        //{
        //    muzzleSpeed = 0;
        //    projectileLifetime = 0;
        //}

        private int projectileCount = 0;
        public void TurretFired(Turret turret)
        {   

            Ship ship = turret.Ship;
            string id = $"1001{ship.Side:D2}{ship.CrewIndex:D2}{projectileCount++:D6}";

            NetworkedPlayer user = turret.UsingPlayer;
            string shooterName;
            if (user != null) // Player or AI
            {
                shooterName = user.name;
            }
            else shooterName = "[Unknown]";
            //turret.AmmoEquipmentID;
            //.           

            float time = GetTimestampSeconds();
            string serialized = $"{id},T={vectorToTransformString(turret.position)},Name=goio-gun-{turret.ItemId},Pilot={shooterName}";
            WriteEvent(time, serialized);
            //turret.Data.

            // Pitch and Yaw to directional vector
            double xzLen = Math.Cos(turret.WorldPitch * 0.0174533);
            Vector3 shotDirection = new Vector3(
                (float)(xzLen * -Math.Sin(-turret.WorldYaw * 0.0174533)),
                (float)-Math.Sin(turret.WorldPitch * 0.0174533),
                (float)(xzLen * Math.Cos(turret.WorldYaw * 0.0174533))
            ).normalized;
            float MuzzleVelocity = turret.Data.muzzleSpeed;
            float deltaT = 2.0f;
            Vector3 ExpirationPosition = turret.position + shotDirection * MuzzleVelocity * deltaT;
            string serializedExpiration = $"{id},T={vectorToTransformString(ExpirationPosition)}";
            WriteEvent(time + deltaT, serializedExpiration);
            WriteEvent(time + deltaT, $"-{id}");
        }

        public void AddShipPosition(Ship ship)
        {
            //string id = ship.CrewId;
            //int id = ship.Side + ship.CrewIndex * 1000;
            string id = $"1000{ship.Side:D2}{ship.CrewIndex:D2}";
            float time = GetTimestampSeconds();

            // Only add data if time has passed or ship died.
            if (!ShipLastTimestamp.ContainsKey(id))
            {
                ShipLastTimestamp.Add(id, time);
                ShipLastDead.Add(id, ship.IsDead);
            }
            else if (time - ShipLastTimestamp[id] < PositionSnapshotInterval && ShipLastDead[id] == ship.IsDead)
            {
                return;
            }

            ShipLastTimestamp[id] = time;
            ShipLastDead[id] = ship.IsDead;

            string transform = vectorToTransformString(ship.position, ship.Forward);
            string serialized = $"{id},T={transform},Name=goio-ship-{ship.ShipModelId},CallSign={ship.name}";
            WriteEvent(time, serialized);
        }
    }
}
