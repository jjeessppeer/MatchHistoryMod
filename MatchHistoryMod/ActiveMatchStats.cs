﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using Muse.Common;
using UnityEngine;
using Muse.Networking;
using LitJson;
using Newtonsoft.Json;

namespace MatchHistoryMod
{
    [HarmonyPatch]
    static class ActiveMatchStats
    {
        //public static List<ShipStats> ShipStatList;

        private static string GetSubjectText(Muse.Goi2.Entity.Announcement announcement)
        {
            string result = string.Empty;
            if (announcement.Subject != null && announcement.Subject.Name != null)
            {
                result = EntityExtensions.FilterPlayerPlatform(announcement.Subject.Name, true);
            }
            else if (announcement.Object != null && announcement.Object.Name != null)
            {
                result = "A Harsh World";
            }
            return result;
        }

        private static string GetVerbText(Muse.Goi2.Entity.Announcement announcement)
        {
            string result = string.Empty;
            if (announcement.Verb != Muse.Goi2.Entity.AnnouncementVerb.None)
            {
                result = announcement.Verb.ToString();
            }
            return result;
        }

        private static string GetObjectText(Muse.Goi2.Entity.Announcement announcement)
        {
            string result = string.Empty;
            if (announcement.Object != null && announcement.Object.Name != null)
            {
                if (!string.IsNullOrEmpty(announcement.Object.ShipName))
                {
                    result = "{0}'s {1}".F(new object[]
                    {
                    announcement.Object.ShipName,
                    announcement.Object.Name
                    });
                }
                else
                {
                    result = announcement.Object.Name;
                }
            }
            return result;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAnnouncementDisplay), "HandleAnnouncement")]
        private static void Announcment(Muse.Goi2.Entity.Announcement newAnnouncement, UIAnnouncementDisplay __instance)
        {
            //Mission mission = Mission.Instance;
            //MatchLobbyView mlv = MatchLobbyView.Instance;
            FileLog.Log($"Announcment HandleAnnouncement");
            FileLog.Log($"{GetSubjectText(newAnnouncement)} | {GetVerbText(newAnnouncement)} | {GetObjectText(newAnnouncement)}");
        }

        static GameData ActiveGameData;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStarted()
        {
            //Mission mission = Mission.Instance;
            //MatchLobbyView mlv = MatchLobbyView.Instance;
            FileLog.Log($"Mission started");
            ActiveGameData = new GameData();
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Ship), "Awake")]
        //private static void ShipInitialized0(Ship __instance)
        //{
        //    FileLog.Log($"Ship Awake {__instance.ShipId}");
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Ship), "Start")]
        //private static void ShipInitialized(Ship __instance)
        //{
        //    FileLog.Log($"Ship Start {__instance.ShipId}");
        //}


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Ship), "OnDeath")]
        //private static void ShipKilled(Ship __instance)
        //{
        //    FileLog.Log($"Ship killed");
        //}

        class GameData
        {
            public List<ShotData> Shots = new List<ShotData>();
            public List<HitData> Hits = new List<HitData>();
            public long GameStartTimestamp;

            // Heatmap / ShipPositions
            // Kills
            // Spawns
            // Repairs

            public GameData()
            {
                //GameStartTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                GameStartTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
            }

            public void MatchHitWithShot(HitData hit)
            {
                // Match the hit with the shot that created it.
                int bestMatch = -1;
                for (int i = 0; i < Shots.Count; i++)
                {
                    if (bestMatch == -1 && Shots[i].HitIndex == -1)
                    {
                        bestMatch == i;
                    }
                    else 
                    {

                    }
                }

                return null;
            }
        }

        class HitDamage
        {
            public int ShotId;
            public int TargetShipIndex;
            public string TargetComponentType;

            public bool DirectHit;
            public bool CoreHit;
            public bool ComponentBroken;
            public int Distance;
            public int Damage;
            public Vector3 HitPosition;
        }

        class ShotData
        {
            public long ShotTimestamp;
            public long HitTimestamp = -1;
            public bool DidHit = false;

            public int ShooterUserId;
            public int ShooterShipIndex;
            public int GunIndex;

            public string GunType;
            public string AmmoType;
            
            public float Velocity;
            public Vector3 GunPosition;
            public Vector3 ShotDirection;

            // Target pos predicted on projectile shot, updated if hit.
            public Vector3 TargetPosition; 
            public int TargetDistance;

            public List<HitDamage> Damages;

            // Test how closely a reported hit seems to correlate to this shot.
            public float RateHitMatch()
            {
                return -1;
            }
            
            public ShotData()
            {
                var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                ShotTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
            }

            public float DistanceTravelledAt(long timestamp)
            {
                // TODO: handle mines.
                return Velocity * timestamp / 1000;
            }
        }


        

        class HitData
        {
            public class HitDamage
            {
            }


            public class SubHit
            {
                public int ShooterId;
                public int ShooterShipId;
                public int ShooterShipIndex;
                public int TargetId;
                public int TargetShipId;
                public int TargetShipIndex;

                public string TargetComponentType;
                public bool DirectHit;
                public bool CoreHit;
                public bool ComponentBroken;
                
                public int Distance;
                public int Damage;
            }
            public long Timestamp;
            public int ShotIndex;
            public List<SubHit> SubHits = new List<SubHit>();
            public HitData()
            {
                var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                Timestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
            }
            public void addSubHit(SubHit s)
            {
                SubHits.Add(s);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnRemoteUpdate")]
        private static void ProjectileShot(Turret __instance)
        {
            int? oldAmmunition = Traverse.Create(__instance).Field("oldAmmunition").GetValue() as int?;
            int? ammounition = Traverse.Create(__instance).Field("ammunition").GetValue() as int?;
            int? clipSize = Traverse.Create(__instance).Field("ammunitionClipSize").GetValue() as int?;

            if (oldAmmunition == ammounition) return; // No ammo change
            if (oldAmmunition == 0)
            {
                // Reloaded
                // ...
                FileLog.Log("Reloaded");
                oldAmmunition = clipSize;
            }
            if (oldAmmunition <= ammounition) return; // Continue if ammo decreased.

            if (ammounition == 0 && oldAmmunition >= 3)
            {
                // Reloaded early.
                // TODO: better conditions for low ammo guns
                return;
            }


            NetworkedPlayer user = __instance.UsingPlayer;
            int userId = -2;
            if (user != null)
            {
                userId = user.UserId;
            }
            if (userId == 0) userId = -1;
            if (userId == -2)
            {
                FileLog.Log("No user");
            }
            //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            //FileLog.Log(t.ToString());
            FileLog.Log($"Projectile Fired Post" +
                $"\n\tTurretId: {__instance.ItemId}" +
                $"\n\tUserId: {userId}" +
                $"\n\tOldAmmo: {oldAmmunition}" +
                $"\n\tNewAmmo: {ammounition}" +
                $"\n\tClipSize: {clipSize}");
            
        }

        // Called on projectile hit.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnCustomEvent")]
        private static void ProjectileHit(int senderId, MuseEvent evt, Turret __instance)
        {
            if (evt.Action != 1) return;

            int nHits = (int)evt.GetInteger(0);

            FileLog.Log($"Projectile hit {nHits}");
            FileLog.Log($"{evt.ToString()}");

            HitData hitData = new HitData();
            for (int i = 0; i < nHits; i++)
            {
                FileLog.Log($"Hit start");

                // Find the GameObject of the component hit based on identifier.
                int targetId = (int)evt.GetInteger(i * 10 + 1);
                int damage = (int)evt.GetInteger(i * 10 + 2);
                int hitType = (int)evt.GetInteger(i * 10 + 4);
                int shooterId = (int)evt.GetInteger(i * 10 + 6);

                Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Hit location?
                Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location?
                UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
                UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

                // Find the Repairable object in game object.
                Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
                GameObject targetGO = targetTransform.gameObject;
                Component[] components = targetGO.GetComponents<MonoBehaviour>();
                Repairable targetComponent = null;
                foreach (Component c in components)
                {
                    if (c is Engine || c is Hull || c is Turret || c is Balloon)
                    {
                        targetComponent = (Repairable)c;
                        break;
                    }
                }

                // Make sure valid target component was found.
                if (targetComponent == null) throw new Exception("Invalid component hit!");

                Ship sourceShip = __instance.Ship;
                Ship targetShip = targetComponent.Ship;

                int distance = (int)(__instance.transform.position - targetComponent.transform.position).magnitude;
                bool directHit = i == 0;
                bool coreHit = (hitType & 1) > 0;
                bool componentBroken = targetComponent.Health - damage <= 1;

                HitData.SubHit subHit = new HitData.SubHit()
                {
                    ShooterId = shooterId,
                    ShooterShipId = sourceShip.ShipId,
                    ShooterShipIndex = sourceShip.CrewIndex,
                    TargetId = targetId,
                    TargetShipId = targetShip.ShipId,
                    TargetShipIndex = targetShip.CrewIndex,
                    TargetComponentType = targetComponent.Type.ToString(),
                    DirectHit = directHit,
                    CoreHit = coreHit,
                    ComponentBroken = componentBroken,
                    Damage = damage,
                    Distance = distance
                };
                hitData.addSubHit(subHit);
                string json = JsonConvert.SerializeObject(subHit);
                FileLog.Log(json);
                //FileLog.Log($"" +
                //    $"{__instance.name} hit.\n\t" +
                //    $"shooter: {shooterId}\n\t" +
                //    $"TargetC: {targetComponent.name}\n\t" +
                //    $"Extra: {}" +
                //    $"Dist: {distance}\n\t" +
                //    $"Damage: {damage}\n\t" +
                //    $"FixedVec1: {vec1}\n\t" +
                //    $"FixedVec2: {vec2}\n\t" +
                //    $"TargetPos: {targetComponent.transform.position}\n\t" +
                //    $"TurretPos: {__instance.transform.position}");
            }
        }


    }


    //class ShipStats
    //{
    //    public int Id;
    //    public int Kills;
    //    public int Deaths;
    //}
}
