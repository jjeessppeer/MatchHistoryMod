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
using System.IO.Compression;
using System.IO;

namespace MatchHistoryMod
{
    public class GunneryData
    {
        [JsonIgnore]
        public List<ShotData> GameShots = new List<ShotData>();
        [JsonIgnore]
        public List<HitData> GameHits = new List<HitData>();

        public ObjectListTransposer<ShotData> GameShotsTransposed = new ObjectListTransposer<ShotData>();
        public ObjectListTransposer<HitData> GameHitsTransposed = new ObjectListTransposer<HitData>();

        // Cool stuff todo:
        // Heatmap for: Ship positions, damage taken at, damage dealt from
        // Kills
        // Spawns
        // Repairs

        public string SerializeAndCompress(object obj)
        {
            string json = JsonConvert.SerializeObject(obj, new VectorJsonConverter());
            byte[] data = Encoding.ASCII.GetBytes(json);
            MemoryStream output = new MemoryStream();
            using (GZipStream dstream = new GZipStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }
            byte[] outArr = output.ToArray();
            string outStr = Convert.ToBase64String(outArr);
            return outStr;
        }

        public GunneryData()
        {
            //GameStartTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        const int GATLING_ITEM_ID = 171;
        const int FLAMER_ITEM_ID = 172;
        const int LASER_ITEM_ID = 1943;
        const int MINE_ITEM_ID = 951;

        public void TurretFired(Turret turret)
        {
            //if (turret.ItemId == GATLING_ITEM_ID ||
            //    turret.ItemId == FLAMER_ITEM_ID ||
            //    turret.ItemId == LASER_ITEM_ID || 
            //    turret.ItemId == MINE_ITEM_ID)
            //{
            //    // TODO: Hit detection broken with rapid fire guns. Multiple hits are merged into one event.
            //    // Split hit event into multiple.
            //    // Check damage done, if higher than single shot vs component: split into x.
            //    // TODO: Mines need additional logic to work.
            //    return;
            //}
            ShotData shot = new ShotData(turret, GameShots.Count);
            FileLog.Log($"Shot {GameShots.Count}");

            GameShots.Add(shot);
            GameShotsTransposed.Add(shot);

            //string s1 = SerializeAndCompress(GameShots);
            //string s11 = JsonConvert.SerializeObject(GameShots);
            //string s3 = SerializeAndCompress(GameShotsTransposed);
            string s33 = JsonConvert.SerializeObject(GameShotsTransposed, new VectorJsonConverter());
            //FileLog.Log($"Objects {s1.Length}");
            FileLog.Log($"Transposed {s33.Length} \n{s33}");

        }

        public void ProjectileHit(MuseEvent evt, Turret turret)
        {
            
            if (
                turret.ItemId == GATLING_ITEM_ID || 
                turret.ItemId == FLAMER_ITEM_ID || 
                turret.ItemId == LASER_ITEM_ID ||
                turret.ItemId == MINE_ITEM_ID)
            {
                // TODO: Hit detection broken with rapid fire guns. Multiple hits are merged into one event.
                // Split hit event into multiple.
                // Check damage done, if higher than single shot vs component: split into x.
                // TODO: Mines need additional logic to work.
                return;
            }

            int hitCount = (int)evt.GetInteger(0);

            for (int i = 0; i < hitCount; i++)
            {
                HitData hitData = new HitData(evt, turret, i, GameHits.Count);
                GameHits.Add(hitData);
                FileLog.Log($"Hit {hitData.HitIndex}");
                int shotIndex = FindMatchingShot(hitData);
                if (shotIndex != -1)
                {
                    GameShots[shotIndex].AddHit(hitData, GameHits.Count - 1);
                    hitData.ShotIndex = shotIndex;
                    FileLog.Log($"Matched with {shotIndex}");
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
                if (shot.Buckshots > 1) continue; // Buckshots are allowed multihits.
                if (GameHits[idx].TargetComponentSlot == hit.TargetComponentSlot && 
                    GameHits[idx].TargetShipId == hit.TargetShipId)
                {
                    // TODO: Test if swapping causes lower avg score.
                    return -1;
                }
            }
            return ShotHitDifference(shot, hit);
        }

        public float ShotHitDifference(ShotData shot, HitData hit)
        {
            Vector3 predictedPosition = shot.PositionAt(hit.HitTimestamp);
            float posDiff = (predictedPosition - hit.Position).magnitude;
            return posDiff;
        }


        public int FindMatchingShot(HitData hit)
        {
            int bestMatchIndex = -1;
            float bestMatchRating = float.MaxValue;
            for (int i = GameShots.Count - 1; i >= 0; --i)
            {
                // TODO: break condition when shot timestamp is some minimum value to.
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
