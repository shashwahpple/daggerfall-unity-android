using System.Collections.Generic;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Shared equip/unequip logic for Display 2 panels (EquipmentPanel's item list, PaperDollPanel's tap-
    /// to-unequip), so neither can equip a broken/forbidden item or leave armor values out of sync in a
    /// way DaggerfallInventoryWindow itself wouldn't allow. Faithful copy of
    /// DaggerfallInventoryWindow.EquipItem/UnequipItem (Assets/Scripts/Game/UserInterfaceWindows/
    /// DaggerfallInventoryWindow.cs:1322-1403), which are protected instance methods and can't be called
    /// directly - reproduced here so both panels stay in sync with exactly one copy of these rules. Any
    /// message boxes this raises (broken item / forbidden equipment) are DFU's own native window-stack
    /// windows, so they render on Display 1, not either panel's screen.
    /// </summary>
    public static class EquipmentActions
    {
        const int ItemBrokenTextId = 29;
        const int ForbiddenEquipmentTextId = 1068;

        public static void EquipItem(PlayerEntity playerEntity, DaggerfallUnityItem item)
        {
            if (item.ItemGroup == ItemGroups.Weapons && item.TemplateIndex == (int)Weapons.Arrow)
                return;

            if (item.currentCondition < 1)
            {
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(ItemBrokenTextId);
                if (tokens != null && tokens.Length > 0)
                {
                    DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, null);
                    messageBox.SetTextTokens(tokens, item);
                    messageBox.ClickAnywhereToClose = true;
                    messageBox.Show();
                }
                return;
            }

            bool prohibited = false;

            if (item.ItemGroup == ItemGroups.Armor)
            {
                if (item.IsShield && ((1 << (item.TemplateIndex - (int)Armor.Buckler) & (int)playerEntity.Career.ForbiddenShields) != 0))
                    prohibited = true;
                else if (!item.IsShield && (1 << (item.NativeMaterialValue >> 8) & (int)playerEntity.Career.ForbiddenArmors) != 0)
                    prohibited = true;
                else if (((item.nativeMaterialValue >> 8) == 2)
                    && (1 << (item.NativeMaterialValue & 0xFF) & (int)playerEntity.Career.ForbiddenMaterials) != 0)
                    prohibited = true;
            }
            else if (item.ItemGroup == ItemGroups.Weapons)
            {
                if ((item.GetWeaponSkillUsed() & (int)playerEntity.Career.ForbiddenProficiencies) != 0)
                    prohibited = true;
                else if ((1 << item.NativeMaterialValue & (int)playerEntity.Career.ForbiddenMaterials) != 0)
                    prohibited = true;
            }

            if (prohibited)
            {
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(ForbiddenEquipmentTextId);
                if (tokens != null && tokens.Length > 0)
                {
                    DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, null);
                    messageBox.SetTextTokens(tokens);
                    messageBox.ClickAnywhereToClose = true;
                    messageBox.Show();
                }
                return;
            }

            List<DaggerfallUnityItem> unequippedList = playerEntity.ItemEquipTable.EquipItem(item);
            if (unequippedList != null)
            {
                foreach (DaggerfallUnityItem unequippedItem in unequippedList)
                    playerEntity.UpdateEquippedArmorValues(unequippedItem, false);
                playerEntity.UpdateEquippedArmorValues(item, true);
            }
        }

        public static void UnequipItem(PlayerEntity playerEntity, DaggerfallUnityItem item)
        {
            if (playerEntity.ItemEquipTable.UnequipItem(item.EquipSlot) != null)
                playerEntity.UpdateEquippedArmorValues(item, false);

            // DontCare matches DaggerfallInventoryWindow's own default preferredOrder - this is a no-op
            // unless the unequipped item can now merge into an existing stack elsewhere in inventory.
            playerEntity.Items.ReorderItem(item, ItemCollection.AddPosition.DontCare);
        }
    }
}
