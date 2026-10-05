using UnityEngine;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Applies the AYN Thor's physical gamepad as a default control scheme, on top of Daggerfall Unity's
    /// existing Actions/AxisActions/JoystickUIActions binding system (see Assets/Scripts/Game/InputManager.cs).
    ///
    /// Actions are bound as PRIMARY, not secondary - confirmed on-device that InputManager's own
    /// FindKeyboardActions() only ever reads the "existingKeyDict" cache, which is the primary bindings
    /// dictionary gap-filled by secondary bindings ONLY for actions that have no primary binding at all.
    /// Since every action here already has a real default keyboard/mouse primary binding, a secondary-only
    /// gamepad binding is silently never checked. That means applying this preset does replace the
    /// keyboard-equivalent key for each rebound action (e.g. Jump moves off Space) - an acceptable
    /// trade-off for a gamepad-first handheld, but worth being upfront about.
    ///
    /// Physical button -> KeyCode.JoystickButtonN / AxisN mapping confirmed on-device (2026-09-17) by
    /// logging Input.GetKeyDown/GetAxisRaw while pressing each button and moving each stick. This mapping
    /// is Thor-specific (driver/OS-dependent) and not assumed to match other Android gamepads, which is
    /// why this is an opt-in preset rather than a change to InputManager's global cross-device defaults.
    ///
    /// Notably absent: DFU has no Block/parry action or mechanic at all (classic Daggerfall combat has no
    /// active player-triggered block), so the Left Trigger is intentionally left unbound here.
    ///
    /// A=Interact/Activate, B=Jump (per the simplified-attack-mode control scheme, 2026-10-05) - note
    /// menu-navigation JoystickUIActions below are unaffected by this and keep the standard
    /// A=confirm/B=cancel convention regardless of what A/B do in gameplay.
    ///
    /// L3/R3 (stick click) are JoystickButton8/9 - confirmed on-device (2026-10-05), matching the
    /// sequential pattern the other confirmed buttons establish (0-3 face, 4-5 bumpers, 6-7 triggers,
    /// 10-11 start/select -> 8-9 stick clicks).
    ///
    /// The D-Pad is NOT a set of JoystickButtonN presses at all - confirmed on-device (2026-10-05) that it
    /// reports as a hat switch on Axis5 (horizontal: Right=-1, Left=+1) and Axis6 (vertical: Up=-1,
    /// Down=+1), via a diagnostic that scanned the full JoystickButton0-19 + Joystick1Button0 through
    /// Joystick8Button19 range (see InputManager.ConvertJoystickButtonKeyCode's own comment on that
    /// Joystick1ButtonX-vs-JoystickButtonX inconsistency - that normalization doesn't help here since this
    /// is an axis, not a button, issue) and came back completely empty for buttons, then an axis scan
    /// caught it immediately. InputManager.SetBinding only binds discrete KeyCodes to Actions - there's no
    /// "axis crosses threshold -> fire this Action once" mechanism in its binding system, so the D-Pad
    /// can't be wired the same way as everything else here. See AynThorDPadHandler, a small polling
    /// component that watches these two axes directly and posts the same DaggerfallUIMessages the
    /// Inventory/CharacterSheet/AutoMap/Transport Actions would.
    /// </summary>
    public static class AynThorGamepadPreset
    {
        public static void Apply()
        {
            InputManager input = InputManager.Instance;

            input.EnableController = true;
            DaggerfallUnity.Settings.EnableController = true;

            // Combined Crouch+Sneak toggle (see PlayerSpeedChanger.CaptureInputSpeedAdjustment) only
            // couples the two together when Toggle Sneak is on, so enable it as part of this preset.
            DaggerfallUnity.Settings.ToggleSneak = true;

            // Left stick / right stick. The Thor's right stick doesn't match InputManager's built-in
            // CameraHorizontal/Vertical defaults (Axis4/Axis5), so both pairs need setting explicitly.
            input.SetAxisBinding("Axis1", InputManager.AxisActions.MovementHorizontal);
            input.SetAxisBinding("Axis2", InputManager.AxisActions.MovementVertical);
            input.SetAxisBinding("Axis3", InputManager.AxisActions.CameraHorizontal);
            input.SetAxisBinding("Axis4", InputManager.AxisActions.CameraVertical);

            // The stock CameraVertical default (Axis5) has invert=1 baked into
            // ProjectSettings/InputManager.asset; Axis4 (the Thor's actual right-stick vertical axis)
            // has invert=0, so without this the look-up/down direction comes out backwards.
            input.SetAxisActionInversion(InputManager.AxisActions.CameraVertical, true);

            // Face buttons
            input.SetBinding(KeyCode.JoystickButton1, InputManager.Actions.ActivateCenterObject, primary: true); // A
            input.SetBinding(KeyCode.JoystickButton0, InputManager.Actions.Jump, primary: true);                 // B
            input.SetBinding(KeyCode.JoystickButton3, InputManager.Actions.ReadyWeapon, primary: true);          // X
            // RecastSpell re-readies whatever spell EntityEffectManager.lastSpell holds (the spell most
            // recently cast, tracked automatically by CastReadySpell), independent of the spellbook window -
            // distinct from CastSpell (Actions.CastSpell), which only opens the spellbook to pick a fresh
            // spell to ready. Bound here so Y can instantly recast without leaving gameplay to the spellbook.
            input.SetBinding(KeyCode.JoystickButton2, InputManager.Actions.RecastSpell, primary: true);          // Y

            // Triggers/bumpers. Left Trigger (JoystickButton6) is intentionally left unbound - no Block
            // mechanic exists to bind it to.
            input.SetBinding(KeyCode.JoystickButton7, InputManager.Actions.SwingWeapon, primary: true); // Right Trigger
            input.SetBinding(KeyCode.JoystickButton5, InputManager.Actions.QuickSave, primary: true);   // Right Bumper
            input.SetBinding(KeyCode.JoystickButton4, InputManager.Actions.LogBook, primary: true);     // Left Bumper

            // Stick clicks (L3/R3)
            input.SetBinding(KeyCode.JoystickButton8, InputManager.Actions.ToggleRun, primary: true); // L3
            input.SetBinding(KeyCode.JoystickButton9, InputManager.Actions.Crouch, primary: true);    // R3

            // D-Pad is handled by AynThorDPadHandler (see class doc above) - it's an axis hat switch, not
            // JoystickButtonN presses, so InputManager.SetBinding can't express it.

            // Start/Select
            input.SetBinding(KeyCode.JoystickButton10, InputManager.Actions.Escape, primary: true); // Start
            input.SetBinding(KeyCode.JoystickButton11, InputManager.Actions.Rest, primary: true);   // Select

            // Menu navigation (JoystickUIActions) - stock defaults are backwards on the Thor
            // (LeftClick=B, Back=A); fix to standard A=confirm/B=cancel convention.
            input.SetJoystickUIBinding(KeyCode.JoystickButton1, InputManager.JoystickUIActions.LeftClick); // A
            input.SetJoystickUIBinding(KeyCode.JoystickButton0, InputManager.JoystickUIActions.Back);      // B

            DaggerfallUnity.Settings.SaveSettings();
            input.SaveKeyBinds();
        }
    }
}
