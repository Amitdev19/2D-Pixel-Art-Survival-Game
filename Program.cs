using System.Windows.Forms;
using MicroRoguelike;

namespace MicroRoguelike;

/// <summary>
/// Entry point for the MicroRoguelike application.
/// Configures and launches the main game form.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point - configures UI and runs the game form.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var form = new GameForm();
        Application.Run(form);
    }
}