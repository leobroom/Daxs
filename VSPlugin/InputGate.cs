using System.Collections.Generic;

namespace Daxs
{
    internal sealed class InputGate
    {
        public static InputGate Instance { get; } = new InputGate();
        private InputGate() { }

        // If not null -> we are in modal mode
        public BaseState ModalOwner { get; private set; }

        // Optional: allow some actions even in modal (e.g. HUD, cancel)
        private readonly HashSet<GAction> allowedInModal = new();

        public bool IsModal => ModalOwner != null;

        public void EnterModal(BaseState owner, params GAction[] allow)
        {
            ModalOwner = owner;
            allowedInModal.Clear();
            foreach (var a in allow) allowedInModal.Add(a);
        }

        public void ExitModal(BaseState owner)
        {
            if (ReferenceEquals(ModalOwner, owner))
                ModalOwner = null;
        }

        public bool Allows(GAction actionType, IAction actionInstance)
        {
            if (!IsModal) return true;

            // Always allow the modal owner action itself (so we can detect release via your deactivation logic)
            if (ReferenceEquals(actionInstance, ModalOwner)) return true;

            // Allow optional whitelisted actions
            return allowedInModal.Contains(actionType);
        }
    }
}