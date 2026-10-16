using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Nonvisual selection adapter. A future view can subscribe to OnUpgradeOffered
/// and call SelectUpgrade from its buttons. Number keys keep the prototype playable.
/// </summary>
public class UpgradeUI : MonoBehaviour {
    public void SelectUpgrade(int index) {
        UpgradeManager.Instance?.ChooseUpgrade(index);
    }

    void Update() {
        if (UpgradeManager.Instance == null || !UpgradeManager.Instance.IsChoosing || Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectUpgrade(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectUpgrade(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectUpgrade(2);
    }
}
