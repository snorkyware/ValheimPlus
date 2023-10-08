using HarmonyLib;
using UnityEngine;
using ValheimPlus.Configurations;
using System.Linq;
using TMPro;
using System.Collections.Generic;

namespace ValheimPlus.UI
{
    /// <summary>
    /// Shows current and total ammo counts below bow icon, if bow is equipped in hotbar
    /// </summary>
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    public static class HotkeyBar_UpdateIcons_Patch
    {
        private const string hudObjectNamePrefix = "BowAmmoCounts";
        private const string noAmmoDisplay = "No Ammo";

        private static readonly GameObject[] ammoCounters = new GameObject[8];
        private static int elementCount = -1;

        private static bool IsEnabled()
        {
            return Configuration.Current.Hud.IsEnabled && Configuration.Current.Hud.displayBowAmmoCounts > 0;
        }

        private static void Prefix(HotkeyBar __instance, Player player)
        {
            if (!IsEnabled()) return;
            elementCount = __instance.m_elements.Count;

            // On death or player removal, also remove all the ammo counters
            if (player == null || player.IsDead())
            {
                DestroyAllAmmoCounters();
            }
        }

        private static void Postfix(HotkeyBar __instance, Player player)
        {
            if (!IsEnabled()) return;
            if (elementCount != __instance.m_elements.Count)
            {
                // If the element count changed, it was completely re-made. Destroy all the ammo counters so that they get remade,
                // otherwise the ammo counter won't be visible.
                DestroyAllAmmoCounters();
            }
            if (player == null || player.IsDead()) return;
            DisplayAmmoCountsUnderBowHotbarIcons(__instance, player);
        }

        private static void DisplayAmmoCountsUnderBowHotbarIcons(HotkeyBar __instance, Player player)
        {
            // keep track of which slots are empty
            HashSet<int> notSeenItemIndices = new HashSet<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };

            foreach (ItemDrop.ItemData item in __instance.m_items)
            {
                if (item != null)
                {
                    notSeenItemIndices.Remove(item.m_gridPos.x);
                    DisplayAmmoCountsUnderBowHotbarIcon(__instance, player, item);
                }
            }

            // if an item was removed from a hotbar slot, we need to destroy its counter
            foreach (int i in notSeenItemIndices) DestroyAmmoCounter(i);
        }

        private static void DisplayAmmoCountsUnderBowHotbarIcon(HotkeyBar __instance, Player player, ItemDrop.ItemData item)
        {
            int elementIndex = item.m_gridPos.x;
            GameObject ammoCounter = ammoCounters[elementIndex];

            if (
                // item is not a bow
                item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Bow ||
                // we only display on equipped and this bow is not equipped
                (Configuration.Current.Hud.displayBowAmmoCounts == 1 && !player.IsItemEquiped(item)) ||
                // invalid element index
                elementIndex >= __instance.m_elements.Count || elementIndex < 0)
            {
                DestroyAmmoCounter(elementIndex);
                return;
            }

            // Create a new text element to display the ammo counts
            HotkeyBar.ElementData element = __instance.m_elements[elementIndex];
            TMP_Text ammoCounterText;
            if (ammoCounter == null)
            {
                var originalGameObject = element.m_amount.gameObject;
                ammoCounter = GameObject.Instantiate(originalGameObject, originalGameObject.transform.parent, false);
                ammoCounter.name = hudObjectNamePrefix + elementIndex;
                ammoCounter.SetActive(true);
                Vector3 offset = originalGameObject.transform.position - element.m_icon.transform.position - new Vector3(0, 15);
                ammoCounter.transform.Translate(offset);
                ammoCounterText = ammoCounter.GetComponentInChildren<TMP_Text>();
                ammoCounterText.fontSize -= 2;
                ammoCounters[elementIndex] = ammoCounter;
            }
            else
            {
                ammoCounterText = ammoCounter.GetComponentInChildren<TMP_Text>();
            }

            // Attach it to the hotbar icon
            ammoCounter.gameObject.transform.SetParent(element.m_amount.gameObject.transform.parent, false);

            // Find the active ammo being used for the bow
            ItemDrop.ItemData ammoItem = player.m_ammoItem;
            if (ammoItem == null || ammoItem.m_shared.m_ammoType != item.m_shared.m_ammoType)
            {
                // either no ammo is equipped, or the equipped ammo doesn't match the type required by the hotbar item.
                ammoItem = player.GetInventory().GetAmmoItem(item.m_shared.m_ammoType);
            }

            // Calculate totals to display for current ammo type and all types
            int currentAmmo = 0;
            int totalAmmo = 0;
            var inventoryItems = player.GetInventory().GetAllItems();
            foreach (ItemDrop.ItemData inventoryItem in inventoryItems)
            {
                if (inventoryItem.m_shared.m_ammoType == item.m_shared.m_ammoType &&
                    (inventoryItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || inventoryItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable))
                {
                    totalAmmo += inventoryItem.m_stack;

                    if (inventoryItem.m_shared.m_name == ammoItem.m_shared.m_name)
                        currentAmmo += inventoryItem.m_stack;
                }
            }

            // Change the visual display text for the UI
            if (totalAmmo == 0)
                ammoCounterText.text = noAmmoDisplay;
            else
                ammoCounterText.text = ammoItem.m_shared.m_name.Split('_').Last() + "\n" + currentAmmo + "/" + totalAmmo;
        }

        private static void DestroyAllAmmoCounters()
        {
            for (int i = 0; i < 8; i++)
            {
                DestroyAmmoCounter(i);
            }
        }

        private static void DestroyAmmoCounter(int index)
        {
            GameObject ammoCounter = ammoCounters[index];
            if (ammoCounter != null)
            {
                GameObject.Destroy(ammoCounter);
                ammoCounters[index] = null;
            }
        }
    }
}
