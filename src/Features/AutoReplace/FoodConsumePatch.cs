using System.Reflection;
using HarmonyLib;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>
    /// Eating and drinking run UseItemController.ConsumeStacks, a coroutine that takes uses from the selected hotbar
    /// slot after the animation delay; the last portion empties the slot. The coroutine body is patched, so every
    /// step is covered.
    /// </summary>
    [HarmonyPatch]
    internal static class FoodConsumePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(
                AccessTools.Method(typeof(UseItemController), "ConsumeStacks", new[] { typeof(int), typeof(float) }));
        }

        [HarmonyPrefix]
        private static void Prefix(out HotbarSnapshot __state)
        {
            __state = AutoReplaceService.Capture();
        }

        [HarmonyPostfix]
        private static void Postfix(HotbarSnapshot __state)
        {
            AutoReplaceService.Complete(__state, ReplaceCategory.Food);
        }
    }
}
