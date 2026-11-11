using System.ComponentModel;
using System.IO;
using System.Windows;
using ScreenRecorderLib;

namespace RecScreen;

/// <summary>
/// Records while it sits minimized on the taskbar. Clicking the taskbar icon restores the
/// window, which stops the recording; the window closes (and the app exits) once the MP4 is saved.
/// </summary>
public partial class RecordingWindow : Window
{
    readonly Recorder _recorder;
    readonly ManualResetEventSlim _saved = new();
    bool _stopping;
    bool _finished;

    public RecordingWindow(Settings settings)
    {
        InitializeComponent();

        Directory.CreateDirectory(settings.OutputFolder);
        string file = Path.Combine(settings.OutputFolder, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4");

        // These events arrive on the recorder's thread. BeginInvoke (not Invoke) lets that thread
        // finish before Finish() disposes the recorder, which waits for it.
        _recorder = Recorder.CreateRecorder(BuildOptions(settings));
        _recorder.OnRecordingComplete += (_, _) =>
        {
            _saved.Set();
            Dispatcher.BeginInvoke(Finish);
        };
        _recorder.OnRecordingFailed += (_, e) =>
        {
            _saved.Set();
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show($"Recording failed:\n{e.Error}", "Rec Screen", MessageBoxButton.OK, MessageBoxImage.Error);
                Finish();
            });
        };

        // Started here, before the window exists, so a stop request can never precede it.
        _recorder.Record(file);
    }

    static RecorderOptions BuildOptions(Settings settings)
    {
        // A saved monitor or microphone may have been unplugged since; fall back to the defaults.
        // Devices are only enumerated when a specific one was chosen, to keep startup fast.
        bool monitorExists = settings.Monitor is not null
            && Recorder.GetDisplays().Any(d => d.DeviceName == settings.Monitor);
        bool micExists = settings.RecordMicrophone && settings.Microphone is not null
            && Recorder.GetSystemAudioCaptureDevices().Any(d => d.DeviceName == settings.Microphone);

        var audioSources = new List<AudioSourceBase>();
        if (settings.RecordSystemAudio)
            audioSources.Add(LoopbackAudioSource.Default);
        if (settings.RecordMicrophone)
            audioSources.Add(micExists ? new CaptureAudioSource(settings.Microphone) : CaptureAudioSource.Default);

        return new RecorderOptions
        {
            SourceOptions = new SourceOptions
            {
                RecordingSources = [monitorExists ? new DisplayRecordingSource(settings.Monitor) : DisplayRecordingSource.MainMonitor],
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = audioSources.Count > 0,
                AudioSources = audioSources,
            },
        };
    }

    void Stop()
    {
        if (_stopping)
            return;
        _stopping = true;
        _recorder.Stop(); // finalizing the file is asynchronous; OnRecordingComplete calls Finish
    }

    /// <summary>
    /// Logoff/shutdown: Windows won't wait for the asynchronous close, so finalize the file
    /// before returning and let the window close.
    /// </summary>
    public void StopAndWait()
    {
        Stop();
        _saved.Wait(TimeSpan.FromSeconds(10));
        _finished = true;
    }

    void Finish()
    {
        _finished = true;
        _recorder.Dispose();
        Close();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState != WindowState.Minimized)
            Stop();
    }

    // "Close window" on the taskbar must also save the file instead of discarding it.
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_finished)
        {
            e.Cancel = true;
            Stop();
        }
        base.OnClosing(e);
    }
}
