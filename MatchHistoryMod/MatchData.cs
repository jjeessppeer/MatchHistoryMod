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
    public class GameData
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
            ShotData shot = new ShotData(turret, GameShots.Count);
            GameShots.Add(shot);
        }


        public void ProjectileHit(MuseEvent evt, Turret turret)
        {
            //const int GATLING_ITEM_ID = 171;
            //const int FLAMER_ITEM_ID = 172;
            //const int LASER_ITEM_ID = 172;
            //if (turret.ItemId == GATLING_ITEM_ID || turret.ItemId == FLAMER_ITEM_ID || turret.ItemId == LASER_ITEM_ID)
            //{
            //    // Fire rate is too high. Multiple hits are merged into one event.
            //    // TODO: Split hit event into multiple.
            //    // Check damage done, if higher than single shot vs component: split into x.
            //    return;
            //}

            int hitCount = (int)evt.GetInteger(0);

            for (int i = 0; i < hitCount; i++)
            {
                HitData hitData = HitData.ParseHitEvent(evt, turret, i);
                GameHits.Add(hitData);

                int shotIndex = FindMatchingShot(hitData);
                if (shotIndex != -1)
                {
                    GameShots[shotIndex].AddHit(hitData, GameHits.Count - 1);
                    hitData.ShotIndex = shotIndex;
                }
                else
                {
                    FileLog.Log("NO MATCHING SHOT?");
                }
            }
            //FileLog.Log(MatchDataRecorder.GetJSONDump());

            
        }

        public float RateShotHitCorrelation(ShotData shot, HitData hit)
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
                if (shot.Buckshots > 1) continue;
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
                // TODO: break condition when shot timestamp is some minimum value.
                float rating = RateShotHitCorrelation(GameShots[i], hit);
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
