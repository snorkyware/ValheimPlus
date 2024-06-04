using HarmonyLib;
using JetBrains.Annotations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class InventoryGrid_UpdateGui_Patch
    {
        /// <summary>
        /// Fixes bug where m_elements is only re-filled when width or height changes.
        /// InventoryGrid is instantiated with a certain width/height, and with an empty m_elements.
        /// If width and height both match the inventory width/height when we get to UpdateGui,
        /// then m_elements is allowed to be an empty list. However, we need m_elements to be of
        /// size `width * height`, so we must force those conditions to be false ourselves. 
        /// </summary>
        [UsedImplicitly]
        private static void Prefix(InventoryGrid __instance)
        {
            int width = __instance.m_inventory.GetWidth();
            int height = __instance.m_inventory.GetHeight();
            
            // Our bug won't trigger, continue with method as normal
            if (__instance.m_width != width || 
                __instance.m_height != height ||
                __instance.m_elements.Count == width * height) return;
            
            // Our bug is about to occur, break one of the conditions to make sure m_elements is re-created.
            // Change m_width to either one or two based on whether the current is already 1 or not.
            // m_width is always set to m_inventory.GetWidth() in this method anyways.
            __instance.m_width = __instance.m_width == 1 ? 2 : 1;
        }
    }
}