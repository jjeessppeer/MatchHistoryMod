using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace MatchHistoryMod.ACMI
{
    class ACMISerializer
    {
        private static string GetShipACMIId(Ship ship)
        {
            return $"1000{ship.Side:X2}{ship.CrewIndex:X2}";
        }

        public static string ShipPosition(Ship ship)
        {
            string id = GetShipACMIId(ship);
            string transform = ACMIRecorder.vectorToTransformString(ship.position, ship.Forward);
            string serialized = $"{id},T={transform},Name=goio-ship-{ship.ShipModelId},CallSign={ship.name},Color={ACMIConstants.GetColor(ship.Side)}";
            return serialized;
        }

        public static string ShellFire(BaseShell shell, int index)
        {
            //shell.GetInstanceID
            string id = $"1001{index:D4}";

            string shipId = "1000";
            string shooter = "Unknown";
            int turretType = 0;
            int turretSlot = -1;
            int side = -1;
            try
            {
                PropertyInfo property = typeof(BaseShell).GetProperty("TurretLaunchedFrom", BindingFlags.NonPublic | BindingFlags.Instance);
                var value = property.GetValue(shell, null);
                Turret turret = (Turret)value;
                Ship ship = turret.Ship;
                NetworkedPlayer player = turret.UsingPlayer;
                side = ship.Side;
                if (player != null)
                {
                    if (player.UserId == 0) shooter = "AI";
                    else shooter = player.name;
                }
                shipId = GetShipACMIId(ship);
                turretType = turret.ItemId;
            }
            catch (Exception e)
            {
                FileLog.Log($"Failed to get turret\n{e.ToString()}");
            }
            string transform = ACMIRecorder.vectorToTransformString(shell.position);
            string serialized = $"{id},T={transform},Name=goio-projectile-{turretType},Parent={shipId},Color={ACMIConstants.GetColor(side)}";
            return serialized;
        }

        public static string ShellUpdate(BaseShell shell, float t)
        {

        }
    }
}
