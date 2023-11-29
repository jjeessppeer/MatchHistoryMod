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

    [HarmonyPatch]
    static class MatchDataRecorder
    {
        public static GunneryData ActiveGunneryData;
        public static List<ShipPositionData> ShipPositions = new List<ShipPositionData>();
        static long GameStartTimestamp;

        public static string GetJSONDump()
        {
            return JsonConvert.SerializeObject(ActiveGunneryData, new VectorJsonConverter());
        }

        struct TableKey
        {
            public int PlayerId;
            public int GunId;
        }
        struct TableEntry
        {
            public int Shots;
            public int Hits;
        }
        public static string GetTableDump()
        {

            Dictionary<TableKey, TableEntry> table = new Dictionary<TableKey, TableEntry>();
            foreach (ShotData shot in ActiveGunneryData.GameShots)
            {
                TableKey key = new TableKey() { 
                    PlayerId = shot.ShooterUserId,
                    GunId = shot.GunItemId
                };
                if (!table.ContainsKey(key)) table.Add(key, new TableEntry() { Shots = 0, Hits = 0 });
                TableEntry t = table[key];
                t.Shots += 1;
                if (shot.DidHit) t.Hits += 1;
                table[key] = t;
            }

            string output = "Player\tGun\tShots\tHits\tAcc\n";
            foreach(var kvp in table)
            {
                float shots = kvp.Value.Shots;
                float hits = kvp.Value.Hits;
                
                double acc = Math.Round((hits / shots) * 100);
                output += $"{kvp.Key.PlayerId}\t{kvp.Key.GunId}\t{shots}\t{hits}\t{acc}%\n";
            }
            return output;
        }

        public static void Reset()
        {

            ActiveGunneryData = new GunneryData();
            ShipPositions = new List<ShipPositionData>();
            var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            GameStartTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;
        }

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

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIAnnouncementDisplay), "HandleAnnouncement")]
        //private static void Announcment(Muse.Goi2.Entity.Announcement newAnnouncement, UIAnnouncementDisplay __instance)
        //{
        //    //Mission mission = Mission.Instance;
        //    //MatchLobbyView mlv = MatchLobbyView.Instance;
        //    FileLog.Log($"Announcment HandleAnnouncement");
        //    FileLog.Log($"{GetSubjectText(newAnnouncement)} | {GetVerbText(newAnnouncement)} | {GetObjectText(newAnnouncement)}");
        //}


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
        [HarmonyPatch(typeof(Ship), "OnRemoteUpdate")]
        private static void ShipUpdate(Ship __instance)
        {
            ShipPositionData.TakeSnapshot(__instance, ShipPositions);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "OnRemoteDestroy")]
        private static void ShipDestroy(Ship __instance)
        {
            ShipPositionData.TakeSnapshot(__instance, ShipPositions);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnRemoteUpdate")]
        private static void TurretUpdate(Turret __instance)
        {
            int? oldAmmunition = Traverse.Create(__instance).Field("oldAmmunition").GetValue() as int?;
            int? ammounition = Traverse.Create(__instance).Field("ammunition").GetValue() as int?;
            int? clipSize = Traverse.Create(__instance).Field("ammunitionClipSize").GetValue() as int?;
            bool? reload= Traverse.Create(__instance).Field("reload").GetValue() as bool?;
            if (oldAmmunition == 0)
            {
                oldAmmunition = clipSize;
            }
            if (oldAmmunition <= ammounition) return; // Continue only if ammo decreased, meaning shot was fired.
            if (ammounition == 0 && oldAmmunition >= 2 && reload == true)
            {
                // Gun was reloaded early.
                // TODO: better conditions for low ammo guns
                // Will currently count reload at 1 ammo as a shot.
                return;
            }

            int shotsFired = (int) (oldAmmunition - ammounition);
            //FileLog.Log($"Fired {shotsFired} shots. R:{reload} O:{oldAmmunition} N:{ammounition}");
            for (int i = 0; i < shotsFired; i++)
            {
                try
                {
                    ActiveGunneryData.TurretFired(__instance);
                }
                catch
                {

                }
            }
            
        }

        // Called on projectile hit.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnCustomEvent")]
        private static void ProjectileHit(int senderId, MuseEvent evt, Turret __instance)
        {
            if (evt.Action != 1) return;
            try
            {
                ActiveGunneryData.ProjectileHit(evt, __instance);
            }
            catch
            {}
        }


    }
}
