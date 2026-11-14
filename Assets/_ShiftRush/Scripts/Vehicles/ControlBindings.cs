using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftRush.Vehicles
{
    /// <summary>
    /// The game's own controls (Resources/SR_Controls.inputactions: Throttle, Brake, Steering, Handbrake, NOS,
    /// Change Camera, Look Back, Reset), with the player's rebinds from Settings layered on top (PlayerPrefs).
    /// Same API as SHIFT//RUSH's version, which sat on RCC Pro's actions; the rider's controller reads these actions.
    /// </summary>
    public static class ControlBindings
    {
        const string SaveKey = "SR_Bindings";
        static InputActionAsset asset;

        public static InputActionAsset Asset
        {
            get
            {
                if (asset == null) asset = Resources.Load<InputActionAsset>("SR_Controls");
                return asset;
            }
        }

        /// <summary>Defaults first, then the player's saved rebinds, and the actions switched on. Safe to call again.</summary>
        public static bool Apply()
        {
            var a = Asset;
            if (a == null)
            {
                Debug.LogWarning("[BIKE//RUSH] Resources/SR_Controls not found; controls not set up.");
                return false;
            }
            a.RemoveAllBindingOverrides();
            string saved = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(saved)) a.LoadBindingOverridesFromJson(saved, false);
            a.Enable();
            return true;
        }

        /// <summary>Mutes or restores the nitro key (a timing moment can borrow Shift).</summary>
        public static void SetNitroInput(bool on)
        {
            var nos = Asset != null ? Asset.FindAction("NOS") : null;
            if (nos == null) return;
            if (on) nos.Enable(); else nos.Disable();
        }

        public static void Save()
        {
            if (Asset != null) PlayerPrefs.SetString(SaveKey, Asset.SaveBindingOverridesAsJson());
        }

        /// <summary>Forgets the player's rebinds and goes back to the defaults.</summary>
        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            Apply();
        }

        /// <summary>The key currently bound (e.g. "W", "LEFT SHIFT"), for on-screen hints; empty if unknown.</summary>
        public static string KeyName(string actionName, string composite, string part)
        {
            var action = Asset != null ? Asset.FindAction(actionName) : null;
            int i = FindKeyboardBinding(action, composite, part);
            if (i < 0) return "";
            return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpperInvariant();
        }

        /// <summary>
        /// Finds the keyboard binding for an action. For composites pass the composite's name ("wasd", "keyboard")
        /// and the part ("positive", "negative"); for plain buttons pass nulls.
        /// </summary>
        public static int FindKeyboardBinding(InputAction action, string composite, string part)
        {
            if (action == null) return -1;
            var b = action.bindings;
            if (string.IsNullOrEmpty(composite))
            {
                for (int i = 0; i < b.Count; i++)
                    if (!b[i].isComposite && !b[i].isPartOfComposite && b[i].path.StartsWith("<Keyboard>")) return i;
                return -1;
            }
            for (int i = 0; i < b.Count; i++)
            {
                if (!b[i].isComposite || !string.Equals(b[i].name, composite, System.StringComparison.OrdinalIgnoreCase)) continue;
                for (int j = i + 1; j < b.Count && b[j].isPartOfComposite; j++)
                    if (string.Equals(b[j].name, part, System.StringComparison.OrdinalIgnoreCase)) return j;
            }
            return -1;
        }
    }
}
