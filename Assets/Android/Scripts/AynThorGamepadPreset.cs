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
    /// A=Jump, B=Interact/Activate (swapped from the initial preset per user preference, 2026-09-17) -
    /// note menu-navigation JoystickUIActions below are unaffected by this swap and keep the standard
    /// A=confirm/B=cancel convention regardless of what A/B do in gameplay.
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
            input.SetBinding(KeyCode.JoystickButton1, InputManager.Actions.Jump, primary: true);                 // A
            input.SetBinding(KeyCode.JoystickButton0, InputManager.Actions.ActivateCenterObject, primary: true); // B
            input.SetBinding(KeyCode.JoystickButton3, InputManager.Actions.ReadyWeapon, primary: true);          // X
            // RecastSpell re-readies whatever spell EntityEffectManager.lastSpell holds (the spell most
            // recently cast, tracked automatically by CastReadySpell), independent of the spellbook window -
            // distinct from CastSpell (Actions.CastSpell), which only opens the spellbook to pick a fresh
            // spell to ready. Bound here so Y can instantly recast without leaving gameplay to the spellbook.
            input.SetBinding(KeyCode.JoystickButton2, InputManager.Actions.RecastSpell, primary: true);          // Y

            // Triggers/bumpers. Left Trigger (JoystickButton6) is intentionally left unbound - no Block
            // mechanic exists to bind it to.
            input.SetBinding(KeyCode.JoystickButton7, InputManager.Actions.SwingWeapon, primary: true); // Right Trigger
            input.SetBinding(KeyCode.JoystickButton5, InputManager.Actions.ToggleRun, primary: true);   // Right Bumper
            input.SetBinding(KeyCode.JoystickButton4, InputManager.Actions.Crouch, primary: true);      // Left Bumper

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
