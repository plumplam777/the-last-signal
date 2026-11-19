using System.Collections.Generic;
using LastSignal.Core;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// General-purpose interactable: prompt key, requirements, effects and a UnityEvent.
    /// Most props need only this component; subclasses override <see cref="OnInteract"/> for special behaviour.
    /// One-shot interactables remember they were used through a WorldState flag so it survives save/load.
    /// </summary>
    public class Interactable : MonoBehaviour, IInteractable
    {
        [Tooltip("Unique within the game. Used for the 'used' flag of one-shot interactables.")]
        [SerializeField] protected string interactableId;
        [Tooltip("Localization key of the action, e.g. prompt.use")]
        [SerializeField] protected string promptKey = "prompt.use";
        [Tooltip("Localization key of the object name inserted into the prompt ({0}). Optional.")]
        [SerializeField] protected string objectNameKey;
        [SerializeField] protected ConditionSet requirements = new();
        [SerializeField] protected List<Effect> effects = new();
        [SerializeField] protected bool oneShot;
        [SerializeField] protected UnityEvent onInteracted = new();

        public string InteractableId => interactableId;
        public string UsedFlag => WorldKeys.Story($"used.{interactableId}");
        public bool WasUsed => oneShot && !string.IsNullOrEmpty(interactableId) && GameServices.World.GetBool(UsedFlag);

        public virtual bool IsInteractionEnabled => isActiveAndEnabled && !WasUsed;

        public UnityEvent OnInteracted => onInteracted;

        protected virtual void Start()
        {
            if (oneShot && string.IsNullOrEmpty(interactableId))
                Log.Warn(nameof(Interactable), $"One-shot interactable '{name}' has no id; its state will not be saved.", this);
            if (WasUsed) RestoreUsedState();
        }

        public virtual string GetPromptText()
        {
            if (string.IsNullOrEmpty(objectNameKey)) return Loc.Get(promptKey);
            return Loc.Get(promptKey, Loc.Get(objectNameKey));
        }

        public virtual bool CanInteract(PlayerInteractor interactor, out string reasonKey)
            => requirements.Evaluate(out reasonKey);

        public void Interact(PlayerInteractor interactor)
        {
            if (!IsInteractionEnabled) return;
            if (!CanInteract(interactor, out _)) return;
            OnInteract(interactor);
            Effects.ApplyAll(effects);
            if (oneShot && !string.IsNullOrEmpty(interactableId)) GameServices.World.SetBool(UsedFlag, true);
            onInteracted.Invoke();
        }

        /// <summary>Object-specific behaviour. Called before effects are applied.</summary>
        protected virtual void OnInteract(PlayerInteractor interactor) { }

        /// <summary>Called on Start when a one-shot was already used in a loaded save (e.g. hide a picked-up item).</summary>
        protected virtual void RestoreUsedState() { }

#if UNITY_EDITOR
        /// <summary>Editor-only setup used by level builder tools.</summary>
        public void EditorConfigure(string id, string prompt, string nameKey, bool isOneShot, ConditionSet reqs = null, List<Effect> fx = null)
        {
            interactableId = id;
            promptKey = prompt;
            objectNameKey = nameKey;
            oneShot = isOneShot;
            requirements = reqs ?? new ConditionSet();
            effects = fx ?? new List<Effect>();
        }
#endif
    }
}
