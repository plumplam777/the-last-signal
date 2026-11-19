using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Anything the player can look at and press E on. Implemented by <see cref="Interactable"/>
    /// and its subclasses; custom objects may implement it directly.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Already-localized prompt text, e.g. "Pick up Fuse".</summary>
        string GetPromptText();

        /// <summary>False when the object is currently unusable; <paramref name="reasonKey"/> is a localization key explaining why (may be null).</summary>
        bool CanInteract(PlayerInteractor interactor, out string reasonKey);

        void Interact(PlayerInteractor interactor);

        /// <summary>False hides the object from the interactor entirely (e.g. already used one-shot).</summary>
        bool IsInteractionEnabled { get; }

        Transform transform { get; }
    }
}
