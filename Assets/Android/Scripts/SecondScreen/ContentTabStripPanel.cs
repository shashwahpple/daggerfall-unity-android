using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Persistent Display 2 strip that switches which main content page is shown (Inventory, Map, Spell
    /// List, ...), sitting between the active content page and InteractModeStripPanel. Distinct from
    /// InteractModeStripPanel, which switches PlayerActivate's interaction mode rather than swapping UI.
    ///
    /// SecondScreenManager creates every page up front (so SecondScreenTouchDispatcher's one-time
    /// GraphicRaycaster scan in its own Awake() - which runs after this - picks up all of them, including
    /// pages hidden at startup) and registers each with AddTab(), then calls BuildAndShowFirstTab() once.
    /// Non-default tabs are deactivated immediately, so their panels' own Start() (and UI construction)
    /// doesn't run until the player actually switches to that tab.
    /// </summary>
    public class ContentTabStripPanel : MonoBehaviour
    {
        static readonly Color IdleColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        static readonly Color ActiveColor = new Color(0.85f, 0.7f, 0.15f, 1f);

        class Tab
        {
            public string Label;
            public GameObject[] PageRoots;
            public Image ButtonImage;
            public Action OnSecondTap;
        }

        readonly List<Tab> tabs = new List<Tab>();
        int selectedIndex = -1;

        /// <summary>
        /// Vertical band this strip occupies, as anchors relative to whatever RectTransform it's attached
        /// to. Defaults match the outer strip's placement in SecondScreenManager (leaving 0.88-1.0 above
        /// for InteractModeStripPanel). Set before BuildAndShowFirstTab() to nest a second instance
        /// flush against a different edge - see CharacterPanel, which has no InteractModeStripPanel
        /// equivalent above it and would otherwise leave that space empty.
        /// </summary>
        public float AnchorMinY = 0.80f;
        public float AnchorMaxY = 0.88f;

        /// <summary>
        /// Registers a tab. pageRoots are the top-level GameObject(s) shown while this tab is selected and
        /// hidden otherwise - Inventory needs two (PaperDollPanel + EquipmentPanel), most tabs need one.
        /// Call before BuildAndShowFirstTab().
        /// </summary>
        public void AddTab(string label, params GameObject[] pageRoots)
        {
            tabs.Add(new Tab { Label = label, PageRoots = pageRoots });
        }

        /// <summary>
        /// Registers a callback fired when the player taps an already-selected tab's button again (a
        /// no-op otherwise). Used to open the real Display 1 window for a tab on a second tap - see
        /// SecondScreenManager and CharacterPanel. Call after the matching AddTab().
        /// </summary>
        public void SetSecondTapAction(string label, Action onSecondTap)
        {
            Tab tab = tabs.Find(t => t.Label == label);
            if (tab != null)
                tab.OnSecondTap = onSecondTap;
        }

        /// <summary>
        /// Builds the tab button row and selects a tab, hiding the rest. Call once, after every AddTab()
        /// call. Selects the tab matching defaultLabel if given and found, otherwise index 0.
        /// </summary>
        public void BuildAndShowFirstTab(string defaultLabel = null)
        {
            BuildUI();

            int defaultIndex = 0;
            if (defaultLabel != null)
            {
                int found = tabs.FindIndex(t => t.Label == defaultLabel);
                if (found >= 0)
                    defaultIndex = found;
            }

            SelectTab(defaultIndex, playSound: false);
        }

        void BuildUI()
        {
            // A root Canvas always fills the entire screen regardless of its own RectTransform's anchors
            // - Unity only honors anchors on children of a canvas. So the actual size-restricted panel
            // content has to live on a child RectTransform, not directly on this Canvas GameObject.
            GameObject rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform panelRect = rootGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, AnchorMinY);
            panelRect.anchorMax = new Vector2(1f, AnchorMaxY);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            HorizontalLayoutGroup layout = rootGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i; // Capture by value for the click listener below
                Tab tab = tabs[i];

                GameObject buttonGO = new GameObject("Tab_" + tab.Label);
                buttonGO.transform.SetParent(rootGO.transform, false);

                Image image = buttonGO.AddComponent<Image>();
                image.color = IdleColor;
                tab.ButtonImage = image;

                Button button = buttonGO.AddComponent<Button>();
                button.onClick.AddListener(() => OnTabButtonClicked(index));

                GameObject labelGO = new GameObject("Label");
                labelGO.transform.SetParent(buttonGO.transform, false);
                TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
                label.text = tab.Label;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 28;
                label.color = Color.white;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
        }

        void OnTabButtonClicked(int index)
        {
            if (index == selectedIndex)
            {
                tabs[index].OnSecondTap?.Invoke();
                return;
            }

            SelectTab(index);
        }

        void SelectTab(int index, bool playSound = true)
        {
            if (index == selectedIndex)
                return;

            selectedIndex = index;
            for (int i = 0; i < tabs.Count; i++)
            {
                bool active = i == index;
                tabs[i].ButtonImage.color = active ? ActiveColor : IdleColor;
                foreach (GameObject root in tabs[i].PageRoots)
                    root.SetActive(active);
            }

            if (playSound)
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }
    }
}
