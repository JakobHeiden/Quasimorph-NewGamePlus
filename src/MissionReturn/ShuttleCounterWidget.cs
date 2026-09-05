using System;
using MGSC;
using TMPro;
using UnityEngine;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Shows "SHUTTLES n/m" on the space HUD, by cloning a label already on it rather than by
    ///     registering a [UIView]: UI.LoadViews resolves a mod view's prefab through CustomResources
    ///     and throws when it is missing, which would mean shipping an asset bundle for one line of
    ///     text. Cloning also inherits the HUD's font asset and material, so it matches without styling.
    /// </summary>
    internal static class ShuttleCounterWidget
    {
        private const string ObjectName = "NGP_ShuttleCounter";
        private const string CounterFormat = "SHUTTLES {0}/{1}";

        private static TextMeshProUGUI _label;
        private static int _lastAvailable = int.MinValue;
        private static int _lastTotal = int.MinValue;

        internal static void Release()
        {
            if (_label != null)
                UnityEngine.Object.Destroy(_label.gameObject);

            _label = null;
            _lastAvailable = int.MinValue;
            _lastTotal = int.MinValue;
        }

        internal static void Refresh()
        {
            try
            {
                if (_label == null && !TryCreate())
                    return;

                var available = ReturnRegistry.AvailableShuttles;
                var total = Plugin.Config.AvailableShuttles;

                if (available == _lastAvailable && total == _lastTotal)
                    return;

                _lastAvailable = available;
                _lastTotal = total;
                _label.text = string.Format(CounterFormat, available, total);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError("Shuttle counter failed; hiding it for this session.");
                Plugin.Logger.LogException(ex);
                Release();
            }
        }

        private static bool TryCreate()
        {
            var hud = UI.Get<SpaceHudScreen>();
            if (hud == null)
                return false;

            var timePanel = hud.TimePanel;
            if (timePanel == null)
                return false;

            var template = timePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (template == null)
                return false;

            var clone = UnityEngine.Object.Instantiate(template.gameObject, hud.transform);
            clone.name = ObjectName;
            clone.SetActive(true);

            _label = clone.GetComponent<TextMeshProUGUI>();
            if (_label == null)
            {
                UnityEngine.Object.Destroy(clone);
                return false;
            }

            // Shed layout components inherited from the template's parent; this one is free-floating.
            foreach (var layout in clone.GetComponents<UnityEngine.UI.LayoutElement>())
                UnityEngine.Object.Destroy(layout);

            var rect = _label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(Plugin.Config.ShuttleCounterX, Plugin.Config.ShuttleCounterY);

            _label.alignment = TextAlignmentOptions.Top;
            _label.raycastTarget = false;
            _label.text = string.Empty;

            return true;
        }
    }
}
