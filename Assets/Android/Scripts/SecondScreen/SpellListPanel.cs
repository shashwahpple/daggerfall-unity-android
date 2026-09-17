using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Persistent Display 2 scrollable list of the player's known spells, tap to ready one. Structurally
    /// mirrors EquipmentPanel, but needs none of that panel's EquipmentActions-style extraction: unlike
    /// DaggerfallInventoryWindow's EquipItem/UnequipItem (protected instance methods, forcing a
    /// reimplementation), EntityEffectManager.SetReadySpell(EntityEffectBundle, bool) is already public on
    /// GameManager.Instance.PlayerEffectManager - a single shared per-player instance, not per-window
    /// state - so this panel calls the exact same method DaggerfallSpellBookWindow's own spell selection
    /// does. That also means this panel, DaggerfallSpellBookWindow, and the gamepad's Recast Spell action
    /// (which reads EntityEffectManager.LastSpell) all read/write the same shared ready-spell state and
    /// can't drift out of sync with each other.
    /// </summary>
    public class SpellListPanel : MonoBehaviour
    {
        static readonly Color ReadyColor = new Color(0.2f, 0.45f, 0.2f, 0.9f);
        static readonly Color IdleColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);

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
            // Spell list can change from the main DaggerfallSpellBookWindow (buy/rename/reorder/delete)
            // while this panel is visible, and readied spell can change from gameplay (cast, recast,
            // abort) - poll periodically rather than refreshing once, so this stays in sync with both.
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
            // Vertical space above this is reserved for ContentTabStripPanel and InteractModeStripPanel -
            // see SecondScreenManager.
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(1f, 0.80f);
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

            EffectBundleSettings[] spells = playerEntity.GetSpells();
            if (spells == null)
                return;

            // Highlight whichever spell is readied, or - once it's actually been cast and readySpell
            // clears - whichever was cast last, since that's what Recast Spell re-readies. Without the
            // LastSpell fallback the highlight would disappear the instant a cast animation completes,
            // even though that spell is still the one gameplay treats as "current" for recasting.
            EntityEffectManager effectManager = GameManager.Instance.PlayerEffectManager;
            EntityEffectBundle current = effectManager == null ? null
                : effectManager.HasReadySpell ? effectManager.ReadySpell
                : effectManager.LastSpell;
            string currentName = current != null ? current.Settings.Name : null;

            foreach (EffectBundleSettings spell in spells)
                CreateRow(spell, spell.Name == currentName);
        }

        void CreateRow(EffectBundleSettings spell, bool isCurrent)
        {
            (int _, int spellPointCost) = FormulaHelper.CalculateTotalEffectCosts(spell.Effects, spell.TargetType, null, spell.MinimumCastingCost);
            if (spell.Tag == PlayerEntity.lycanthropySpellTag)
                spellPointCost = 0; // Lycanthropy is a free spell, even though it shows a cost in classic

            GameObject rowGO = new GameObject("Row_" + spell.Name);
            rowGO.transform.SetParent(content, false);

            LayoutElement layoutElement = rowGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 70f;

            Image rowImage = rowGO.AddComponent<Image>();
            rowImage.color = isCurrent ? ReadyColor : IdleColor;

            Button button = rowGO.AddComponent<Button>();
            button.onClick.AddListener(() => OnSpellRowClicked(spell));

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = isCurrent ? string.Format("{0} - {1}  [Current]", spell.Name, spellPointCost) : string.Format("{0} - {1}", spell.Name, spellPointCost);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 28;
            label.color = Color.white;
            // Single line with an ellipsis rather than wrapping - a wrapped second line would otherwise
            // just get vertically centered under the first, reading as if the name were cut off.
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(50f, 0f);
            labelRect.offsetMax = new Vector2(-30f, 0f);
        }

        void OnSpellRowClicked(EffectBundleSettings spell)
        {
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            EntityEffectManager effectManager = GameManager.Instance.PlayerEffectManager;
            if (effectManager == null)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            // Faithful copy of DaggerfallSpellBookWindow.SpellsListBox_OnUseSelectedItem - lycanthropes
            // cast for free, matching classic.
            bool noSpellPointCost = spell.Tag == PlayerEntity.lycanthropySpellTag;
            effectManager.SetReadySpell(new EntityEffectBundle(spell, GameManager.Instance.PlayerEntityBehaviour), noSpellPointCost);

            Refresh();
        }
    }
}
