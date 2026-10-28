using UnityEngine;

namespace QoLChanges.UI
{
    /// <summary>
    /// Sprites of HUD panels. The rounded panel and its soft shadow are drawn by the mod at runtime; the slot background
    /// and the divider are the game's own sprites from the crafting menu, found among the loaded sprites by name.
    /// </summary>
    internal static class HudSprites
    {
        // Wood brown of the game's menus and a lighter rim.
        private static readonly Color32 PanelFill = new Color32(92, 63, 38, 255);
        private static readonly Color32 PanelRim = new Color32(150, 108, 64, 255);

        private const int TextureSize = 32;
        private const float PanelRadius = 10f;
        private const float PanelRimWidth = 2f;
        private const float ShadowSoftness = 6f;
        private const float SliceBorder = 12f;

        private static Sprite _panel;
        private static Sprite _shadow;
        private static Sprite _slot;
        private static Sprite _divider;
        private static bool _gameSpritesSearched;

        /// <summary>Rounded wooden panel with a lighter rim, 9-sliced with a 12 px border.</summary>
        public static Sprite Panel
        {
            get
            {
                if (_panel == null)
                {
                    _panel = CreateSprite("QoLChanges_HudPanel", PanelPixel);
                }

                return _panel;
            }
        }

        /// <summary>Black rounded rectangle with soft edges, 9-sliced with a 12 px border.</summary>
        public static Sprite Shadow
        {
            get
            {
                if (_shadow == null)
                {
                    _shadow = CreateSprite("QoLChanges_HudShadow", ShadowPixel);
                }

                return _shadow;
            }
        }

        /// <summary>The slot background behind the selected recipe in the crafting menu; null when it is not loaded.</summary>
        public static Sprite Slot
        {
            get
            {
                FindGameSprites();
                return _slot;
            }
        }

        /// <summary>The divider under the recipe name in the crafting menu; null when it is not loaded.</summary>
        public static Sprite Divider
        {
            get
            {
                FindGameSprites();
                return _divider;
            }
        }

        /// <summary>Destroys the sprites drawn by the mod; the game's sprites are only forgotten.</summary>
        public static void Release()
        {
            DestroySprite(_panel);
            DestroySprite(_shadow);
            _panel = null;
            _shadow = null;
            _slot = null;
            _divider = null;
            _gameSpritesSearched = false;
        }

        private static void FindGameSprites()
        {
            if (_gameSpritesSearched)
            {
                return;
            }

            _gameSpritesSearched = true;
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite == null)
                {
                    continue;
                }

                if (_slot == null && sprite.name == "ItemBG_wo_Frame")
                {
                    _slot = sprite;
                }
                else if (_divider == null && sprite.name == "DividerSmall")
                {
                    _divider = sprite;
                }
            }
        }

        private delegate Color32 PixelFunction(float distance);

        /// <summary>
        /// A square texture filled by <paramref name="pixel"/> from the signed distance of every pixel centre to the edge
        /// of a rounded square (negative inside).
        /// </summary>
        private static Sprite CreateSprite(string name, PixelFunction pixel)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    pixels[y * TextureSize + x] = pixel(DistanceToEdge(x + 0.5f, y + 0.5f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(SliceBorder, SliceBorder, SliceBorder, SliceBorder));
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>Signed distance to the edge of the texture square with corners rounded by <see cref="PanelRadius"/>.</summary>
        private static float DistanceToEdge(float x, float y)
        {
            float half = TextureSize * 0.5f;
            float qx = Mathf.Abs(x - half) - (half - PanelRadius);
            float qy = Mathf.Abs(y - half) - (half - PanelRadius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - PanelRadius;
        }

        private static Color32 PanelPixel(float distance)
        {
            // Anti-aliased outer edge; the rim blends into the fill over one pixel.
            float coverage = Mathf.Clamp01(0.5f - distance);
            float rim = Mathf.Clamp01(distance + PanelRimWidth + 0.5f);
            Color32 color = Color32.Lerp(PanelFill, PanelRim, rim);
            color.a = (byte)Mathf.RoundToInt(255f * coverage);
            return color;
        }

        private static Color32 ShadowPixel(float distance)
        {
            // Solid well inside, fading out towards the edge of the texture.
            float alpha = Mathf.Clamp01(-distance / ShadowSoftness);
            alpha = alpha * alpha * (3f - 2f * alpha);
            return new Color32(0, 0, 0, (byte)Mathf.RoundToInt(255f * alpha));
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
