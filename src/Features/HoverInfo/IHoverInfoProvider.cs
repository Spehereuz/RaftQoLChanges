namespace QoLChanges.Features.HoverInfo
{
    /// <summary>Describes one kind of object the player can aim at.</summary>
    internal interface IHoverInfoProvider
    {
        /// <summary>
        /// Returns true when the provider is responsible for what the player aims at; providers after it are not
        /// asked then. <paramref name="target"/> is the interactable found by the game's raycast, null when there is
        /// none (some objects, like plants, are found with a raycast of their own). <paramref name="text"/> is
        /// null or empty when there is nothing to show.
        /// </summary>
        bool TryDescribe(RaycastInteractable target, out string text);
    }
}
