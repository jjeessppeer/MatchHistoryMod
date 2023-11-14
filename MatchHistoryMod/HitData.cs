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
    public class HitData
    {
        public int ShotIndex; // Related shot.
        public int HitIndex;
        public long HitTimestamp;

        public int ShooterUserId;
        public int ShipId;
        public int GunSlot;
        public int GunItemId;

        public int TargetShipId;
        public int TargetComponentId;
        public string TargetComponentType;
        public string TargetComponentSlot;
        public bool TargetComponentBroken;
        public float[] TargetVelocity;

        [JsonIgnore]
        public Vector3 PositionVec;
        public int[] Position;
        public int Damage;
        public bool CoreHit;

        public HitData(MuseEvent evt, Turret turret, int i, int hitIndex)
        {
            // Extract event data.
            int shooterUserId = (int)evt.GetInteger(i * 10 + 6);
            int targetId = (int)evt.GetInteger(i * 10 + 1);
            int damage = (int)evt.GetInteger(i * 10 + 2);
            int hitType = (int)evt.GetInteger(i * 10 + 4);
            string binary = Convert.ToString(hitType, 2);
            FileLog.Log($"HitType: {binary}");
            bool coreHit = (hitType & 1) > 0;
            //bool directHit = i == 0;
            //bool hitWeakness = (hitType & 2) > 0; // Pve shit
            //bool hitProtection = (hitType & 4) > 0; // Pve shit
            Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Relative hit location to target component
            Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location when missed, idk?
            UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
            UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

            Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
            if (targetTransform == null) throw new Exception("No transform found.");

            long hitTimestamp = MatchDataRecorder.GetActiveGameTimestamp();

            Ship targetShip = null;
            Repairable targetComponent = null;
            GameObject targetGO = targetTransform.gameObject;
            Component[] components = targetGO.GetComponents<MonoBehaviour>();
            // Find the target component
            foreach (Component c in components)
            {
                if (c is Engine || c is Hull || c is Turret || c is Balloon)
                {
                    targetComponent = (Repairable)c;
                    targetShip = targetComponent.Ship;
                    break;
                }
            }
            if (targetShip == null || targetComponent == null) throw new Exception("No target component found");

            Vector3 positionVec = targetComponent.transform.TransformPoint(vec1);
            Vector3 velocityVec = targetShip.WorldVelocity;

            HitIndex = hitIndex;
            HitTimestamp = hitTimestamp;
            ShooterUserId = shooterUserId;
            ShipId = turret.Ship.ShipId;
            GunSlot = turret.SlotName[turret.SlotName.Length - 1] - '0';
            GunItemId = turret.ItemId;

            TargetShipId = targetShip.ShipId;
            TargetComponentId = targetComponent.ItemId;
            TargetComponentType = targetComponent.Type.ToString();
            TargetComponentSlot = targetComponent.SlotName;
            TargetComponentBroken = targetComponent.Health - damage <= 1;
            TargetVelocity = new float[] { velocityVec.x, velocityVec.y, velocityVec.z };
            PositionVec = positionVec;
            Position = new int[] { (int)positionVec.x, (int)positionVec.y, (int)positionVec.z };
            Damage = damage;
            CoreHit = (hitType & 1) > 0;
            ShotIndex = -1;
        }

        //public static HitData ParseHitEvent(MuseEvent evt, Turret turret, int i, int hitIndex)
        //{
        //    // Extract event data.
        //    int shooterUserId = (int)evt.GetInteger(i * 10 + 6);
        //    int targetId = (int)evt.GetInteger(i * 10 + 1);
        //    int damage = (int)evt.GetInteger(i * 10 + 2);
        //    int hitType = (int)evt.GetInteger(i * 10 + 4);
        //    //bool directHit = i == 0;
        //    //bool hitWeakness = (hitType & 2) > 0; // Pve shit
        //    //bool hitProtection = (hitType & 4) > 0; // Pve shit
        //    Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Relative hit location to target component
        //    Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location when missed, idk?
        //    UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
        //    UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

        //    long hitTimestamp = MatchDataRecorder.GetActiveGameTimestamp();

        //    Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
        //    if (targetTransform == null) throw new Exception("No transform found.");

        //    Ship targetShip = null;
        //    Repairable targetComponent = null;
        //    GameObject targetGO = targetTransform.gameObject;
        //    Component[] components = targetGO.GetComponents<MonoBehaviour>();
        //    // Find the target component
        //    foreach (Component c in components)
        //    {
        //        if (c is Engine || c is Hull || c is Turret || c is Balloon)
        //        {
        //            targetComponent = (Repairable)c;
        //            targetShip = targetComponent.Ship;
        //            break;
        //        }
        //    }
        //    if (targetShip == null || targetComponent == null) throw new Exception("No target component found");

        //    Vector3 positionVec = targetComponent.transform.TransformPoint(vec1);
        //    Vector3 velocityVec = targetShip.WorldVelocity;

        //    HitData hitData = new HitData()
        //    {
        //        HitIndex = hitIndex,
        //        HitTimestamp = hitTimestamp,
        //        ShooterUserId = shooterUserId,
        //        ShipId = turret.Ship.ShipId,
        //        GunSlot = turret.SlotName[turret.SlotName.Length - 1] - '0',
        //        GunItemId = turret.ItemId,

        //        TargetShipId = targetShip.ShipId,
        //        TargetComponentId = targetComponent.ItemId,
        //        TargetComponentType = targetComponent.Type.ToString(),
        //        TargetComponentSlot = targetComponent.SlotName,
        //        TargetComponentBroken = targetComponent.Health - damage <= 1,
        //        TargetVelocity = new float[] { velocityVec.x, velocityVec.y, velocityVec.z },
        //        PositionVec = positionVec,
        //        Position = new float[] { positionVec.x, positionVec.y, positionVec.z },
        //        Damage = damage,
        //        CoreHit = (hitType & 1) > 0,

        //        ShotIndex = -1
        //    };

        //    return hitData;
        //}

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
