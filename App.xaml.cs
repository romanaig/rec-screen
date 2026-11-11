using System.IO;
using System.Windows;
using System.Windows.Shell;

namespace RecScreen;

/// <summary>
/// Launched with no arguments (the pinned taskbar icon): starts recording right away.
/// Launched with --settings (the "Settings" entry on the icon's right-click menu): opens the settings.
/// </summary>
public partial class App : Application
{
    const string SettingsArg = "--settings";

    // Held for the whole recording so a second launch can't start a parallel one.
    Mutex? _recordingLock;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterJumpList();

        if (e.Args.Contains(SettingsArg))
        {
            new SettingsWindow().Show();
            return;
        }

        // Clicking the pinned icon during a recording just activates the running window (which stops it).
        // This guard covers the other launch paths, such as a Start menu shortcut.
        _recordingLock = new Mutex(true, "RecScreen.Recording", out bool isOnlyRecorder);
        if (!isOnlyRecorder)
        {
            Shutdown();
            return;
        }

        try
        {
            var window = new RecordingWindow(Settings.Load());
            SessionEnding += (_, _) => window.StopAndWait();
            window.Show();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Typically the output folder is on a drive that is no longer available.
            MessageBox.Show($"Cannot use the output folder:\n{ex.Message}\n\nPick another one in Settings.",
                "Rec Screen", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    // Adds "Settings" to the right-click menu of the taskbar icon; Windows keeps it after the app exits.
    void RegisterJumpList()
    {
        var jumpList = new JumpList();
        JumpList.SetJumpList(this, jumpList);
        jumpList.JumpItems.Add(new JumpTask
        {
            Title = "Settings",
            Arguments = SettingsArg,
            ApplicationPath = Environment.ProcessPath,
            IconResourcePath = Environment.ProcessPath,
        });
        jumpList.Apply();
    }
}
