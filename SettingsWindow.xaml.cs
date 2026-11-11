using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScreenRecorderLib;

namespace RecScreen;

public partial class SettingsWindow : Window
{
    /// <summary>A combo box entry. A null Id means "let Windows decide" (main monitor / default microphone).</summary>
    sealed record Choice(string? Id, string Name)
    {
        public override string ToString() => Name;
    }

    readonly Settings _settings = Settings.Load();

    public SettingsWindow()
    {
        InitializeComponent();

        Fill(MonitorBox, "Main monitor", _settings.Monitor,
            Recorder.GetDisplays().Select((d, i) => new Choice(d.DeviceName, $"{i + 1}: {d.FriendlyName}")));
        Fill(MicBox, "Windows default", _settings.Microphone,
            Recorder.GetSystemAudioCaptureDevices().Select(d => new Choice(d.DeviceName, d.FriendlyName)));

        SystemAudioBox.IsChecked = _settings.RecordSystemAudio;
        MicrophoneBox.IsChecked = _settings.RecordMicrophone;
        FolderBox.Text = _settings.OutputFolder;
    }

    static void Fill(ComboBox box, string defaultName, string? selectedId, IEnumerable<Choice> devices)
    {
        var choices = devices.Prepend(new Choice(null, defaultName)).ToList();
        box.ItemsSource = choices;
        box.SelectedItem = choices.Find(c => c.Id == selectedId) ?? choices[0];
    }

    // Classic Windows "Sound" dialog: default devices, levels, enabling disabled microphones.
    void OpenSoundSettings(object sender, RoutedEventArgs e) =>
        Process.Start("control.exe", "mmsys.cpl")?.Dispose();

    void ChooseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { InitialDirectory = FolderBox.Text };
        if (dialog.ShowDialog(this) == true)
            FolderBox.Text = dialog.FolderName;
    }

    // IsCancel only auto-closes modal dialogs; this window is shown modeless.
    void Cancel(object sender, RoutedEventArgs e) => Close();

    void Save(object sender, RoutedEventArgs e)
    {
        _settings.Monitor = ((Choice)MonitorBox.SelectedItem).Id;
        _settings.RecordSystemAudio = SystemAudioBox.IsChecked == true;
        _settings.RecordMicrophone = MicrophoneBox.IsChecked == true;
        _settings.Microphone = ((Choice)MicBox.SelectedItem).Id;
        _settings.OutputFolder = FolderBox.Text;
        try
        {
            _settings.Save();
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Could not save settings:\n{ex.Message}", "Rec Screen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
