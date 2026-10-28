using System;
using System.Collections.Generic;
using QoLChanges.Core;
using UnityEngine;

namespace QoLChanges.Features.PinnedRecipes
{
    [Serializable]
    public sealed class PinnedRecipesConfig : FeatureConfig
    {
        public const int MaxPinned = 3;
        public const int MinScale = 75;
        public const int MaxScale = 150;
        public const int ScaleStep = 25;

        /// <summary>Unique names of the pinned items, in the order they were pinned; at most <see cref="MaxPinned"/>.</summary>
        public List<string> recipes = new List<string>();

        /// <summary>A line with the ingredients under every recipe.</summary>
        public bool showIngredients = true;

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

            int steps = Mathf.RoundToInt((float)Mathf.Clamp(scale, MinScale, MaxScale) / ScaleStep);
            scale = steps * ScaleStep;
        }
    }
}
