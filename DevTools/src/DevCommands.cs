using System;
using System.Collections.Generic;
using System.Text;
using RaftModLoader;

namespace QoLDevTools
{
    /// <summary>RML console commands (F10). The returned text is printed to the console.</summary>
    public static class DevCommands
    {
        private const string DurSyntax = "qoldev.dur <uses|percent%>";
        private const string DurAllSyntax = "qoldev.dur.all <uses|percent%|random>";
        private const string WearSyntax = "qoldev.wear <on|off>";
        private const string FindSyntax = "qoldev.find <text>";
        private const string GiveSyntax = "qoldev.give <item> [amount]";

        private const int MaxFindResults = 40;
        private const int MaxGiveAmount = 9999;

        [ConsoleCommand(
            name: "qoldev.find",
            docs: "Lists items whose internal or English name contains the text. Syntax: " + FindSyntax)]
        public static string Find(string[] args)
        {
            return Run(() =>
            {
                string text = args != null ? string.Join(" ", args).Trim() : string.Empty;
                if (text.Length == 0)
                {
                    return "Syntax: " + FindSyntax;
                }

                List<Item_Base> matches = ItemCatalog.Search(text);
                if (matches.Count == 0)
                {
                    return "No items match '" + text + "'.";
                }

                StringBuilder report = new StringBuilder();
                for (int i = 0; i < matches.Count && i < MaxFindResults; i++)
                {
                    report.AppendLine(ItemCatalog.Describe(matches[i]));
                }

                if (matches.Count > MaxFindResults)
                {
                    report.AppendLine("... and " + (matches.Count - MaxFindResults) + " more, refine the search.");
                }

                return report.ToString().TrimEnd();
            });
        }

        [ConsoleCommand(
            name: "qoldev.give",
            docs: "Adds items to the inventory, as many as fit. Item is an internal name (Cooked_Catfish) or an English name (Cooked catfish). Syntax: " + GiveSyntax)]
        public static string Give(string[] args)
        {
            return Run(() =>
            {
                if (!TryParseGiveArgs(args, out string name, out int amount, out string error))
                {
                    return error;
                }

                if (!TryGetLocalPlayer(out Network_Player player, out error))
                {
                    return error;
                }

                List<Item_Base> items = ItemCatalog.FindExact(name);
                if (items.Count == 0)
                {
                    return "No item '" + name + "'. Search with: qoldev.find " + name;
                }

                if (items.Count > 1)
                {
                    StringBuilder choices = new StringBuilder("Several items are called '" + name + "', use the internal name:");
                    foreach (Item_Base candidate in items)
                    {
                        choices.AppendLine().Append(ItemCatalog.Describe(candidate));
                    }

                    return choices.ToString();
                }

                Item_Base item = items[0];
                int given = ItemGiver.Give(player.Inventory, item, amount);
                string itemName = ItemCatalog.DisplayName(item);
                if (given == amount)
                {
                    return "Gave " + given + " x " + itemName + ".";
                }

                return given == 0
                    ? "Inventory is full, nothing given."
                    : "Gave " + given + " of " + amount + " x " + itemName + ", inventory is full.";
            });
        }

        [ConsoleCommand(
            name: "qoldev.dur",
            docs: "Sets the durability of the item in the selected hotbar slot; 0 uses it up. Syntax: " + DurSyntax)]
        public static string SetSelected(string[] args)
        {
            return Run(() =>
            {
                if (!TryParseSingle(args, false, DurSyntax, out DurabilityValue value, out string error))
                {
                    return error;
                }

                if (!TryGetLocalPlayer(out Network_Player player, out error))
                {
                    return error;
                }

                Slot slot = player.Inventory.GetSelectedHotbarSlot();
                if (slot == null || slot.IsEmpty)
                {
                    return "The selected hotbar slot is empty.";
                }

                if (!SlotDurability.HasDurability(slot))
                {
                    return "The selected item has no durability bar.";
                }

                return SlotDurability.Apply(slot, value);
            });
        }

        [ConsoleCommand(
            name: "qoldev.dur.all",
            docs: "Sets the durability of every item with a durability bar in the hotbar, inventory, equipment and the open storage; 'random' never breaks items. Syntax: " + DurAllSyntax)]
        public static string SetAll(string[] args)
        {
            return Run(() =>
            {
                if (!TryParseSingle(args, true, DurAllSyntax, out DurabilityValue value, out string error))
                {
                    return error;
                }

                if (!TryGetLocalPlayer(out Network_Player player, out error))
                {
                    return error;
                }

                List<Slot> slots = SlotDurability.CollectPlayerSlots(player, out bool includesStorage);
                StringBuilder report = new StringBuilder();
                int changed = 0;
                foreach (Slot slot in slots)
                {
                    if (!SlotDurability.HasDurability(slot))
                    {
                        continue;
                    }

                    report.AppendLine(SlotDurability.Apply(slot, value));
                    changed++;
                }

                string scope = includesStorage ? "inventory, equipment and open storage" : "inventory and equipment";
                return changed == 0
                    ? "No items with durability in " + scope + "."
                    : report + "Changed " + changed + " item(s) in " + scope + ".";
            });
        }

        [ConsoleCommand(
            name: "qoldev.wear",
            docs: "Makes tools lose durability in Creative until turned off or the world is left. Syntax: " + WearSyntax)]
        public static string Wear(string[] args)
        {
            return Run(() =>
            {
                string mode = args != null && args.Length == 1 ? args[0].Trim().ToLowerInvariant() : null;
                if (mode != "on" && mode != "off")
                {
                    return "Tool wear is " + (CreativeWear.IsOn ? "on" : "off") + ". Syntax: " + WearSyntax;
                }

                if (!TryGetLocalPlayer(out Network_Player _, out string error))
                {
                    return error;
                }

                return mode == "on" ? CreativeWear.TurnOn() : CreativeWear.TurnOff();
            });
        }

        [ConsoleCommand(
            name: "qoldev.fuel",
            docs: "Host only: sets the fuel of every engine on the raft, in units (a tank holds 50) or percent of the tank. Syntax: " + EngineFuel.Syntax)]
        public static string Fuel(string[] args)
        {
            return Run(() =>
            {
                if (args == null || args.Length != 1)
                {
                    return "Syntax: " + EngineFuel.Syntax;
                }

                if (!TryGetLocalPlayer(out Network_Player _, out string error))
                {
                    return error;
                }

                return EngineFuel.Set(args[0]);
            });
        }

        [ConsoleCommand(
            name: "qoldev.net",
            docs: "Host only: fills the collection net under the crosshair with debris, to a number of items or 'full', or empties it with 'clear'. Syntax: " + CollectionNet.Syntax)]
        public static string Net(string[] args)
        {
            return Run(() =>
            {
                if (args == null || args.Length != 1)
                {
                    return "Syntax: " + CollectionNet.Syntax;
                }

                if (!TryGetLocalPlayer(out Network_Player _, out string error))
                {
                    return error;
                }

                return CollectionNet.Set(args[0]);
            });
        }

        /// <summary>The last argument is the amount when it is a number; everything before it is the name.</summary>
        private static bool TryParseGiveArgs(string[] args, out string name, out int amount, out string error)
        {
            name = null;
            amount = 1;
            error = null;
            if (args == null || args.Length == 0)
            {
                error = "Syntax: " + GiveSyntax;
                return false;
            }

            int nameLength = args.Length;
            if (args.Length > 1 && int.TryParse(args[args.Length - 1], out int parsed))
            {
                if (parsed < 1 || parsed > MaxGiveAmount)
                {
                    error = "Amount must be between 1 and " + MaxGiveAmount + ".";
                    return false;
                }

                amount = parsed;
                nameLength--;
            }

            name = string.Join(" ", args, 0, nameLength).Trim();
            if (name.Length == 0)
            {
                error = "Syntax: " + GiveSyntax;
                return false;
            }

            return true;
        }

        private static bool TryParseSingle(string[] args, bool allowRandom, string syntax, out DurabilityValue value, out string error)
        {
            value = default(DurabilityValue);
            if (args == null || args.Length != 1)
            {
                error = "Syntax: " + syntax;
                return false;
            }

            return DurabilityValue.TryParse(args[0], allowRandom, out value, out error);
        }

        private static bool TryGetLocalPlayer(out Network_Player player, out string error)
        {
            try
            {
                player = RAPI.GetLocalPlayer();
            }
            catch (NullReferenceException)
            {
                // Outside a world the network manager RAPI reads from does not exist.
                player = null;
            }

            if (player == null || player.Inventory == null)
            {
                error = "Load a world first.";
                return false;
            }

            error = null;
            return true;
        }

        private static string Run(Func<string> command)
        {
            try
            {
                return command();
            }
            catch (Exception e)
            {
                DevLog.Error("Command failed.", e);
                return "Command failed: " + e.Message;
            }
        }
    }
}
