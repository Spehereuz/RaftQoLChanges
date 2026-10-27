using UnityEngine;

namespace QoLChanges.Features.AutoReplace
{
    /// <summary>
    /// Refills the hotbar at the end of the frame, after the game has finished the action that used the item up:
    /// the tool or food that was in the hand is already put away and the replacement is selected like any other item.
    /// </summary>
    internal sealed class AutoReplaceRunner : MonoBehaviour
    {
        private void LateUpdate()
        {
            AutoReplaceService.ProcessPending();
        }
    }
}
