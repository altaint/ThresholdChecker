using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ThresholdChecker
{
    public enum PacingState
    {
        TooFast,
        OnTrack,
        Behind
    }

    public enum ChatChannel
    {
        Echo,
        Party,
        Alliance,
        Say,
        Yell,
        Shout
    }

    [Serializable]
    public class ThresholdPhase
    {
        public int TimeMinutes { get; set; }
        public int TimeSeconds { get; set; }
        public double TargetHpPercent { get; set; }

        public int TotalSeconds => (TimeMinutes * 60) + TimeSeconds;
    }

    [Serializable]
    public class Configurations
    {
        public string ConfigurationName { get; set; } = "Configuration";
        public List<ThresholdPhase> Phase1Thresholds { get; set; } = new();
        public List<ThresholdPhase> Phase2Thresholds { get; set; } = new();
    }

    [Serializable]
    public class TargetConfig
    {
        public string TargetName { get; set; } = "Unknown";
        public double TolerancePercent { get; set; } = 3.0;
        public List<Configurations> Configurations { get; set; } = new();
    }

    [Serializable]
    public class DutyConfig
    {
        public string DutyName { get; set; } = "Unknown Duty";
        public uint TerritoryId { get; set; } 
        public List<TargetConfig> Targets { get; set; } = new();
    }

    [Serializable]
    public class SerializableColor
    {
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public float A { get; set; } = 1f;

        public SerializableColor() { }

        public SerializableColor(float r, float g, float b, float a)
        {
            R = r; G = g; B = b; A = a;
        }

        public Vector4 ToVector4() => new Vector4(R, G, B, A);
        public void FromVector4(Vector4 v)
        {
            R = v.X; G = v.Y; B = v.Z; A = v.W;
        }
    }

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public List<DutyConfig> Duties { get; set; } = new();

        public ChatChannel OutputChannel { get; set; } = ChatChannel.Echo;

        public string TooFastMessage { get; set; } = "Pace is too fast by {diff}%! Hold Damage.";
        public string OnTrackMessage { get; set; } = "Pace is on track. (Off by {diff}%)";
        public string BehindMessage { get; set; } = "Pace is behind by {diff}%! Push Damage.";

        public SerializableColor TooFastColor { get; set; } = new SerializableColor(0.6f, 0.5f, 0.0f, 1.0f);
        public SerializableColor OnTrackColor { get; set; } = new SerializableColor(0.1f, 0.7f, 0.2f, 1.0f);
        public SerializableColor BehindColor { get; set; } = new SerializableColor(1.0f, 0.35f, 0.1f, 1.0f);

        public void Save()
        {
            Service.PluginInterface.SavePluginConfig(this);
        }
    }
}
