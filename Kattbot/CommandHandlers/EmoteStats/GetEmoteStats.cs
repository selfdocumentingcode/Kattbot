using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.CommandModules;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helper;
using Kattbot.Helpers;
using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.CommandHandlers.EmoteStats
{
    public class GetEmoteStats
    {
        public class GetEmoteStatsRequest : CommandRequest
        {
            public TempEmote Emote { get; set; } = null!;
            public DateTime? FromDate { get; set; }

            public GetEmoteStatsRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class GetEmoteStatsHandler : AsyncRequestHandler<GetEmoteStatsRequest>
        {
            private const int MaxUserCount = 10;

            private readonly EmoteStatsRepository _emoteStatsRepo;

            public GetEmoteStatsHandler(
                EmoteStatsRepository emoteStatsRepo
                )
            {
                _emoteStatsRepo = emoteStatsRepo;
            }

            protected override async Task Handle(GetEmoteStatsRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;
                var emote = request.Emote;
                var fromDate = request.FromDate;

                var guildId = ctx.Guild.Id;

                var emoteUsageResult = await _emoteStatsRepo.GetSingleEmoteStats(guildId, emote, MaxUserCount, fromDate);

                if (emoteUsageResult == null)
                {
                    await ctx.RespondAsync("No stats yet");
                    return;
                }

                var emoteStats = emoteUsageResult.EmoteStats;
                var emoteUsers = emoteUsageResult.EmoteUsers;

                var emoteCode = EmoteHelper.BuildEmoteCode(emoteStats.EmoteId, emoteStats.IsAnimated);
                var totalUsage = emoteStats.Usage;

                var title = $"Stats for `{emoteCode}`";

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
                                                PercentageOfTotal = (double)r.Usage / totalUsage

                                            })
                                            .ToList();

                    // Resolve display names
                    foreach (var emoteUser in extendedEmoteUsers)
                    {
                        DiscordMember user;

                        if (ctx.Guild.Members.ContainsKey(emoteUser.UserId))
                        {
                            user = ctx.Guild.Members[emoteUser.UserId];
                        }
                        else
                        {
                            try
                            {
                                user = await ctx.Guild.GetMemberAsync(emoteUser.UserId);
                            }
                            catch
                            {
                                user = null!;
                            }
                        }

                        if (user != null)
                        {
                            emoteUser.DisplayName = user.GetNicknameOrUsername();
                        }
                        else
                        {
                            emoteUser.DisplayName = "Unknown user";
                        }
                    }

                    result.AppendLine();
                    result.AppendLine("Top users");

                    var lines = FormattedResultHelper.FormatExtendedEmoteUsers(extendedEmoteUsers, 0);

                    lines.ForEach(l => result.AppendLine(l));
                }

                var formattedResultMessage = $"`{result}`";

                await ctx.RespondAsync(formattedResultMessage);
            }
        }
    }
}
