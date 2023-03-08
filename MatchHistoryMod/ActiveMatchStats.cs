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

namespace MatchHistoryMod
{   
    //[HarmonyPatch]
    //static class MatchStatsPatch
    //{
    //}

    [HarmonyPatch]
    static class MatchDataRecorder
    {
        //public static List<ShipStats> ShipStatList;
        static GameData ActiveGameData;
        static long GameStartTimestamp;


        public static long GetActiveGameTimestamp()
        {
            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
            timestamp -= GameStartTimestamp;
            return timestamp;
        }

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "Start")]
        private static void MissionStarted()
        {
            //Mission mission = Mission.Instance;
            //MatchLobbyView mlv = MatchLobbyView.Instance;
            FileLog.Log($"Mission started");
            ActiveGameData = new GameData();

            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            GameStartTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnRemoteUpdate")]
        private static void TurretUpdate(Turret __instance)
        {
            int? oldAmmunition = Traverse.Create(__instance).Field("oldAmmunition").GetValue() as int?;
            int? ammounition = Traverse.Create(__instance).Field("ammunition").GetValue() as int?;
            int? clipSize = Traverse.Create(__instance).Field("ammunitionClipSize").GetValue() as int?;
            bool? reload= Traverse.Create(__instance).Field("reload").GetValue() as bool?;


            if (oldAmmunition == ammounition) return; // No ammo change
            if (oldAmmunition == 0)
            {
                // Reloaded
                oldAmmunition = clipSize;
            }
            if (oldAmmunition <= ammounition) return; // Continue only if ammo decreased meanin shot was fired.


            //if (oldAmmunition != 0 && reload == true)
            //{
            //    FileLog.Log("EARLY RELOAD");
            //}

            if (ammounition == 0 && oldAmmunition >= 3)
            {
                // Reloaded early.
                // TODO: better conditions for low ammo guns
                // Fails if one reloads on 1 or 2 bullets left
                return;
            }

            int shotsFired = (int) (oldAmmunition - ammounition);
            for (int i = 0; i < shotsFired; i++)
            {
                ActiveGameData.TurretFired(__instance);
            }
            
        }

        // Called on projectile hit.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnCustomEvent")]
        private static void ProjectileHit(int senderId, MuseEvent evt, Turret __instance)
        {
            if (evt.Action != 1) return;
            ActiveGameData.ProjectileHit(evt, __instance);
            //int nHits = (int)evt.GetInteger(0);

            //FileLog.Log($"Projectile hit {nHits}");
            //FileLog.Log($"{evt.ToString()}");

            //HitData hitData = new HitData();
            //Vector3 hitLocation =  Vector3.zero;
            //for (int i = 0; i < nHits; i++)
            //{
            //    FileLog.Log($"Hit start");

            //    // Find the GameObject of the component hit based on identifier.
            //    int targetId = (int)evt.GetInteger(i * 10 + 1);
            //    int damage = (int)evt.GetInteger(i * 10 + 2);
            //    int hitType = (int)evt.GetInteger(i * 10 + 4);
            //    int shooterId = (int)evt.GetInteger(i * 10 + 6);

            //    Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Relative hit location to target component?
            //    Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location when missed?
            //    UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
            //    UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

            //    // Find the Repairable object in game object.
            //    Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
            //    GameObject targetGO = targetTransform.gameObject;
            //    Component[] components = targetGO.GetComponents<MonoBehaviour>();
            //    Repairable targetComponent = null;
            //    foreach (Component c in components)
            //    {
            //        if (c is Engine || c is Hull || c is Turret || c is Balloon)
            //        {
            //            targetComponent = (Repairable)c;
            //            break;
            //        }
            //    }

            //    // Make sure valid target component was found.
            //    if (targetComponent == null) throw new Exception("Invalid component hit!");

            //    Ship sourceShip = __instance.Ship;
            //    Ship targetShip = targetComponent.Ship;

            //    int distance = (int)(__instance.transform.position - targetComponent.transform.position).magnitude;
            //    bool directHit = i == 0;
            //    bool coreHit = (hitType & 1) > 0;
            //    bool componentBroken = targetComponent.Health - damage <= 1;


            //    if (i == 0)
            //    {
            //        hitLocation = targetComponent.transform.TransformPoint(vec1);
            //    }
            //    float AoEDistance = (targetComponent.transform.TransformPoint(vec1) - hitLocation).magnitude;


            //    FileLog.Log($"" +
            //        $"{__instance.id} hit.\n\t" +
            //        $"Shooter: {shooterId}\n\t" +
            //        $"TargetC: {targetComponent.name}\n\t" +
            //        $"Extra: {}" +
            //        $"AoEDistance: {AoEDistance}\n\t" +
            //        $"Damage: {damage}\n\t" +
            //        $"FixedVec1: {vec1}\n\t" +
            //        $"FixedVec2: {vec2}\n\t" +
            //        $"TargetPos: {targetComponent.transform.position}\n\t" +
            //        $"TurretPos: {__instance.transform.position}");

            //    //HitData.SubHit subHit = new HitData.SubHit()
            //    //{
            //    //    ShooterId = shooterId,
            //    //    ShooterShipId = sourceShip.ShipId,
            //    //    ShooterShipIndex = sourceShip.CrewIndex,
            //    //    TargetId = targetId,
            //    //    TargetShipId = targetShip.ShipId,
            //    //    TargetShipIndex = targetShip.CrewIndex,
            //    //    TargetComponentType = targetComponent.Type.ToString(),
            //    //    DirectHit = directHit,
            //    //    CoreHit = coreHit,
            //    //    ComponentBroken = componentBroken,
            //    //    Damage = damage,
            //    //    Distance = distance
            //    //};
            //    //hitData.addSubHit(subHit);
            //    //string json = JsonConvert.SerializeObject(subHit);
            //    //FileLog.Log(json);
            //    FileLog.Log($"" +
            //        $"{__instance.name} hit.\n\t" +
            //        $"shooter: {shooterId}\n\t" +
            //        $"TargetC: {targetComponent.name}\n\t" +
            //        $"Extra: {}" +
            //        $"AoEDistance: {AoEDistance}\n\t" +
            //        $"Damage: {damage}\n\t" +
            //        $"FixedVec1: {vec1}\n\t" +
            //        $"FixedVec2: {vec2}\n\t" +
            //        $"TargetPos: {targetComponent.transform.position}\n\t" +
            //        $"TurretPos: {__instance.transform.position}");
            //}
        }


    }
}
