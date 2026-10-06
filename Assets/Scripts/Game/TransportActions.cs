namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Shared validation for changing transport mode, extracted from DaggerfallUI's
    /// dfuiOpenTransportWindow handler so DaggerfallTransportWindow and any other caller (e.g. the Android
    /// fork's Display 2 Home panel) can't drift apart.
    /// </summary>
    public static class TransportActions
    {
        /// <summary>
        /// True when the player is currently allowed to change transport mode (not indoors, and grounded).
        /// </summary>
        public static bool CanChangeTransportMode()
        {
            return !GameManager.Instance.IsPlayerInside && GameManager.Instance.PlayerController.isGrounded;
        }
    }
}
