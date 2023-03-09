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
    struct HitData
    {
        public int ShotIndex; // Related shot.
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

        [JsonIgnore]
        public Vector3 PositionVec;
        public float[] Position;
        public int Damage;
        public bool CoreHit;

        public static HitData ParseHitEvent(MuseEvent evt, Turret turret, int i)
        {
            // Extract event data.
            int shooterUserId = (int)evt.GetInteger(i * 10 + 6);
            int targetId = (int)evt.GetInteger(i * 10 + 1);
            int damage = (int)evt.GetInteger(i * 10 + 2);
            int hitType = (int)evt.GetInteger(i * 10 + 4);
            //bool directHit = i == 0;
            //bool hitWeakness = (hitType & 2) > 0; // Pve shit
            //bool hitProtection = (hitType & 4) > 0; // Pve shit
            Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Relative hit location to target component
            Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location when missed, idk?
            UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
            UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

            long hitTimestamp = MatchDataRecorder.GetActiveGameTimestamp();

            Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
            if (targetTransform == null) throw new Exception("No transform found.");

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

            HitData hitData = new HitData()
            {
                HitTimestamp = hitTimestamp,
                ShooterUserId = shooterUserId,
                ShipId = turret.Ship.ShipId,
                GunSlot = turret.SlotName[turret.SlotName.Length - 1] - '0',
                GunItemId = turret.ItemId,

                TargetShipId = targetShip.ShipId,
                TargetComponentId = targetComponent.ItemId,
                TargetComponentType = targetComponent.Type.ToString(),
                TargetComponentSlot = targetComponent.SlotName,
                TargetComponentBroken = targetComponent.Health - damage <= 1,
                PositionVec = positionVec,
                Position = new float[] { positionVec.x, positionVec.y, positionVec.z },
                Damage = damage,
                CoreHit = (hitType & 1) > 0,

                ShotIndex = -1
            };

            return hitData;
        }

        //public HitData(MuseEvent evt, Turret turret, int hitIndex, int i)
        //{
        //    //HitIndex = hitIndex;
        //    HitTimestamp = MatchDataRecorder.GetActiveGameTimestamp();

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

        //    ShooterUserId = shooterUserId;
        //    ShipId = turret.Ship.ShipId;
        //    GunSlot = turret.SlotName[turret.SlotName.Length - 1] - '0';
        //    GunItemId = turret.ItemId;


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

        //    TargetShipId = targetShip.ShipId;
        //    TargetComponentId = targetComponent.ItemId;
        //    TargetComponentType = targetComponent.Type.ToString();
        //    TargetComponentSlot = targetComponent.SlotName;
        //    TargetComponentBroken = targetComponent.Health - damage <= 1;

        //    Vector3 positionVec = targetComponent.transform.TransformPoint(vec1);
        //    Position = new float[] { positionVec.x, positionVec.y, positionVec.z };
        //    Damage = damage;
        //    CoreHit = (hitType & 1) > 0;
        //}
    }
    //class HitData
    //{
    //    public int HitIndex;
    //    public int ShotIndex = -1; // Related shot.
    //    public long HitTimestamp;

    //    public int ShooterUserId;
    //    public int ShipId;
    //    public int GunSlot;
    //    public int GunItemId;

    //    [JsonIgnore]
    //    public Vector3 HitLocation = Vector3.zero;
    //    public float[] HitLocationArr;

    //    public int HitCount = 0; // Number of components hit.
    //    public List<HitDamage> HitDamages = new List<HitDamage>();

    //    public struct HitDamage
    //    {
    //        public int ShipId;
    //        public int ComponentId;
    //        public string ComponentType;
    //        public string ComponentSlot;
    //        public bool ComponentBroken;
    //        public int Damage;
    //        public bool CoreHit;
    //        [JsonIgnore]
    //        public Vector3 Position;
    //        public float[] PositionArr;
    //    }

    //    public HitData(MuseEvent evt, Turret turret, int index)
    //    {
    //        HitTimestamp = MatchDataRecorder.GetActiveGameTimestamp();
    //        HitIndex = index;

    //        HitCount = (int)evt.GetInteger(0);

    //        GunItemId = turret.ItemId;
    //        ShipId = turret.Ship.ShipId;
    //        GunSlot = turret.SlotName[turret.SlotName.Length - 1] - '0';

    //        ShooterUserId = (int)evt.GetInteger(0 * 10 + 6);

    //        for (int i = 0; i < HitCount; i++)
    //        {
    //            if (GunItemId == 951 && i == 0) continue;
    //            ParseSubHit(evt, turret, i);
    //        }
    //        HitLocation = HitDamages[0].Position;
    //        HitLocationArr = new float[] { HitLocation.x, HitLocation.y, HitLocation.z };
    //    }
    
    //    private void ParseSubHit(MuseEvent evt, Turret turret, int i)
    //    {
    //        // Find the GameObject of the component hit based on identifier.
    //        int targetId = (int)evt.GetInteger(i * 10 + 1);
    //        int damage = (int)evt.GetInteger(i * 10 + 2);
    //        int hitType = (int)evt.GetInteger(i * 10 + 4);
    //        int shooterId = (int)evt.GetInteger(i * 10 + 6);

    //        Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Relative hit location to target component
    //        Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location when missed, idk?
    //        UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
    //        UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

    //        // Find the Repairable object in game object.

    //        //FileLog.Log($"Finding Target {targetId}");
    //        Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
    //        if (targetTransform == null)
    //        {
    //            FileLog.Log("NO TARGET TRANSFORM FOUND!");
    //            //HitCount -= 1;
    //            return;
    //        }

    //        //FileLog.Log($"Components found {components.Length}");

    //        Ship targetShip;

    //        bool coreHit = (hitType & 1) > 0;
    //        //bool directHit = i == 0;
    //        //bool hitWeakness = (num2 & 2) > 0; // Pve shit
    //        //bool hitProtection = (num2 & 4) > 0; // Pve shit
    //        bool componentBroken = false;
    //        //int distance = (int)(turret.transform.position - targetComponent.transform.position).magnitude;

    //        Vector3 subhitLocation = Vector3.zero;

    //        GameObject targetGO = targetTransform.gameObject;
    //        Component[] components = targetGO.GetComponents<MonoBehaviour>();
    //        Repairable targetComponent = null;
    //        foreach (Component c in components)
    //        {
    //            //FileLog.Log($"{c.GetType()}");
    //            if (c is Engine || c is Hull || c is Turret || c is Balloon)
    //            {
    //                targetComponent = (Repairable)c;
    //                targetShip = targetComponent.Ship;

    //                subhitLocation = targetComponent.transform.TransformPoint(vec1);
    //                componentBroken = targetComponent.Health - damage <= 1;

    //                break;
    //            }
    //            if (c is BaseShell)
    //            {
    //                // TODO: mines are fucky, skip for fix.
    //                FileLog.Log($"TYPE IS SHELL");

    //            }
    //        }

    //        // Make sure valid target component was found.
    //        if (targetComponent == null)
    //        {
    //            FileLog.Log("NO COMPONENT FOUND FOR HIT");
    //            HitCount -= 1;
    //            return;
    //        }

    //        targetShip = targetComponent.Ship;

      
    //        HitDamage hitDamage = new HitDamage()
    //        {
    //            ShipId = targetShip.ShipId,
    //            ComponentId = targetComponent.ItemId,
    //            ComponentType = targetComponent.Type.ToString(),
    //            ComponentSlot = targetComponent.SlotName,
    //            ComponentBroken = componentBroken,
    //            Damage = damage,
    //            CoreHit = coreHit,
    //            Position = subhitLocation,
    //            PositionArr = new float[] { subhitLocation.x, subhitLocation.y, subhitLocation.z }
    //    };
    //        HitDamages.Add(hitDamage);
    //        FileLog.Log($"SubHit {damage}");
    //    }
    //}
}
