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
using System.Reflection;

namespace MatchHistoryMod
{

    public class ObjectListTransposer<T>
    {
        public Dictionary<string, List<object>> Values = new Dictionary<string, List<object>>();

        public ObjectListTransposer()
        {
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in fi)
            {
                if (FieldShouldBeIgnored(info)) continue;
                Values.Add(info.Name, new List<object>());
            }
        }

        //public T GetInstance(int i)
        //{
        //}

        public void Add(T newObj)
        {
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in fi)
            {
                if (FieldShouldBeIgnored(info)) continue;
                Values[info.Name].Add(info.GetValue(newObj));
            }
        }

        private static bool FieldShouldBeIgnored(FieldInfo info)
        {
            foreach (object attr in info.GetCustomAttributes(true))
                if (attr.GetType() == typeof(Newtonsoft.Json.JsonIgnoreAttribute))
                    return true;
            return false;
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

        [JsonIgnore]
        public Vector3 GunPosition;
        public int[] GunPositionArr;
        [JsonIgnore]
        public Vector3 GunDirection;
        public float[] GunDirectionArr;
        public float[] ShipVelocity;
        public int MuzzleVelocity;

        // Target position predicted on projectile shot, updated if hit.
        public int TargetShipId = -1;
        [JsonIgnore]
        public Vector3 TargetPosition;
        public int[] TargetPositionArr;
        public int TargetDistance;

        public bool DidHit = false;
        public List<int> HitIndexes = new List<int>();

        public ShotData()
        {

        }

        public ShotData(Turret turret, int shotIndex)
        {
            // TODO: Use match time timestamp.
            ShotTimestamp = MatchDataRecorder.GetActiveGameTimestamp();
            //ShotIndex = shotIndex;
            ShipId = turret.Ship.ShipId;
            ShipIndex = turret.Ship.CrewIndex;
            TeamIndex = turret.Ship.Side;
            Vector3 velocityVec = turret.Ship.WorldVelocity;
            ShipVelocity = new float[] { velocityVec.x, velocityVec.y, velocityVec.z };

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

            GunPosition = turret.position;
            GunDirection = shotDirection;

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
                TargetPosition = targetShip.position;
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

            // Cannot serialize regular Vector3, use float arrays instead.
            GunPositionArr = new int[] { (int)GunPosition.x, (int)GunPosition.y, (int)GunPosition.z };
            GunDirectionArr = new float[] { GunDirection.x, GunDirection.y, GunDirection.z };
            TargetPositionArr = new int[] { (int)TargetPosition.x, (int)TargetPosition.y, (int)TargetPosition.z };
        }

        public void AddHit(HitData hitData, int hitIndex)
        {
            DidHit = true;
            TargetPosition = hitData.PositionVec;
            TargetPositionArr = hitData.Position;
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
