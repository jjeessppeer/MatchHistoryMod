using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using Muse.Common;
using UnityEngine;
using Muse.Networking;
using LitJson;
using Newtonsoft.Json;
using Muse.Goi2.Entity;

namespace MatchHistoryMod
{
    public class VectorJsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, Vector3 value, Newtonsoft.Json.JsonSerializer serializer)
        {
            //writer.WriteValue(value.ToString());
            writer.WriteValue($"[{value.x},{value.y},{value.z}]");
        }
        public override Vector3 ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {

            throw new NotImplementedException();
        }
    }

    public class ShotData
    {
        //public int ShotIndex;
        public long ShotTimestamp;

        public int Buckshots = 1;

        public int TeamIndex;
        public int ShipIndex;
        public int ShipId;
        public int ShooterUserId;

        public int GunSlot;
        public int GunItemId;
        public int AmmoItemId;

        public Vector3 GunPosition;
        public Vector3 GunDirection;
        public Vector3 ShipVelocity;
        public int MuzzleVelocity;

        // Target position predicted on projectile shot, updated if hit.
        public int TargetShipId = -1;
        public Vector3 TargetPosition;
        public int TargetDistance;

        public bool DidHit = false;
        public List<int> HitIndexes = new List<int>();

        public ShotData()
        {

        }

        private static Vector3 RoundVector3(Vector3 vec, int decimals = 3)
        {
            return new Vector3(
                (float)Math.Round(vec.x, decimals),
                (float)Math.Round(vec.y, decimals),
                (float)Math.Round(vec.z, decimals));
        }

        public ShotData(Turret turret, int shotIndex)
        {
            ShotTimestamp = MatchDataRecorder.GetActiveGameTimestamp();
            ShipId = turret.Ship.ShipId;
            ShipIndex = turret.Ship.CrewIndex;
            TeamIndex = turret.Ship.Side;

            // Get the userId.
            NetworkedPlayer user = turret.UsingPlayer;
            ShooterUserId = -2; // Default no user
            if (user != null) // Player or AI
            {
                ShooterUserId = user.UserId;
            }
            if (ShooterUserId == 0) ShooterUserId = -1; // AI user

            // Get buckshots.
            GunItem gunItem = CachedRepository.Instance.Get<GunItem>(turret.ItemId);
            if (gunItem.Params.ContainsKey("iRaysPerShot"))
            {
                Buckshots = int.Parse(gunItem.Params["iRaysPerShot"]);
            }

            // The slotnames are 1 indexed.
            char slotName = turret.SlotName[turret.SlotName.Length - 1];
            GunSlot = slotName - '0';

            // Pitch and Yaw to directional vector
            double xzLen = Math.Cos(turret.WorldPitch * 0.0174533);
            Vector3 shotDirection = new Vector3(
                (float)(xzLen * -Math.Sin(-turret.WorldYaw * 0.0174533)),
                (float)-Math.Sin(turret.WorldPitch * 0.0174533),
                (float)(xzLen * Math.Cos(turret.WorldYaw * 0.0174533))
            );

            GunPosition = RoundVector3(turret.position, 0);
            GunDirection = RoundVector3(shotDirection, 2);
            ShipVelocity = RoundVector3(turret.Ship.WorldVelocity, 1);

            // Find predicted target ship.
            Ship targetShip = null;
            double targetAngle = -1;
            double targetDistance = -1;

            foreach (Ship ship in ShipRegistry.All)
            {
                if (ship.ShipId == ShipId) continue; // Dont target own ship.

                Vector3 targetVector = ship.Position - GunPosition;
                double dot = Vector3.Dot(targetVector, shotDirection);
                double magA = Vector3.Magnitude(targetVector);
                double magB = Vector3.Magnitude(shotDirection);
                double angle = Math.Acos(dot / (magA * magB));
                angle = Math.Abs(angle);
                double distance = Vector3.Magnitude(targetVector);

                // TODO low prio: Bias to closer ships. Account for projectile drop.
                if (targetShip == null || angle < targetAngle)
                {
                    targetShip = ship;
                    targetAngle = angle;
                    targetDistance = (float)distance;
                }
            }
            if (targetShip != null)
            {
                TargetShipId = targetShip.ShipId;
                TargetPosition = RoundVector3(targetShip.position, 0);
                TargetDistance = (int)targetDistance;
                //FileLog.Log($"Shot data: " +
                //    $"\n\ttgt {GunPosition} ,  {targetShip.position} ,  {targetDistance}" +
                //    $"\n\tywp: {turret.WorldYaw} {turret.WorldPitch}" +
                //    $"\n\tdir: {shotDirection}");
                //FileLog.Log($"Target ship: {targetShip.ShipId} {targetShip.name} D: {TargetDistance} A: {targetAngle}");
            }

            // Turret stats
            MuzzleVelocity = (int)turret.Data.muzzleSpeed;
            GunItemId = turret.ItemId;
            AmmoItemId = turret.AmmoEquipmentID;
        }

        public void AddHit(HitData hitData, int hitIndex)
        {
            DidHit = true;
            TargetPosition = hitData.Position;
            TargetDistance = (int)Vector3.Magnitude(TargetPosition - GunPosition);
            TargetShipId = hitData.TargetShipId;
            //hitData.ShotIndex = ShotIndex;
            HitIndexes.Add(hitIndex);
        }

        public Vector3 PositionAt(long timestamp)
        {
            // Return predicted position at a given timestamp.
            // TODO: consider arming for mines.
            // TODO: consider projectile drop.
            // TODO: consider jitter cone.
            Vector3 forwards = GunDirection.normalized;
            float deltaT = (float)(timestamp - ShotTimestamp);
            deltaT /= 1000;

            Vector3 position = GunPosition + forwards * MuzzleVelocity * deltaT;

            return position;
        }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
