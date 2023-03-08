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
    class GameData
    {
        public List<ShotData> Shots = new List<ShotData>();
        public List<HitData> Hits = new List<HitData>();
        // Cool stuff todo:
        // Heatmap / ShipPositions
        // Kills
        // Spawns
        // Repairs

        public GameData()
        {
            //GameStartTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            
        }

        

        public void TurretFired(Turret turret)
        {
            // Fire one projectile per buckshot.
            GunItem gunItem = CachedRepository.Instance.Get<GunItem>(turret.ItemId);
            int buckshots = 1;
            if (gunItem.Params.ContainsKey("iRaysPerShot"))
            {
                buckshots = int.Parse(gunItem.Params["iRaysPerShot"]);
            }
            FileLog.Log($"new shot {buckshots}");

            // Add shots, add multiple for buckshots.
            for (int i = 0; i < buckshots; ++i)
            {
                ShotData shot = new ShotData(turret, Shots.Count);
                Shots.Add(shot);
                FileLog.Log($"Projectile Fired {turret.SlotName} {turret.name}");
            }
            string shotJson = JsonConvert.SerializeObject(Shots[Shots.Count - 1]);
            FileLog.Log($"" +
                $"NEW SHOT\n{shotJson}" +
                $"");

        }

        public void ProjectileHit(MuseEvent evt, Turret turret)
        {
            FileLog.Log($"Projectile Hit {turret.SlotName} {turret.name}");
            HitData hitData = new HitData(evt, turret, Hits.Count);
            Hits.Add(hitData);

            // TODO: Do on match end for all shots instead.
            MatchHitWithShot(hitData);
            
            string hitJson = JsonConvert.SerializeObject(hitData);
            //string shotJson = JsonConvert.SerializeObject(Shots[Shots.Count - 1]);
            FileLog.Log($"" +
                $"HIT\n{hitJson}" +
                //$"\nMATCHED SHOT{shotJson}" +
                $"");
        }



        public void MatchHitWithShot(HitData hit)
        {
            // Match the hit with the shot that created it.

            // TODO: Can be optimized a lot using the timestamps and projectile lifetimes
            // + considering that the shots are sorted on time fired.
            
            int bestMatchIndex = -1;
            float bestMatchRating = float.MaxValue;
            FileLog.Log("Matching hit...");
            for (int i = Shots.Count - 1; i >= 0; --i)
            {
                ShotData shot = Shots[i];

                // Shot was already matched.
                // TODO: Check if this is a better match?
                if (shot.DidHit) continue;

                // Needs to match.
                if (shot.ShooterUserId != hit.ShooterUserId || 
                    shot.ShipId != hit.ShipId || 
                    shot.GunSlot != hit.GunSlot || 
                    shot.ShotTimestamp > hit.HitTimestamp) 
                {
                    continue;
                }
                
                // Rate the match
                // TODO: special check for mines.
                // TODO: special check for tempest.
                // TODO: special check for detonator like guns.
                Vector3 predictedDiff = shot.PositionAt(hit.HitTimestamp) - hit.HitLocation;
                float rating = predictedDiff.magnitude;

                float deltaT = (float)(hit.HitTimestamp - shot.ShotTimestamp);
                deltaT /= 1000;
                FileLog.Log($"" +
                    $"Testing Shot {i}: dT:{deltaT} diff: {predictedDiff.magnitude} | {hit.HitTimestamp/1000} {shot.ShotTimestamp/1000}" +
                    $"\n\tP1: {hit.HitLocation} " +
                    $"\n\tP2: {shot.PositionAt(hit.HitTimestamp)}");

                // TODO: check projectile lifetime.

                if (bestMatchIndex == -1 || rating < bestMatchRating)
                {
                    bestMatchIndex = i;
                    bestMatchRating = rating;
                }
            }
            if (bestMatchIndex != -1)
            {
                Shots[bestMatchIndex].AddHit(hit);
                hit.ShotIndex = bestMatchIndex;
            }
            else
            {
                FileLog.Log("NO MATCHING SHOT FOR HIT?=!??!");
            }


        }
    }

    class HitData
    {
        public int HitIndex;
        public int ShotIndex = -1; // Related shot.
        public long HitTimestamp;

        public int ShooterUserId;
        public int ShipId;
        public int GunSlot;
        public int GunItemId;

        [JsonIgnore]
        public Vector3 HitLocation = Vector3.zero;

        public float[] HitLocationArr;

        public int HitCount = 0; // Number of components hit.
        public List<HitDamage> HitDamages = new List<HitDamage>();

        public struct HitDamage
        {
            public int ShipId;
            public int ComponentId;
            public string ComponentType;
            public string ComponentSlot;
            public bool ComponentBroken;
            public float AoEDistance;
            public int Damage;
            public bool CoreHit;
        }

        public HitData(MuseEvent evt, Turret turret, int index)
        {
            HitTimestamp = MatchDataRecorder.GetActiveGameTimestamp();
            HitIndex = index;

            HitCount = (int)evt.GetInteger(0);

            for (int i = 0; i < HitCount; i++)
            {
                FileLog.Log($"SubHit start");

                // Find the GameObject of the component hit based on identifier.
                int targetId = (int)evt.GetInteger(i * 10 + 1);
                int damage = (int)evt.GetInteger(i * 10 + 2);
                int hitType = (int)evt.GetInteger(i * 10 + 4);
                int shooterId = (int)evt.GetInteger(i * 10 + 6);

                Muse.Vector3 museVec1 = evt.GetFixedVector(i * 10 + 3, 2); // Relative hit location to target component
                Muse.Vector3 museVec2 = evt.GetFixedVector(i * 10 + 5, 2); // Hit location when missed, idk?
                UnityEngine.Vector3 vec1 = new UnityEngine.Vector3(museVec1.x, museVec1.y, museVec1.z);
                UnityEngine.Vector3 vec2 = new UnityEngine.Vector3(museVec2.x, museVec2.y, museVec2.z);

                // Find the Repairable object in game object.

                FileLog.Log($"Finding Target {targetId}");
                Transform targetTransform = MuseWorldObject.FindByNetworkId(targetId);
                if (targetTransform == null)
                {
                    FileLog.Log("NO TARGET TRANSFORM FOUND!");
                    HitCount -= 1;
                    continue;
                }
                GameObject targetGO = targetTransform.gameObject;
                Component[] components = targetGO.GetComponents<MonoBehaviour>();

                FileLog.Log($"Components found {components.Length}");

                Ship sourceShip = turret.Ship;
                Ship targetShip;



                bool coreHit = (hitType & 1) > 0;
                //bool directHit = i == 0;
                //bool hitWeakness = (num2 & 2) > 0;
                //bool hitProtection = (num2 & 4) > 0;
                bool componentBroken = false;
                //int distance = (int)(turret.transform.position - targetComponent.transform.position).magnitude;

                Vector3 subhitLocation = Vector3.zero;

                Repairable targetComponent = null;
                foreach (Component c in components)
                {
                    //FileLog.Log($"{c.GetType()}");
                    if (c is Engine || c is Hull || c is Turret || c is Balloon)
                    {
                        targetComponent = (Repairable)c;
                        targetShip = targetComponent.Ship;

                        subhitLocation = targetComponent.transform.TransformPoint(vec1);
                        componentBroken = targetComponent.Health - damage <= 1;

                        break;
                    }
                    if (c is BaseShell)
                    {
                        FileLog.Log($"TYPE IS SHELL");

                    }
                }

                // Make sure valid target component was found.
                if (targetComponent == null)
                {
                    FileLog.Log("NO COMPONENT FOUND FOR HIT");
                    HitCount -= 1;
                    continue;
                }

                targetShip = targetComponent.Ship;

                // First hit is always the direct hit, following are AoE
                if (i == 0)
                {
                    HitLocation = subhitLocation;
                    ShooterUserId = shooterId;
                    ShipId = sourceShip.ShipId;
                    GunSlot = turret.SlotName[turret.SlotName.Length - 1] - '0';
                    GunItemId = turret.ItemId;
                }

                float aoeDistance = (subhitLocation - HitLocation).magnitude;


                HitDamage hitDamage = new HitDamage()
                {
                    ShipId = targetShip.ShipId,
                    ComponentId = targetComponent.ItemId,
                    ComponentType = targetComponent.Type.ToString(),
                    ComponentSlot = targetComponent.SlotName,
                    ComponentBroken = componentBroken,
                    AoEDistance = aoeDistance,
                    Damage = damage,
                    CoreHit = coreHit
                };
                HitDamages.Add(hitDamage);
                FileLog.Log($"SubHit processed");
            }
            HitLocationArr = new float[]{HitLocation.x, HitLocation.y, HitLocation.z};
        }
    }

    class ShotData
    {
        public int ShotIndex;
        public long ShotTimestamp;

        // Updated if hit is matched.
        public bool DidHit = false;
        public int HitIndex = -1; 
        public long HitTimestamp = -1;

        public int ShooterUserId;
        public int ShipId;
        public int ShipIndex;
        public int GunSlot;

        public int GunItemId;
        public int AmmoItemId;

        [JsonIgnore]
        public Vector3 GunPosition;
        public float[] GunPositionArr;
        [JsonIgnore]
        public Vector3 GunDirection;
        public float[] GunDirectionArr;
        public float MuzzleVelocity;

        // Target position predicted on projectile shot, updated if hit.
        public int TargetShipId;
        [JsonIgnore]
        public Vector3 TargetPosition;
        public float[] TargetPositionArr;
        public float TargetDistance;

        public ShotData(Turret turret, int shotIndex)
        {
            // TODO: Use match time timestamp.
            ShotTimestamp = MatchDataRecorder.GetActiveGameTimestamp();

            ShotIndex = shotIndex;

            NetworkedPlayer user = turret.UsingPlayer;
            ShooterUserId = -2; // Default no user
            if (user != null) // Player or AI
            {
                ShooterUserId = user.UserId;
            }
            if (ShooterUserId == 0) ShooterUserId = -1; // AI user

            ShipId = turret.Ship.ShipId;
            ShipIndex = turret.Ship.CrewIndex;

            // 1 indexed gun slot
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
                TargetDistance = (float)targetDistance;
                //FileLog.Log($"Shot data: " +
                //    $"\n\ttgt {GunPosition} ,  {targetShip.position} ,  {targetDistance}" +
                //    $"\n\tywp: {turret.WorldYaw} {turret.WorldPitch}" +
                //    $"\n\tdir: {shotDirection}");
                //FileLog.Log($"Target ship: {targetShip.ShipId} {targetShip.name} D: {TargetDistance} A: {targetAngle}");
            }

            MuzzleVelocity = turret.Data.muzzleSpeed;
            GunItemId = turret.ItemId;
            AmmoItemId = turret.AmmoEquipmentID;

            // Cannot serialize regular Vector3, use float arrays instead.
            GunPositionArr = new float[] { GunPosition.x, GunPosition.y, GunPosition.z };
            GunDirectionArr = new float[] { GunDirection.x, GunDirection.y, GunDirection.z };
            TargetPositionArr = new float[] { TargetPosition.x, TargetPosition.y, TargetPosition.z };
        }

        public void AddHit(HitData hit)
        {
            DidHit = true;
            HitIndex = hit.HitIndex;
            HitTimestamp = hit.HitTimestamp;
            TargetPosition = hit.HitLocation;
            TargetPositionArr = new float[] { TargetPosition.x, TargetPosition.y, TargetPosition.z };
            TargetDistance = Vector3.Magnitude(TargetPosition - GunPosition);
            TargetShipId = hit.HitDamages[0].ShipId;
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
    }
}
