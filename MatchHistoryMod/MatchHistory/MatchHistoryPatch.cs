using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace MatchHistoryMod.MatchHistory
{
    [HarmonyPatch]
    class MatchHistoryPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIManager.UIMatchCompleteState), "Enter")]
        private static void MatchComplteStateEnter()
        {
            MatchHistory.SaveMatchHistory();
        }
    }
}
