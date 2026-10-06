using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Display 2 "Character" tab: nests a second ContentTabStripPanel inside this tab's own content area,
    /// switching between SkillsPanel (read-only attributes/skills) and JournalPanel (active quest log).
    /// ContentTabStripPanel needed no changes to support this - it only ever assumed it owns the top slice
    /// of whatever RectTransform it's attached to, so a second instance nested one level deeper behaves
    /// the same way at a smaller scale, and both sub-panels share this tab's single Canvas/GraphicRaycaster
    /// (SecondScreenTouchDispatcher's one-time raycaster scan doesn't need to know about the nesting).
    ///
    /// Builds lazily in Start(), same as every other second-screen page - the outer ContentTabStripPanel
    /// (in SecondScreenManager) creates every top-level tab up front and immediately deactivates
    /// non-default ones, so this only runs once the player actually switches to this tab.
    /// </summary>
    public class CharacterPanel : MonoBehaviour
    {
        void Start()
        {
            GameObject rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform rootRect = rootGO.AddComponent<RectTransform>();
            // Vertical space above this is reserved for the outer ContentTabStripPanel and
            // InteractModeStripPanel - see SecondScreenManager.
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = new Vector2(1f, 0.80f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Nested strip sits flush against the outer ContentTabStripPanel's bottom edge (0.80) instead
            // of reusing the outer strip's own 0.80-0.88 default, which would leave the 0.88-1.0 sliver
            // above it empty - there's no InteractModeStripPanel-equivalent at this nested level to fill
            // it. Sub-pages get the matching boundary so their content fills exactly up to the strip.
            const float SubTabStripAnchorMinY = 0.90f;
            const float SubTabStripAnchorMaxY = 0.98f;

            SkillsPanel skills = CreateSubPage<SkillsPanel>(rootGO.transform, "SkillsPanel");
            skills.ContentAnchorMaxY = SubTabStripAnchorMinY;
            JournalPanel journal = CreateSubPage<JournalPanel>(rootGO.transform, "JournalPanel");
            journal.ContentAnchorMaxY = SubTabStripAnchorMinY;

            // Created last, after both sub-pages exist, same ordering rule SecondScreenManager itself
            // follows for the outer strip.
            ContentTabStripPanel subTabStrip = rootGO.AddComponent<ContentTabStripPanel>();
            subTabStrip.AnchorMinY = SubTabStripAnchorMinY;
            subTabStrip.AnchorMaxY = SubTabStripAnchorMaxY;
            subTabStrip.AddTab("Skills", skills.gameObject);
            subTabStrip.AddTab("Journal", journal.gameObject);
            // Skills has no real-window equivalent, so it gets no second-tap action (same as Home/Map at
            // the outer strip level - see SecondScreenManager).
            subTabStrip.SetSecondTapAction("Journal", OnJournalSecondTap);
            subTabStrip.BuildAndShowFirstTab();
        }

        void OnJournalSecondTap()
        {
            // Toggle like HomePanel's Local Map button: close the real window if it's already the one on
            // top. Type + DisplayMode check since DaggerfallQuestJournalWindow is also reused for the
            // Notebook (see HomePanel's Notes button) - a plain type check alone can't tell them apart.
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallQuestJournalWindow journalWindow &&
                journalWindow.DisplayMode == DaggerfallQuestJournalWindow.JournalDisplay.ActiveQuests)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.UIManager.PopWindow();
                return;
            }

            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenQuestJournalWindow);
        }

        static T CreateSubPage<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<T>();
        }
    }
}
