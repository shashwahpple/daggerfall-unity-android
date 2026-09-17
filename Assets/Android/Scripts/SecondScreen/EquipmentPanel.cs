using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Persistent Display 2 scrollable list of the player's equippable inventory, tap to equip/unequip.
    /// Equip/unequip logic is a faithful copy of DaggerfallInventoryWindow.EquipItem/UnequipItem
    /// (Assets/Scripts/Game/UserInterfaceWindows/DaggerfallInventoryWindow.cs:1322-1403), which are
    /// protected instance methods and can't be called directly - reproduced here so this panel can never
    /// equip a broken or forbidden item, or leave armor values out of sync, in a way the main inventory
    /// window wouldn't allow. Any message boxes this raises (broken item / forbidden equipment) are DFU's
    /// own native window-stack windows, so they render on Display 1, not this panel's screen.
    /// </summary>
    public class EquipmentPanel : MonoBehaviour
    {
        const int ItemBrokenTextId = 29;
        const int ForbiddenEquipmentTextId = 1068;

        static readonly Color EquippedColor = new Color(0.2f, 0.45f, 0.2f, 0.9f);
        static readonly Color UnequippedColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        const float RefreshInterval = 1f;

        RectTransform content;
        float refreshTimer;

        void Start()
        {
            BuildUI();
            Refresh();
        }

        void Update()
        {
            // PlayerEntity may not exist yet the moment this panel is created (the game scene loads
            // before character creation finishes), and inventory can also change from the main
            // DaggerfallInventoryWindow while this panel is visible - poll periodically rather than
            // refreshing once, so the list actually populates and stays in sync.
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= RefreshInterval)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }

        void BuildUI()
        {
            // A root Canvas always fills the entire screen regardless of its own RectTransform's anchors
            // - Unity only honors anchors on children of a canvas. So the actual size-restricted panel
            // content has to live on a child RectTransform, not directly on this Canvas GameObject.
            GameObject rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform panelRect = rootGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0.88f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(rootGO.transform, false);
            RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, 10f);
            viewportRect.offsetMax = new Vector2(-10f, -10f);
            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f); // Mask requires a Graphic to clip against
            Mask mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            content = contentGO.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            VerticalLayoutGroup layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = rootGO.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        void Refresh()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity == null)
                return;

            // Snapshot before iterating: EquipItem can call SplitStack, which mutates playerEntity.Items
            // in place, so we never want to be iterating the live collection during a click callback.
            List<DaggerfallUnityItem> snapshot = new List<DaggerfallUnityItem>();
            for (int i = 0; i < playerEntity.Items.Count; i++)
                snapshot.Add(playerEntity.Items.GetItem(i));

            foreach (DaggerfallUnityItem item in snapshot)
            {
                if (playerEntity.ItemEquipTable.GetEquipSlot(item) == EquipSlots.None)
                    continue; // Not an equippable item type (gold, potions, ingredients, etc.)

                CreateRow(item, playerEntity.ItemEquipTable.IsEquipped(item));
            }
        }

        void CreateRow(DaggerfallUnityItem item, bool isEquipped)
        {
            GameObject rowGO = new GameObject("Row_" + item.LongName);
            rowGO.transform.SetParent(content, false);

            LayoutElement layoutElement = rowGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 70f;

            Image rowImage = rowGO.AddComponent<Image>();
            rowImage.color = isEquipped ? EquippedColor : UnequippedColor;

            Button button = rowGO.AddComponent<Button>();
            button.onClick.AddListener(() => OnItemRowClicked(item));

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = isEquipped ? item.LongName + "  [Equipped]" : item.LongName;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 32;
            label.color = Color.white;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(15f, 0f);
            labelRect.offsetMax = new Vector2(-15f, 0f);
        }

        void OnItemRowClicked(DaggerfallUnityItem item)
        {
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity == null)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            if (playerEntity.ItemEquipTable.IsEquipped(item))
                UnequipItem(playerEntity, item);
            else
                EquipItem(playerEntity, item);

            Refresh();
        }

        // Faithful copy of DaggerfallInventoryWindow.EquipItem (lines 1322-1393), minus the call to that
        // window's own Refresh() at the end - this panel calls its own Refresh() from OnItemRowClicked.
        void EquipItem(PlayerEntity playerEntity, DaggerfallUnityItem item)
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

        // Faithful copy of DaggerfallInventoryWindow.UnequipItem (lines 1395-1403), minus refreshPaperDoll
        // (this panel has no paper doll) and that window's own Refresh() call.
        void UnequipItem(PlayerEntity playerEntity, DaggerfallUnityItem item)
        {
            if (playerEntity.ItemEquipTable.UnequipItem(item.EquipSlot) != null)
                playerEntity.UpdateEquippedArmorValues(item, false);

            // DontCare matches DaggerfallInventoryWindow's own default preferredOrder - this is a no-op
            // unless the unequipped item can now merge into an existing stack elsewhere in inventory.
            playerEntity.Items.ReorderItem(item, ItemCollection.AddPosition.DontCare);
        }
    }
}
