using Dalamud.Configuration;
using System;
using System.Collections.Generic;

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
    public class TargetConfig
    {
        public string TargetName { get; set; } = "Unknown";
        public double TolerancePercent { get; set; } = 3.0;
        public List<ThresholdPhase> Thresholds { get; set; } = new();
        public List<ThresholdPhase> P2Thresholds { get; set; } = new();
    }

    [Serializable]
    public class DutyConfig
    {
        public string DutyName { get; set; } = "Unknown Duty";
        public uint TerritoryId { get; set; } 
        public List<TargetConfig> Targets { get; set; } = new();
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

        public void Save()
        {
            Service.PluginInterface.SavePluginConfig(this);
        }
    }
}
