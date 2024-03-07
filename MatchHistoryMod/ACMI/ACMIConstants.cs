using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatchHistoryMod.ACMI
{
    static class ACMIConstants
    {
        public static float GetMapOffset(string mapName)
        {
            switch (mapName)
            {
                case "Alleron Affray":
                    return 0;
                case "Ashen Scuffle":
                    return -1;
                case "Assault on Kinforth":
                    return -2;
                case "Batcave":
                    return -3;
                case "Battle on the Dunes":
                case "Duel at Dawn":
                case "Graveyard Rumble":
                    return -4;
                case "Canyon Ambush":
                    return -5;
                case "Clash at Blackcliff":
                    return -6;
                case "Crown Gambit":
                    return -7;
                case "Derelict Deception":
                    return -8;
                case "Fight over Firnfeld":
                    return -9;
                case "Misty Mutiny":
                    return -10;
                case "Northern Fjords":
                    return -11;
                case "Oblivion South":
                    return -12;
                case "Paritan Rumble":
                    return -13;
                case "Thornholt Throwndown":
                    return -14;
                case "Water Hazard":
                    return -15;
                default:
                    return 1;
            }
        }

        public static string GetColor(int teamIdx)
        {
            switch (teamIdx)
            {
                case 0:
                    return "Red";
                case 1:
                    return "Blue";
                default:
                    return "Cyan";
            }
        }
    }
}
