using System;
using System.Collections.Generic;
using QoLChanges.Core;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.Features.DurabilityIndicator
{
    internal enum DurabilityGroup
    {
        None,
        Tools,
        Water,
        Food,
    }

    /// <summary>
    /// Writes the durability of an item into its slot, at the place of the amount text. The label is a copy of
    /// the slot's own amount text, so it uses the game's font and shadow; it is shown only while the amount
    /// text is empty (one item in the slot).
    /// </summary>
    internal static class DurabilityLabels
    {
        private const string LabelName = "QoLChanges_Durability";

        // Vanilla amount text of hotbar and inventory slots (70x70 cells): top-left corner, 24 px.
        // Equipment slots use a scaled copy of it, so the geometry is set explicitly for every slot.
        private const int FontSize = 24;
        private const int MinFontSize = 12;
        private const float MaxTextWidth = 64f;
        private static readonly Vector2 LabelPosition = new Vector2(2.85f, -1.75f);
        private static readonly Vector2 LabelSize = new Vector2(74.1f, 31.5f);

        private static readonly Dictionary<Item_Base, DurabilityGroup> Groups = new Dictionary<Item_Base, DurabilityGroup>();

        private static DurabilityIndicatorConfig _config;
        private static bool _errorLogged;

        public static void Activate(DurabilityIndicatorConfig config)
        {
            _config = config;
            _errorLogged = false;
            Groups.Clear();
            ForEachSceneSlot(Refresh);
        }

        public static void Deactivate()
        {
            _config = null;
            Groups.Clear();
            ForEachSceneSlot(RemoveLabel);
        }

        public static void Refresh(Slot slot)
        {
            if (_config == null || slot == null)
            {
                return;
            }

            // Runs inside the game's slot update; an exception here would break inventory handling.
            try
            {
                UpdateLabel(slot);
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(DurabilityIndicatorFeature.FeatureId + ": failed to update a slot.", e);
                }
            }
        }

        private static void UpdateLabel(Slot slot)
        {
            Text amount = slot.textComponent;
            if (amount == null)
            {
                return;
            }

            Text label = FindLabel(slot);
            if (!TryDescribe(slot, out string value, out Color color))
            {
                if (label != null)
                {
                    label.text = string.Empty;
                }

                return;
            }

            if (label == null)
            {
                label = CreateLabel(slot, amount);
            }

            label.color = color;
            label.fontSize = FontSize;
            label.text = value;

            float width = label.preferredWidth;
            if (width > MaxTextWidth)
            {
                label.fontSize = Mathf.Max(MinFontSize, Mathf.FloorToInt(FontSize * MaxTextWidth / width));
            }
        }

        private static bool TryDescribe(Slot slot, out string value, out Color color)
        {
            value = null;
            color = Color.white;

            if (slot.IsEmpty || slot.sliderComponent == null)
            {
                return false;
            }

            ItemInstance instance = slot.itemInstance;
            Item_Base item = instance.baseItem;
            if (item == null || item.MaxUses <= 1 || instance.Amount > 1)
            {
                return false;
            }

            DurabilityDisplayMode mode = GetMode(GetGroup(item));
            if (mode == DurabilityDisplayMode.Off)
            {
                return false;
            }

            int max = item.MaxUses;
            int uses = Mathf.Clamp(instance.Uses, 0, max);
            if (uses >= max && !_config.showAtFullDurability)
            {
                return false;
            }

            value = Format(mode, uses, max);
            Gradient gradient = item.BarGradient;
            color = gradient != null ? gradient.Evaluate((float)uses / max) : slot.textComponent.color;
            return true;
        }

        private static string Format(DurabilityDisplayMode mode, int uses, int max)
        {
            switch (mode)
            {
                case DurabilityDisplayMode.Remaining:
                    return uses.ToString();
                case DurabilityDisplayMode.RemainingOfMax:
                    return uses + "/" + max;
                default:
                    // Rounded down, so a used item never reads 100%, and a nearly broken one never reads 0%.
                    int percent = (int)((long)uses * 100 / max);
                    if (percent == 0 && uses > 0)
                    {
                        percent = 1;
                    }

                    return percent + "%";
            }
        }

        private static DurabilityDisplayMode GetMode(DurabilityGroup group)
        {
            switch (group)
            {
                case DurabilityGroup.Tools:
                    return _config.tools;
                case DurabilityGroup.Water:
                    return _config.water;
                case DurabilityGroup.Food:
                    return _config.food;
                default:
                    return DurabilityDisplayMode.Off;
            }
        }

        private static DurabilityGroup GetGroup(Item_Base item)
        {
            if (!Groups.TryGetValue(item, out DurabilityGroup group))
            {
                group = Classify(item);
                Groups[item] = group;
            }

            return group;
        }

        /// <summary>
        /// Groups items by their properties, so items added by other mods are sorted as well. Batteries are
        /// left out: their charge belongs to the hover tooltip of placed batteries.
        /// </summary>
        private static DurabilityGroup Classify(Item_Base item)
        {
            string name = item.UniqueName;
            if (!string.IsNullOrEmpty(name) && name.IndexOf("Battery", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DurabilityGroup.None;
            }

            ItemInstance_Consumeable consumable = item.settings_consumeable;
            if (consumable != null)
            {
                if (consumable.FoodForm == FoodForm.Fluid)
                {
                    return DurabilityGroup.Water;
                }

                if (consumable.FoodForm == FoodForm.Solid)
                {
                    return DurabilityGroup.Food;
                }
            }

            return DurabilityGroup.Tools;
        }

        private static Text FindLabel(Slot slot)
        {
            Transform label = slot.transform.Find(LabelName);
            return label != null ? label.GetComponent<Text>() : null;
        }

        private static Text CreateLabel(Slot slot, Text amount)
        {
            float amountScale = RelativeScale(amount.transform, slot.transform);
            GameObject copy = UnityEngine.Object.Instantiate(amount.gameObject, slot.transform, false);
            copy.name = LabelName;

            for (int i = copy.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);
            }

            foreach (MonoBehaviour behaviour in copy.GetComponents<MonoBehaviour>())
            {
                if (!(behaviour is Text) && !(behaviour is BaseMeshEffect))
                {
                    UnityEngine.Object.DestroyImmediate(behaviour);
                }
            }

            RectTransform rect = (RectTransform)copy.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = LabelSize;
            rect.anchoredPosition3D = new Vector3(LabelPosition.x, LabelPosition.y, 0f);

            // The shadow offset is in the text's own units; equipment slots scale their amount text down
            // (~0.137 with a 15 px offset), so keep the offset the same on screen at scale 1.
            foreach (Shadow shadow in copy.GetComponents<Shadow>())
            {
                shadow.effectDistance *= amountScale;
            }

            int index = amount.transform.parent == slot.transform ? amount.transform.GetSiblingIndex() + 1 : slot.transform.childCount - 1;
            copy.transform.SetSiblingIndex(index);

            Text label = copy.GetComponent<Text>();
            label.alignment = TextAnchor.UpperLeft;
            label.fontStyle = FontStyle.Normal;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            label.supportRichText = false;
            label.raycastTarget = false;

            copy.SetActive(true);
            return label;
        }

        /// <summary>Scale of <paramref name="child"/> as seen from <paramref name="ancestor"/>; 1 when unknown.</summary>
        private static float RelativeScale(Transform child, Transform ancestor)
        {
            float ancestorScale = ancestor.lossyScale.x;
            if (Mathf.Approximately(ancestorScale, 0f))
            {
                return 1f;
            }

            float scale = child.lossyScale.x / ancestorScale;
            return scale > 0f ? scale : 1f;
        }

        private static void RemoveLabel(Slot slot)
        {
            Transform label = slot.transform.Find(LabelName);
            if (label != null)
            {
                // Immediate, so that a re-enable in the same frame does not find the old label.
                UnityEngine.Object.DestroyImmediate(label.gameObject);
            }
        }

        /// <summary>FindObjectsOfTypeAll also returns prefab assets; they must never get a label.</summary>
        private static void ForEachSceneSlot(Action<Slot> action)
        {
            foreach (Slot slot in Resources.FindObjectsOfTypeAll<Slot>())
            {
                if (slot != null && slot.gameObject.scene.IsValid())
                {
                    action(slot);
                }
            }
        }
    }
}
