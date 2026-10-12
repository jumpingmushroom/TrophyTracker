using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyTracker.UI
{
    /// <summary>PLAN.md §7.3: click a Compendium trophy to pin or unpin it; a dot on the icon marks pinned ones.</summary>
    internal sealed class PinToggle : MonoBehaviour
    {
        private static readonly Color BadgeColor = new Color(1f, 0.62f, 0.18f, 1f);

        private string _id;
        private Image _badge;
        private TMPro.TMP_Text _description;
        private string _baseDescription;
        private bool _collected;

        public void Init(string id, bool collected)
        {
            _id = id;
            _collected = collected;
            Transform iconBkg = transform.Find("icon_bkg");
            Image icon = transform.Find("icon_bkg/icon").GetComponent<Image>();
            icon.raycastTarget = true;
            Transform description = transform.Find("description");
            _description = description != null ? description.GetComponent<TMPro.TMP_Text>() : null;
            _baseDescription = _description != null ? _description.text : "";

            RectTransform badge = UiUtil.Rect("TrophyTrackerPin", iconBkg != null ? iconBkg : icon.transform);
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 1f);
            badge.anchoredPosition = new Vector2(-2f, -2f);
            badge.sizeDelta = new Vector2(12f, 12f);
            _badge = badge.gameObject.AddComponent<Image>();
            _badge.sprite = Sprites.Dot;
            _badge.color = BadgeColor;
            _badge.raycastTarget = false;

            // Not ??: Unity overloads == for destroyed objects, and ?? bypasses it.
            Button button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = icon;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(Toggle);
            Refresh();
        }

        private void Toggle()
        {
            if (Keys.Held(PluginConfig.DragModifier.Value))
                return;
            PluginConfig.Pinned.Value = PinList.Toggle(PluginConfig.Pinned.Value, _id);
            Runtime.Dirty = true;
            Refresh();
        }

        private void Refresh()
        {
            bool pinned = PinList.Contains(PluginConfig.Pinned.Value, _id);
            _badge.gameObject.SetActive(pinned);
            // Collected trophies keep vanilla's lore text; only silhouettes carry our hint line.
            if (_description != null && !_collected)
                _description.text = pinned ? Runtime.Tr.Get("pinned") ?? _baseDescription : _baseDescription;
        }
    }
}
