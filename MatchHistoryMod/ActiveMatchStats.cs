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
            public long GameStartTimestamp;
            public List<ShotData> Shots = new List<ShotData>();

            // Cool stuff todo:
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

            public void TurretFired(Turret turret)
            {
                FileLog.Log($"Projectile Fired {turret.SlotName} {turret.name}");
                ShotData shot = new ShotData(turret, Shots.Count);
                Shots.Add(shot);
            }

            //public void MatchHitWithShot(HitData hit)
            //{
            //    // Match the hit with the shot that created it.
            //    int bestMatch = -1;
            //    for (int i = 0; i < Shots.Count; i++)
            //    {
            //        if (bestMatch == -1 && Shots[i].HitIndex == -1)
            //        {
            //            bestMatch == i;
            //        }
            //        else 
            //        {

            //        }
            //    }

            //    return null;
            //}
        }

        class HitDamage
        {
            public int HitIndex;
            public int ShotIndex;

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
            public int ShotIndex;
            public long ShotTimestamp;
            public long HitTimestamp = -1;
            public bool DidHit = false;

            public int ShooterUserId;
            public int ShipId;
            public int GunSlot;

            public string GunType;
            public string AmmoType;
            
            //public float Velocity;
            public Vector3 GunPosition;
            public Vector3 GunAngles;

            // Target pos predicted on projectile shot, updated if hit.
            public int TargetShipId;
            public Vector3 TargetPosition; 
            public float TargetDistance;

            public List<HitDamage> Damages = new List<HitDamage>();

            
            public ShotData(Turret turret, int shotIndex)
            {
                // TODO: Use match time timestamp.
                var unixTime = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                ShotTimestamp = unixTime.Ticks / TimeSpan.TicksPerMillisecond;

                ShotIndex = shotIndex;

                NetworkedPlayer user = turret.UsingPlayer;
                ShooterUserId = -2; // Default no user
                if (user != null) // Player or AI
                {
                    ShooterUserId = user.UserId;
                }
                if (ShooterUserId == 0) ShooterUserId = -1; // AI user


                ShipId = turret.Ship.ShipId;

                char slotName = turret.SlotName[turret.SlotName.Length-1];
                GunSlot = slotName - '0';

                GunPosition = turret.position;
                GunAngles = turret.eulerAngles;
                //Vector3 forwardsVector = Vector3()

                // Angles to directional vector
                double xzLen = Math.Cos(turret.WorldPitch * 0.0174533);
                Vector3 shotDirection = new Vector3(
                    (float) (xzLen * -Math.Sin(-turret.WorldYaw * 0.0174533)),
                    (float) -Math.Sin(turret.WorldPitch * 0.0174533), 
                    (float) (xzLen * Math.Cos(turret.WorldYaw * 0.0174533))
                );

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
                    TargetDistance = (float)targetDistance;
                    //FileLog.Log($"Shot data: " +
                    //    $"\n\ttgt {GunPosition} ,  {targetShip.position} ,  {targetDistance}" +
                    //    $"\n\tywp: {turret.WorldYaw} {turret.WorldPitch}" +
                    //    $"\n\tdir: {shotDirection}");
                    FileLog.Log($"Target ship: {targetShip.ShipId} {targetShip.name} D: {TargetDistance} A: {targetAngle}");
                }




            }

            //public float DistanceTravelledAt(long timestamp)
            //{
            //    // TODO: handle mines.
            //    return Velocity * timestamp / 1000;
            //}
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), "OnRemoteUpdate")]
        private static void TurretUpdate(Turret __instance)
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
            if (oldAmmunition <= ammounition) return; // Continue only if ammo decreased.

            if (ammounition == 0 && oldAmmunition >= 3)
            {
                // Reloaded early.
                // TODO: better conditions for low ammo guns
                return;
            }

            // Todo, multiple shots same frame?
            if (oldAmmunition - ammounition != 1)
            {
                FileLog.Log("MULTISHOT");

            }
            int shotsFired = (int) (oldAmmunition - ammounition);
            for (int i = 0; i < shotsFired; i++)
            {
                ActiveGameData.TurretFired(__instance);
            }


            //NetworkedPlayer user = __instance.UsingPlayer;
            //int userId = -2;
            //if (user != null)
            //{
            //    userId = user.UserId;
            //}
            //if (userId == 0) userId = -1;
            //if (userId == -2)
            //{
            //    // Should never happen
            //    FileLog.Log("No user");
            //}
            ////System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            ////FileLog.Log(t.ToString());
            //FileLog.Log($"Projectile Fired Post" +
            //    $"\n\tTurretId: {__instance.ItemId}" +
            //    $"\n\tUserId: {userId}" +
            //    $"\n\tOldAmmo: {oldAmmunition}" +
            //    $"\n\tNewAmmo: {ammounition}" +
            //    $"\n\tClipSize: {clipSize}");
            
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

            //HitData hitData = new HitData();
            //for (int i = 0; i < nHits; i++)
            //{
            //    FileLog.Log($"Hit start");

            //    // Find the GameObject of the component hit based on identifier.
            //    int targetId = (int)evt.GetInteger(i * 10 + 1);
            //    int damage = (int)evt.GetInteger(i * 10 + 2);
            //    int hitType = (int)evt.GetInteger(i * 10 + 4);
            //    int shooterId = (int)evt.GetInteger(i * 10 + 6);

            //    Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Hit location?
            //    Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location?
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

            //    HitData.SubHit subHit = new HitData.SubHit()
            //    {
            //        ShooterId = shooterId,
            //        ShooterShipId = sourceShip.ShipId,
            //        ShooterShipIndex = sourceShip.CrewIndex,
            //        TargetId = targetId,
            //        TargetShipId = targetShip.ShipId,
            //        TargetShipIndex = targetShip.CrewIndex,
            //        TargetComponentType = targetComponent.Type.ToString(),
            //        DirectHit = directHit,
            //        CoreHit = coreHit,
            //        ComponentBroken = componentBroken,
            //        Damage = damage,
            //        Distance = distance
            //    };
            //    hitData.addSubHit(subHit);
            //    string json = JsonConvert.SerializeObject(subHit);
            //    FileLog.Log(json);
            //    //FileLog.Log($"" +
            //    //    $"{__instance.name} hit.\n\t" +
            //    //    $"shooter: {shooterId}\n\t" +
            //    //    $"TargetC: {targetComponent.name}\n\t" +
            //    //    $"Extra: {}" +
            //    //    $"Dist: {distance}\n\t" +
            //    //    $"Damage: {damage}\n\t" +
            //    //    $"FixedVec1: {vec1}\n\t" +
            //    //    $"FixedVec2: {vec2}\n\t" +
            //    //    $"TargetPos: {targetComponent.transform.position}\n\t" +
            //    //    $"TurretPos: {__instance.transform.position}");
            //}
        }


    }


    //class ShipStats
    //{
    //    public int Id;
    //    public int Kills;
    //    public int Deaths;
    //}
}
