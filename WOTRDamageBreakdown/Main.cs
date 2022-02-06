using HarmonyLib;
using System.Reflection;
using UnityModManagerNet;

namespace WOTRDamageBreakdown
{

    public class Main
    {
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }
    }
}