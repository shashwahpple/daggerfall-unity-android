using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Read-only Display 2 view of the player's identity, attributes and skills - nested under the
    /// Character tab's own sub tab strip alongside JournalPanel (see CharacterPanel). Deliberately never
    /// reads PlayerEntity.ReadyToLevelUp or touches leveling: DaggerfallCharacterSheetWindow's
    /// StatsRollout is the only place that flow should run, since it mutates PlayerEntity.Level/.Stats,
    /// and running it a second time from here risks double-processing a level-up if the player opens both
    /// windows at once.
    /// </summary>
    public class SkillsPanel : MonoBehaviour
    {
        const float RefreshInterval = 1f;

        /// <summary>
        /// Top edge of this panel's content area, as an anchor relative to its own transform. Defaults to
        /// the standard top-level convention (leaving 0.80-1.0 for a tab strip above), but CharacterPanel
        /// overrides this before Start() runs, since the nested Skills/Journal strip sits higher up than
        /// the outer strip would.
        /// </summary>
        public float ContentAnchorMaxY = 0.80f;

        RectTransform content;
        float refreshTimer;

        void Start()
        {
            BuildUI();
            Refresh();
        }

        void Update()
        {
            // Skills/attributes can change from gameplay (skill use, equipment, drains/boosts) while this
            // panel is visible - poll periodically rather than refreshing once.
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
            // Top of this local rect is reserved for the nested Skills/Journal tab strip - see
            // CharacterPanel.
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(1f, ContentAnchorMaxY);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(rootGO.transform, false);
            RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(20f, 20f);
            viewportRect.offsetMax = new Vector2(-20f, -20f);
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
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(24, 24, 24, 24);

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

            CreateHeaderText(string.Format("{0}   Lv.{1}   {2} {3}",
                playerEntity.Name, playerEntity.Level, playerEntity.RaceTemplate.Name, playerEntity.Career.Name));
            CreateBodyText(string.Format("HP {0}/{1}   Fatigue {2}/{3}   Gold {4}",
                playerEntity.CurrentHealth, playerEntity.MaxHealth,
                playerEntity.CurrentFatigue / DaggerfallEntity.FatigueMultiplier, playerEntity.MaxFatigue / DaggerfallEntity.FatigueMultiplier,
                playerEntity.GetGoldAmount()));
            CreateBodyText(string.Format("Encumbrance {0}/{1}", (int)playerEntity.CarriedWeight, playerEntity.MaxEncumbrance));

            CreateSpacer();
            CreateHeaderText("Attributes");
            for (int i = 0; i < DaggerfallStats.Count; i++)
                CreateStatRow((DFCareer.Stats)i, playerEntity);

            CreateSpacer();
            CreateSkillGroup("Primary Skills", playerEntity.GetPrimarySkills(), playerEntity, twoColumn: false);
            CreateSpacer();
            CreateSkillGroup("Major Skills", playerEntity.GetMajorSkills(), playerEntity, twoColumn: false);
            CreateSpacer();
            CreateSkillGroup("Minor Skills", playerEntity.GetMinorSkills(), playerEntity, twoColumn: false);
            CreateSpacer();
            // Misc covers everything not in the other three groups - many more entries than those, so
            // it gets two columns, same distinction DaggerfallCharacterSheetWindow.ShowSkillsDialog makes.
            CreateSkillGroup("Miscellaneous Skills", playerEntity.GetMiscSkills(), playerEntity, twoColumn: true);
        }

        void CreateSkillGroup(string header, List<DFCareer.Skills> skills, PlayerEntity playerEntity, bool twoColumn)
        {
            CreateHeaderText(header);

            if (!twoColumn)
            {
                foreach (DFCareer.Skills skill in skills)
                    CreateBodyText(content, FormatSkill(skill, playerEntity));
                return;
            }

            GameObject columnsGO = new GameObject("Columns_" + header);
            columnsGO.transform.SetParent(content, false);
            columnsGO.AddComponent<RectTransform>();

            HorizontalLayoutGroup columnsLayout = columnsGO.AddComponent<HorizontalLayoutGroup>();
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = false;
            columnsLayout.spacing = 20f;

            ContentSizeFitter columnsFitter = columnsGO.AddComponent<ContentSizeFitter>();
            columnsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform leftColumn = CreateColumn(columnsGO.transform);
            RectTransform rightColumn = CreateColumn(columnsGO.transform);

            bool useRight = false;
            foreach (DFCareer.Skills skill in skills)
            {
                CreateBodyText(useRight ? rightColumn : leftColumn, FormatSkill(skill, playerEntity));
                useRight = !useRight;
            }
        }

        static string FormatSkill(DFCareer.Skills skill, PlayerEntity playerEntity)
        {
            return string.Format("{0}   {1}", DaggerfallUnity.Instance.TextProvider.GetSkillName(skill), playerEntity.Skills.GetLiveSkillValue(skill));
        }

        static RectTransform CreateColumn(Transform parent)
        {
            GameObject columnGO = new GameObject("Column");
            columnGO.transform.SetParent(parent, false);
            RectTransform columnRect = columnGO.AddComponent<RectTransform>();

            VerticalLayoutGroup layout = columnGO.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;

            ContentSizeFitter fitter = columnGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return columnRect;
        }

        void CreateStatRow(DFCareer.Stats stat, PlayerEntity playerEntity)
        {
            int liveValue = playerEntity.Stats.GetLiveStatValue((int)stat);
            int permanentValue = playerEntity.Stats.GetPermanentStatValue((int)stat);

            TextMeshProUGUI label = CreateBodyText(string.Format("{0}   {1}", DaggerfallUnity.Instance.TextProvider.GetStatName(stat), liveValue));
            if (liveValue < permanentValue)
                label.color = DaggerfallUI.DaggerfallUnityStatDrainedTextColor;
            else if (liveValue > permanentValue)
                label.color = DaggerfallUI.DaggerfallUnityStatIncreasedTextColor;
        }

        void CreateHeaderText(string text)
        {
            TextMeshProUGUI label = CreateRow(content, text, 32f);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.85f, 0.7f, 0.15f, 1f);
        }

        TextMeshProUGUI CreateBodyText(string text)
        {
            return CreateRow(content, text, 26f);
        }

        static TextMeshProUGUI CreateBodyText(Transform parent, string text)
        {
            return CreateRow(parent, text, 26f);
        }

        void CreateSpacer()
        {
            GameObject spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(content, false);
            LayoutElement layoutElement = spacerGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 12f;
        }

        static TextMeshProUGUI CreateRow(Transform parent, string text, float fontSize)
        {
            GameObject rowGO = new GameObject("Row");
            rowGO.transform.SetParent(parent, false);

            LayoutElement layoutElement = rowGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 34f;

            TextMeshProUGUI label = rowGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = fontSize;
            label.color = Color.white;
            // Text inset via margin, not the RectTransform's own anchors/offsets - this row is directly
            // controlled by its parent VerticalLayoutGroup (childControlWidth/Height), which would fight
            // over and clobber any manual anchor/offset placed on the same rect.
            label.margin = new Vector4(32f, 0f, 32f, 0f);

            return label;
        }
    }
}
