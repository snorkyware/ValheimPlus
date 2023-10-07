using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Demister), "OnEnable")]
    static class Demister_OnEnable_Patch
    {
        static readonly string wispLight = "demister_ball";
        static readonly string wispTorch = "piece_groundtorch_mist";
        static readonly string mistwalker = "Mistwalker";

        static void Postfix(ref Demister __instance)
        {
            GameObject gameObject = __instance.gameObject;
            if (Utils.GetPrefabName(gameObject.transform.root.name) == wispLight && Configuration.Current.Demister.IsEnabled)
            {
                EditRange(gameObject, Configuration.Current.Demister.wispLight);
            }
            else if (Utils.GetPrefabName(gameObject.transform.root.name) == wispTorch && Configuration.Current.Demister.IsEnabled)
            {
                EditRange(gameObject, Configuration.Current.Demister.wispTorch);
            }
            else if (Utils.GetPrefabName(gameObject.transform.parent.name) == mistwalker && Configuration.Current.Demister.IsEnabled)
            {
                EditRange(gameObject, Configuration.Current.Demister.Mistwalker);
            }
        }

        private static void EditRange(GameObject gameObject, float range)
        {
            gameObject.GetComponentInChildren<ParticleSystemForceField>().endRange = range;
        }
    }
}
