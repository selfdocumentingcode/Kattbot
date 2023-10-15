using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.CommandModules.ResultFormatters;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helpers;
using MediatR;

namespace Kattbot.CommandHandlers.EmoteStats;

public class GetEmoteStats
{
    public class GetEmoteStatsRequest : CommandRequest
    {
        public TempEmote Emote { get; set; } = null!;

        public DateTime? FromDate { get; set; }

        public GetEmoteStatsRequest(CommandContext ctx)
            : base(ctx)
        {
        }
    }

    public class GetEmoteStatsHandler : IRequestHandler<GetEmoteStatsRequest>
    {
        private const int MaxUserCount = 10;

        private readonly EmoteStatsRepository _emoteStatsRepo;

        public GetEmoteStatsHandler(
            EmoteStatsRepository emoteStatsRepo)
        {
            _emoteStatsRepo = emoteStatsRepo;
        }

        public async Task Handle(GetEmoteStatsRequest request, CancellationToken cancellationToken)
        {
            CommandContext ctx = request.Ctx;
            TempEmote emote = request.Emote;
            DateTime? fromDate = request.FromDate;

            ulong guildId = ctx.Guild.Id;

            EmoteUsageResult emoteUsageResult = await _emoteStatsRepo.GetSingleEmoteStats(guildId, emote, MaxUserCount, fromDate);

            if (emoteUsageResult == null)
            {
                await ctx.RespondAsync("No stats yet");
                return;
            }

            Common.Models.Emotes.EmoteStats emoteStats = emoteUsageResult.EmoteStats;
            List<EmoteUser> emoteUsers = emoteUsageResult.EmoteUsers;

            string emoteCode = EmoteHelper.BuildEmoteCode(emoteStats.EmoteId, emoteStats.IsAnimated);
            int totalUsage = emoteStats.Usage;

            string title = $"Stats for `{emoteCode}`";

            if (fromDate != null)
            {
                title += $" from {fromDate.Value:yyyy-MM-dd}";
            }

            var result = new StringBuilder();

            result.AppendLine(title);
            result.AppendLine($"Total usage: {totalUsage}");

            if (emoteUsers.Count > 0)
            {
                var extendedEmoteUsers = emoteUsers
                                        .Select(r => new ExtendedEmoteUser()
                                        {
                                            UserId = r.UserId,
                                            Usage = r.Usage,
                                            PercentageOfTotal = (double)r.Usage / totalUsage,
                                        })
                                        .ToList();

                // Resolve display names
                foreach (ExtendedEmoteUser? emoteUser in extendedEmoteUsers)
                {
                    DiscordMember member;

                    if (ctx.Guild.Members.ContainsKey(emoteUser.UserId))
                    {
                        member = ctx.Guild.Members[emoteUser.UserId];
                    }
                    else
                    {
                        try
                        {
                            member = await ctx.Guild.GetMemberAsync(emoteUser.UserId);
                        }
                        catch
                        {
                            member = null!;
                        }
                    }

                    emoteUser.DisplayName = member?.DisplayName ?? "Unknown user";
                }

                result.AppendLine();
                result.AppendLine("Top users");

                List<string> lines = FormattedResultHelper.FormatExtendedEmoteUsers(extendedEmoteUsers, 0);

                lines.ForEach(l => result.AppendLine(l));
            }

            string formattedResultMessage = $"`{result}`";

            await ctx.RespondAsync(formattedResultMessage);
        }
    }
}
