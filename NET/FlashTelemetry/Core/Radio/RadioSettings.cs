using System.Collections.Generic;

namespace FlashTelemetry.Core.Radio
{
    public enum DiscordAction
    {
        None = 0,
        MuteToggle = 1,
        DeafenToggle = 2
    }

    public sealed class RadioAlias
    {
        public string Original { get; set; } = "";
        public string Alias { get; set; } = "";
    }

    public sealed class RadioSettings
    {
        // Toggle principal (lo enciende/apaga tu check de la base)
        public bool Enabled { get; set; } = false;

        // Audio
        public string OutputDeviceId { get; set; } = "";
        public string InputDeviceId { get; set; } = "";

        // 0..1
        public double TtsVolume { get; set; } = 0.85;
        public double BeepVolume { get; set; } = 0.35;

        // Discord
        public DiscordAction DiscordAction { get; set; } = DiscordAction.MuteToggle;
        public string DiscordMuteHotkey { get; set; } = "CTRL+ALT+MAYÚS+F11";
        public string DiscordDeafenHotkey { get; set; } = "CTRL+ALT+MAYÚS+F12";

        // WAV opcionales (si vacío => beep por defecto)
        public string RadioOpenWavPath { get; set; } = "";
        public string RadioCloseWavPath { get; set; } = "";

        // Carpeta de grabaciones (si vacío => AppData\Roaming\FlashTelemetry\recordings\<profileId>)
        public string RecordingsFolderPath { get; set; } = "";

        // STT (whisper.cpp)
        public bool SttEnabled { get; set; } = false;
        public string WhisperExePath { get; set; } = "";
        public string WhisperModelPath { get; set; } = "";
        public string WhisperLanguage { get; set; } = "es";
        public string WhisperExtraArgs { get; set; } = "";

        // Aliases pilotos
        public List<RadioAlias> Aliases { get; set; } = new();
    }
}
