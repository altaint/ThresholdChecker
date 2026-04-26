using System;
using Dalamud.Game.Text;

namespace ThresholdChecker
{
    public class ChatManager
    {
        private readonly Plugin plugin;

        public ChatManager(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void PrintStatusToChat(bool fromCommand = false)
        {
            if (!plugin.Tracker.IsTracking)
            {
                if (fromCommand)
                {
                    Service.Chat.PrintError("Cannot print: No target is currently being tracked.");
                }
                return;
            }

            if (plugin.Tracker.LastResult == null)
            {
                if (fromCommand)
                {
                    Service.Chat.PrintError("Cannot print: The first threshold has not been evaluated yet.");
                }
                return;
            }

            string chatMessage = plugin.Tracker.CurrentPace switch
            {
                PacingState.TooFast => plugin.Configuration?.TooFastMessage ?? "Too Fast",
                PacingState.Behind => plugin.Configuration?.BehindMessage ?? "Behind",
                _ => plugin.Configuration?.OnTrackMessage ?? "On Track"
            };

            var diff = Math.Abs(plugin.Tracker.LastResult.Difference);
            chatMessage = chatMessage.Replace("{diff}", diff.ToString("F2"));

            var channel = plugin.Configuration?.OutputChannel ?? ChatChannel.Echo;

            var chatType = channel switch
            {
                ChatChannel.Party => XivChatType.Party,
                ChatChannel.Alliance => XivChatType.Alliance,
                ChatChannel.Say => XivChatType.Say,
                ChatChannel.Yell => XivChatType.Yell,
                ChatChannel.Shout => XivChatType.Shout,
                _ => XivChatType.Echo
            };

            Service.Chat.Print(new XivChatEntry
            {
                Type = chatType,
                Message = chatMessage
            });
        }
    }
}
