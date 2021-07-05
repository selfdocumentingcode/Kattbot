using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.CommandModules;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
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
    public class GetGuildEmoteStats
    {
        public class GetGuildEmoteStatsRequest : CommandRequest
        {
            public SortDirection SortDirection { get; set; }
            public int Page { get; set; }
            public DateTime? FromDate { get; set; }
            public IReadOnlyDictionary<ulong, DiscordEmoji> GuildEmojis { get; set; } = null!;

            public GetGuildEmoteStatsRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class GetGuildEmoteStatsHandler : AsyncRequestHandler<GetGuildEmoteStatsRequest>
        {
            private const int ResultsPerPage = 10;

            private readonly EmoteStatsRepository _emoteStatsRepo;

            public GetGuildEmoteStatsHandler(
                EmoteStatsRepository emoteStatsRepo
                )
            {
                _emoteStatsRepo = emoteStatsRepo;
            }

            protected override async Task Handle(GetGuildEmoteStatsRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;
                var page = request.Page;
                var fromDate = request.FromDate;
                var guildEmotes = request.GuildEmojis;
                var direction = request.SortDirection;

                var guildId = ctx.Guild.Id;

                int pageOffset = page - 1;

                var mappedGuildEmotes = guildEmotes.Select(kv => new TempEmote()
                {
                    Id = kv.Key,
                    Name = kv.Value.Name,
                    Animated = kv.Value.IsAnimated
                }).ToList();

                var emoteUsageResult = await _emoteStatsRepo.GetGuildEmoteStats(guildId, direction, mappedGuildEmotes, pageOffset: pageOffset, perPage: ResultsPerPage, fromDate: fromDate);

                var emoteUsageItems = emoteUsageResult.Items;
                var safePageOffset = emoteUsageResult.PageOffset;
                var pageCount = emoteUsageResult.PageCount;
                var totalCount = emoteUsageResult.TotalCount;

                if (emoteUsageItems.Count == 0)
                {
                    await ctx.RespondAsync("No stats yet");

                    return;
                }

                var bestOrWorst = direction == SortDirection.ASC ? "worst" : "best";

                string rangeText;

                if (safePageOffset == 0)
                {
                    rangeText = ResultsPerPage.ToString();
                }
                else
                {
                    var rangeMin = ResultsPerPage * safePageOffset + 1;
                    var rangeMax = Math.Min(ResultsPerPage * safePageOffset + ResultsPerPage, totalCount);
                    rangeText = $"{rangeMin} - {rangeMax}";
                }

                var title = $"Top {rangeText} {bestOrWorst} emotes";

                if (fromDate != null)
                {
                    title += $" from {fromDate.Value:yyyy-MM-dd}";
                }

                var rankOffset = safePageOffset * ResultsPerPage;

                var lines = FormattedResultHelper.FormatEmoteStats(emoteUsageItems, rankOffset);

                var body = FormattedResultHelper.BuildBody(lines);

                var result = new StringBuilder();

                result.AppendLine(title);
                result.AppendLine();
                result.AppendLine(body);

                if (pageCount > 1)
                {
                    result.AppendLine();

                    var pagingText = $"Page {safePageOffset + 1}/{pageCount}";

                    if (safePageOffset + 1 < pageCount)
                    {
                        pagingText += $" (use -p {safePageOffset + 2} to view next page)";
                    }

                    result.AppendLine(pagingText);
                }

                var formattedResultMessage = $"`{result}`";

                await ctx.RespondAsync(formattedResultMessage);
            }
        }
    }
}
