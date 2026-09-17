using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Persistent Display 2 scrollable list of the player's equippable inventory, tap to equip/unequip.
    /// Sits to the right of PaperDollPanel, which shows the equipped result visually and offers a second,
    /// faster way to unequip (tap the doll). Equip/unequip logic lives in EquipmentActions, shared by both
    /// panels so they can never disagree about what's equippable or leave armor values out of sync in a
    /// way the main inventory window wouldn't allow.
    /// </summary>
    public class EquipmentPanel : MonoBehaviour
    {
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
            // Left of this width is reserved for PaperDollPanel - see SecondScreenManager.
            panelRect.anchorMin = new Vector2(0.35f, 0f);
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
                EquipmentActions.UnequipItem(playerEntity, item);
            else
                EquipmentActions.EquipItem(playerEntity, item);

            Refresh();
        }
    }
}
