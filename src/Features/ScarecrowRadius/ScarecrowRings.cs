using System;
using System.Collections.Generic;
using QoLChanges.Core;
using QoLChanges.GameState;
using QoLChanges.UI;
using UnityEngine;

namespace QoLChanges.Features.ScarecrowRadius
{
    /// <summary>
    /// Shows the zones of all placed scarecrows while the local player holds the hammer or a scarecrow and no menu is
    /// open. Basic scarecrows, which break, are yellow; advanced ones green.
    /// </summary>
    internal sealed class ScarecrowRings : MonoBehaviour
    {
        private const string HostName = "QoLChanges_ScarecrowRadius";
        private const string HammerItem = "Hammer";

        // Transparent, coloured by vertex, hidden behind blocks; always included in Unity builds.
        private const string LineShader = "Sprites/Default";

        private static readonly Color BasicColour = StatusText.Progress;
        private static readonly Color AdvancedColour = StatusText.Ready;

        private static ScarecrowRings _instance;

        private readonly Dictionary<Scarecrow, ScarecrowRing> _rings = new Dictionary<Scarecrow, ScarecrowRing>();
        private readonly List<Scarecrow> _removed = new List<Scarecrow>();
        private readonly Dictionary<Item_Base, bool> _scarecrowItems = new Dictionary<Item_Base, bool>();
        private ScarecrowRadiusConfig _config;
        private Material _material;
        private bool _errorLogged;

        public static void Activate(ScarecrowRadiusConfig config)
        {
            Deactivate();

            Shader shader = Shader.Find(LineShader);
            if (shader == null)
            {
                throw new InvalidOperationException("Shader '" + LineShader + "' was not found.");
            }

            GameObject host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<ScarecrowRings>();
            _instance._config = config;
            _instance._material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public static void Deactivate()
        {
            if (_instance != null)
            {
                // Immediate, so that rings activated again in the same frame (settings reapplied) are the only ones.
                DestroyImmediate(_instance.gameObject);
            }

            _instance = null;
        }

        private void OnDestroy()
        {
            _rings.Clear();
            Scarecrows.Clear();
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        // After the raft and the player have moved in this frame, so the rings do not lag behind the scarecrows.
        private void LateUpdate()
        {
            try
            {
                if (IsHoldingTool())
                {
                    ShowRings();
                }
                else
                {
                    HideRings();
                }
            }
            catch (Exception e)
            {
                HideRings();
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    ModLog.Error(ScarecrowRadiusFeature.FeatureId + ": failed to update the scarecrow zones.", e);
                }
            }
        }

        private void ShowRings()
        {
            foreach (ScarecrowRing ring in _rings.Values)
            {
                ring.Used = false;
            }

            float radius = Scarecrows.Radius;
            foreach (Scarecrow scarecrow in Scarecrows.Placed())
            {
                // A destroyed scarecrow protects nothing and is removed by the game a few seconds later.
                if (scarecrow != null && !scarecrow.Destroyed)
                {
                    Color colour = scarecrow.invurnerable ? AdvancedColour : BasicColour;
                    GetRing(scarecrow).Show(scarecrow.transform, Scarecrows.GetZone(scarecrow), radius, colour);
                }
            }

            _removed.Clear();
            foreach (KeyValuePair<Scarecrow, ScarecrowRing> pair in _rings)
            {
                if (pair.Key == null)
                {
                    pair.Value.Destroy();
                    _removed.Add(pair.Key);
                }
                else if (!pair.Value.Used)
                {
                    pair.Value.Hide();
                }
            }

            foreach (Scarecrow scarecrow in _removed)
            {
                _rings.Remove(scarecrow);
            }
        }

        private void HideRings()
        {
            foreach (ScarecrowRing ring in _rings.Values)
            {
                ring.Hide();
            }
        }

        private ScarecrowRing GetRing(Scarecrow scarecrow)
        {
            if (!_rings.TryGetValue(scarecrow, out ScarecrowRing ring))
            {
                ring = new ScarecrowRing(transform, _material, _config.lineWidth / 100f);
                _rings[scarecrow] = ring;
            }

            return ring;
        }

        /// <summary>The hammer or a scarecrow is selected in the hotbar and no menu is open.</summary>
        private bool IsHoldingTool()
        {
            if (CanvasHelper.ActiveMenu != MenuType.None)
            {
                return false;
            }

            Network_Player player = ComponentManager<Network_Player>.Value;
            Slot slot = player != null && player.Inventory != null ? player.Inventory.GetSelectedHotbarSlot() : null;
            Item_Base item = slot != null && !slot.IsEmpty ? slot.GetItemBase() : null;
            if (item == null)
            {
                return false;
            }

            if (item.UniqueName == HammerItem)
            {
                return _config.withHammer;
            }

            return _config.withScarecrow && IsScarecrow(item);
        }

        /// <summary>Any placeable that builds a scarecrow, including scarecrows of other mods.</summary>
        private bool IsScarecrow(Item_Base item)
        {
            if (!_scarecrowItems.TryGetValue(item, out bool isScarecrow))
            {
                ItemInstance_Buildable buildable = item.settings_buildable;
                Block prefab = buildable != null && buildable.Placeable ? buildable.GetBlockPrefab(0) : null;
                isScarecrow = prefab != null && prefab.GetComponent<Scarecrow>() != null;
                _scarecrowItems[item] = isScarecrow;
            }

            return isScarecrow;
        }
    }
}
