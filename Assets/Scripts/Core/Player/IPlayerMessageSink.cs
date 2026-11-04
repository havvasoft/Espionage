namespace Espionage.Core
{
    /// <summary>
    /// Shows a short message to the player (implemented by the player's HUD). Lets world objects such as a
    /// control panel talk to the player without referencing the player side.
    /// </summary>
    public interface IPlayerMessageSink
    {
        void ShowMessage(string text);
    }
}
