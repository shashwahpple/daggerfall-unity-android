using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Persistent Display 2 visual paper doll, to the left of EquipmentPanel's item list. Reuses DFU's
    /// actual PaperDollRenderer (Assets/Scripts/Game/Utility/PaperDollRenderer.cs) directly - a plain,
    /// non-singleton C# class that composites body/item sprites into its own Texture2D - rather than
    /// recreating the doll's visuals in uGUI or hosting DFU's native PaperDoll/BaseScreenComponent UI (with
    /// its Input.mousePosition-based click pipeline that, like the travel map, isn't driven by Display 2's
    /// touch input). Owning a second independent PaperDollRenderer instance is safe: confirmed each
    /// instance owns its own RenderTexture/Texture2D/Material/item-layout list, and its Refresh() saves and
    /// restores RenderTexture.active around the only Unity global it touches.
    ///
    /// Displays PaperDollRenderer.PaperDollTexture (a plain Texture2D, produced by its own internal
    /// RenderTexture readback) directly on a uGUI RawImage - no UserInterfaceRenderTarget/OnGUI bridging
    /// needed. Taps are handled the same way as every other Display 2 panel, via
    /// SecondScreenTouchDispatcher's GraphicRaycaster-based dispatch to IPointerClickHandler - converted to
    /// PaperDollRenderer's own native (unscaled, 110x184) coordinate space and passed straight to its
    /// public GetEquipIndex(x, y) hit-test, sidestepping DFU's native input pipeline entirely.
    ///
    /// The original Paper Doll has no drag-to-equip: tapping the doll only ever unequips whatever item is
    /// drawn under that pixel (DaggerfallInventoryWindow.PaperDoll_OnLeftMouseClick calls UnequipItem, never
    /// EquipItem) - equipping is inventory-list-only, which is what EquipmentPanel already provides.
    /// </summary>
    public class PaperDollPanel : MonoBehaviour, IPointerClickHandler
    {
        const float RefreshInterval = 1f;

        // Render scale for the composited texture. DaggerfallUI.Instance.PaperDollRenderer (the main
        // window's own instance) uses 8; this panel's on-screen doll is considerably smaller, so a lower
        // scale avoids wasting fill-rate on detail that can't be seen.
        const float RenderScale = 4f;

        PaperDollRenderer paperDollRenderer;
        RawImage rawImage;
        RectTransform rawImageRect;
        Camera canvasCamera;
        float refreshTimer;

        void Start()
        {
            paperDollRenderer = new PaperDollRenderer(RenderScale);
            BuildUI();
            Refresh();
        }

        void Update()
        {
            // Same reasoning as EquipmentPanel: inventory/equipment can change from the main
            // DaggerfallInventoryWindow while this panel is visible, so poll periodically to stay in sync.
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
            // Right of this width is reserved for EquipmentPanel's item list - see SecondScreenManager.
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0.35f, 0.88f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject dollGO = new GameObject("DollImage");
            dollGO.transform.SetParent(rootGO.transform, false);
            rawImage = dollGO.AddComponent<RawImage>();
            rawImageRect = rawImage.rectTransform;
            rawImageRect.anchorMin = Vector2.zero;
            rawImageRect.anchorMax = Vector2.one;
            rawImageRect.offsetMin = new Vector2(10f, 10f);
            rawImageRect.offsetMax = new Vector2(-10f, -10f);

            // The doll's native 110x184 aspect ratio doesn't match this panel's region - fit within it
            // rather than stretching, so tap-to-native-coordinate math matches what's actually drawn.
            AspectRatioFitter fitter = dollGO.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = PaperDollRenderer.paperDollWidth / (float)PaperDollRenderer.paperDollHeight;

            canvasCamera = GetComponentInParent<Canvas>().worldCamera;
        }

        void Refresh()
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity == null)
                return;

            paperDollRenderer.Refresh(PaperDollRenderer.LayerFlags.All, playerEntity);
            rawImage.texture = paperDollRenderer.PaperDollTexture;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            if (playerEntity == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, canvasCamera, out Vector2 localPoint))
                return;

            // rawImageRect.rect is pivot-relative already, so this works regardless of the RawImage's own
            // pivot - normalize the tap into the 0-1 range of the fitted image, top-left origin to match
            // GetEquipIndex's own coordinate space.
            Rect rect = rawImageRect.rect;
            float u = (localPoint.x - rect.xMin) / rect.width;
            float v = (rect.yMax - localPoint.y) / rect.height;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return; // Tapped the letterboxed margin around the doll, not the doll image itself

            int nativeX = (int)(u * PaperDollRenderer.paperDollWidth);
            int nativeY = (int)(v * PaperDollRenderer.paperDollHeight);

            byte slot = paperDollRenderer.GetEquipIndex(nativeX, nativeY);
            if (slot == 0xff)
                return;

            DaggerfallUnityItem item = playerEntity.ItemEquipTable.GetItem((EquipSlots)slot);
            if (item == null)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            EquipmentActions.UnequipItem(playerEntity, item);
            Refresh();
        }
    }
}
