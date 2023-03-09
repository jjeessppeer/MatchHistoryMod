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
        public List<ShotData> GameShots = new List<ShotData>();
        public List<HitData> GameHits = new List<HitData>();
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
            //GunItem gunItem = CachedRepository.Instance.Get<GunItem>(turret.ItemId);
            //int buckshots = 1;
            //if (gunItem.Params.ContainsKey("iRaysPerShot"))
            //{
            //    buckshots = int.Parse(gunItem.Params["iRaysPerShot"]);
            //}

            FileLog.Log($"Adding shot {turret.SlotName} {turret.name}");
            ShotData shot = new ShotData(turret, GameShots.Count);
            GameShots.Add(shot);

            string shotJson = JsonConvert.SerializeObject(GameShots[GameShots.Count - 1]);
            FileLog.Log($"" +
                $"NEW SHOT\n{shotJson}" +
                $"");

        }


        public void ProjectileHit(MuseEvent evt, Turret turret)
        {
            FileLog.Log($"Projectile Hit {turret.SlotName} {turret.name}");
            FileLog.Log($"{evt}");
            const int GATLING_ITEM_ID = 171;
            const int FLAMER_ITEM_ID = 172;
            const int LASER_ITEM_ID = 172;
            if (turret.ItemId == GATLING_ITEM_ID || turret.ItemId == FLAMER_ITEM_ID)
            {
                // Fire rate is too high. Multiple hits are merged into one event.
                // TODO: Split hit event into multiple.
                // Check damage done, if higher than single shot vs component: split into x.
                return;
            }

            int hitCount = (int)evt.GetInteger(0);

            for (int i = 0; i < hitCount; i++)
            {
                HitData hitData = HitData.ParseHitEvent(evt, turret, i);
                GameHits.Add(hitData);
                //string hitJson = JsonConvert.SerializeObject(hitData); 
                //FileLog.Log($"" +
                // $"HIT\n" +
                // $"{hitJson}" +
                // //$"\nMATCHED SHOT{shotJson}" +
                // $"");

                int shotIndex = FindMatchingShot(hitData);
                if (shotIndex != -1)
                {
                    GameShots[shotIndex].AddHit(hitData, GameHits.Count - 1);
                    //FileLog.Log($"SHOT MATCHED\n" +
                    //    $"{GameShots[shotIndex].ShotTimestamp}, {hitData.HitTimestamp}\n" +
                    //    $"{GameShots[shotIndex]}");
                }
                else
                {
                    FileLog.Log("NO MATCHING SHOT?");
                }
            }


            // TODO: Do on match end for all shots instead.
            
        }

        public float RateShotHitMatch(ShotData shot, HitData hit)
        {
            // Return how well shot and hit match
            // Smaller number means better match.
            // -1 means invalid.
            // -2 break loop, stop checking backwards, todo.
            if (shot.ShooterUserId != hit.ShooterUserId ||
                shot.ShipId != hit.ShipId ||
                shot.GunSlot != hit.GunSlot ||
                shot.ShotTimestamp > hit.HitTimestamp)
            {
                return -1;
            }
            // TODO: check projectile lifetime.

            // Check if shot already has a hit to the specified component.
            foreach (int idx in shot.HitIndexes)
            {
                if (GameHits[idx].TargetComponentSlot == hit.TargetComponentSlot && 
                    GameHits[idx].TargetShipId == hit.TargetShipId)
                {
                    // TODO: Test if this hit is better, in that case swap.
                    return -1;
                }
            }

            Vector3 predictedPosition = shot.PositionAt(hit.HitTimestamp);
            float posDiff = (predictedPosition - hit.PositionVec).magnitude;

            return posDiff;
        }

        public int FindMatchingShot(HitData hit)
        {
            int bestMatchIndex = -1;
            float bestMatchRating = float.MaxValue;
            for (int i = GameShots.Count - 1; i >= 0; --i)
            {
                float rating = RateShotHitMatch(GameShots[i], hit);
                if (rating == -1) continue;
                if (rating < bestMatchRating)
                {
                    bestMatchRating = rating;
                    bestMatchIndex = i;
                }
            }
            return bestMatchIndex;
        }
    }

    

    
}
