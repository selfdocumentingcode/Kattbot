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
using MediatR;

namespace Kattbot.CommandHandlers.EmoteStats;

public class GetGuildEmoteStats
{
    public class GetGuildEmoteStatsRequest : CommandRequest
    {
        public SortDirection SortDirection { get; set; }

        public int Page { get; set; }

        public DateTime? FromDate { get; set; }

        public IReadOnlyDictionary<ulong, DiscordEmoji> GuildEmojis { get; set; } = null!;

        public GetGuildEmoteStatsRequest(CommandContext ctx)
            : base(ctx)
        {
        }
    }

    public class GetGuildEmoteStatsHandler : AsyncRequestHandler<GetGuildEmoteStatsRequest>
    {
        private const int _resultsPerPage = 10;

        private readonly EmoteStatsRepository _emoteStatsRepo;

        public GetGuildEmoteStatsHandler(
            EmoteStatsRepository emoteStatsRepo)
        {
            _emoteStatsRepo = emoteStatsRepo;
        }

        protected override async Task Handle(GetGuildEmoteStatsRequest request, CancellationToken cancellationToken)
        {
            CommandContext ctx = request.Ctx;
            int page = request.Page;
            DateTime? fromDate = request.FromDate;
            IReadOnlyDictionary<ulong, DiscordEmoji> guildEmotes = request.GuildEmojis;
            SortDirection direction = request.SortDirection;

            ulong guildId = ctx.Guild.Id;

            int pageOffset = page - 1;

            var mappedGuildEmotes = guildEmotes.Select(kv => new TempEmote()
            {
                Id = kv.Key,
                Name = kv.Value.Name,
                Animated = kv.Value.IsAnimated,
            }).ToList();

            Models.PaginatedResult<Common.Models.Emotes.EmoteStats> emoteUsageResult = await _emoteStatsRepo.GetGuildEmoteStats(guildId, direction, mappedGuildEmotes, pageOffset: pageOffset, perPage: _resultsPerPage, fromDate: fromDate);

            List<Common.Models.Emotes.EmoteStats> emoteUsageItems = emoteUsageResult.Items;
            int safePageOffset = emoteUsageResult.PageOffset;
            int pageCount = emoteUsageResult.PageCount;
            int totalCount = emoteUsageResult.TotalCount;

            if (emoteUsageItems.Count == 0)
            {
                await ctx.RespondAsync("No stats yet");

                return;
            }

            string bestOrWorst = direction == SortDirection.ASC ? "worst" : "best";

            string rangeText;

            if (safePageOffset == 0)
            {
                rangeText = _resultsPerPage.ToString();
            }
            else
            {
                int rangeMin = (_resultsPerPage * safePageOffset) + 1;
                int rangeMax = Math.Min((_resultsPerPage * safePageOffset) + _resultsPerPage, totalCount);
                rangeText = $"{rangeMin} - {rangeMax}";
            }

            string title = $"Top {rangeText} {bestOrWorst} emotes";

            if (fromDate != null)
            {
                title += $" from {fromDate.Value:yyyy-MM-dd}";
            }

            int rankOffset = safePageOffset * _resultsPerPage;

            List<string> lines = FormattedResultHelper.FormatEmoteStats(emoteUsageItems, rankOffset);

            string body = FormattedResultHelper.BuildBody(lines);

            StringBuilder result = new();

            result.AppendLine(title);
            result.AppendLine();
            result.AppendLine(body);

            if (pageCount > 1)
            {
                result.AppendLine();

                string pagingText = $"Page {safePageOffset + 1}/{pageCount}";

                if (safePageOffset + 1 < pageCount)
                {
                    pagingText += $" (use -p {safePageOffset + 2} to view next page)";
                }

                result.AppendLine(pagingText);
            }

            string formattedResultMessage = $"`{result}`";

            await ctx.RespondAsync(formattedResultMessage);
        }
    }
}
