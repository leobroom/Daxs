using System.Collections.Generic;

namespace Daxs.Actions;

internal sealed class ActionGate
{
    public static ActionGate Instance { get; } = new ActionGate();
    private ActionGate() { }

    // If not null -> we are in modal mode
    public ActionBase ModalOwner { get; private set; }

    // Optional: allow some actions even in modal (e.g. HUD, cancel)
    private readonly HashSet<BindingId> allowedInModal = new();

    public bool IsModal => ModalOwner != null;

    public void EnterModal(ActionBase owner, params BindingId[] allow)
    {
        ModalOwner = owner;
        allowedInModal.Clear();
        foreach (var a in allow) allowedInModal.Add(a);
    }

    public void ExitModal(ActionBase owner)
    {
        if (ReferenceEquals(ModalOwner, owner))
            ModalOwner = null;
    }

    public bool Allows(BindingId actionType, IAction actionInstance)
    {
        if (!IsModal) return true;

        // Always allow the modal owner action itself (so we can detect release via your deactivation logic)
        if (ReferenceEquals(actionInstance, ModalOwner)) return true;

        // Allow optional whitelisted actions
        return allowedInModal.Contains(actionType);
    }
}