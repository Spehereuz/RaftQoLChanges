using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.PinnedRecipes
{
    /// <summary>Screen corner of the pinned recipes.</summary>
    public enum PinnedRecipesPosition
    {
        /// <summary>Away from the crafting menu, which covers the bottom-left corner.</summary>
        TopRight,

        /// <summary>Above the health, hunger and thirst bars and the raft status.</summary>
        BottomLeft,
    }

    [Serializable]
    public sealed class PinnedRecipesConfig : FeatureConfig
    {
        public const int MaxPinned = 3;
        public const int MinScale = 75;
        public const int MaxScale = 150;
        public const int ScaleStep = 25;
        public const int OpacityStep = 10;

        /// <summary>Unique names of the pinned items, in the order they were pinned; at most <see cref="MaxPinned"/>.</summary>
        public List<string> recipes = new List<string>();

        [JsonConverter(typeof(StringEnumConverter))]
        public PinnedRecipesPosition position = PinnedRecipesPosition.TopRight;

        /// <summary>Items in every other storage of the world are shown next to those at hand.</summary>
        public bool countAllStorages = true;

        /// <summary>A line with the ingredients under every recipe.</summary>
        public bool showIngredients = true;

        /// <summary>A pinned recipe is unpinned after it has been crafted once.</summary>
        public bool unpinAfterCrafting;

        /// <summary>"PINNED RECIPES" above the recipes.</summary>
        public bool showTitle = true;

        /// <summary>Opacity of the panel behind the recipes in percent, 0-100 in steps of 10; 0 shows only the text.</summary>
        public int backgroundOpacity = 80;

        /// <summary>Size of the HUD lines in percent: 75, 100, 125 or 150.</summary>
        public int scale = 100;

        public override void Normalize()
        {
            List<string> unique = new List<string>();
            if (recipes != null)
            {
                foreach (string name in recipes)
                {
                    if (!string.IsNullOrEmpty(name) && !unique.Contains(name) && unique.Count < MaxPinned)
                    {
                        unique.Add(name);
                    }
                }
            }

            recipes = unique;

            if (!Enum.IsDefined(typeof(PinnedRecipesPosition), position))
            {
                position = PinnedRecipesPosition.TopRight;
            }

            int steps = Mathf.RoundToInt((float)Mathf.Clamp(scale, MinScale, MaxScale) / ScaleStep);
            scale = steps * ScaleStep;

            backgroundOpacity = Mathf.RoundToInt((float)Mathf.Clamp(backgroundOpacity, 0, 100) / OpacityStep) * OpacityStep;
        }
    }
}
