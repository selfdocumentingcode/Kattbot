using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using Kattbot.CommandModules.ResultFormatters;
using Kattbot.Common.Models;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using MediatR;

namespace Kattbot.CommandHandlers.EmoteStats;

public class GetUserEmoteStats
{
    public class GetUserEmoteStatsRequest : CommandRequest
    {
        public GetUserEmoteStatsRequest(CommandContext ctx)
            : base(ctx)
        { }

        public ulong UserId { get; set; }

        public string Mention { get; set; } = null!;

        public int Page { get; set; }

        public DateTime? FromDate { get; set; }
    }

    public class GetUserEmoteStatsHandler : IRequestHandler<GetUserEmoteStatsRequest>
    {
        private const int ResultsPerPage = 10;

        private readonly EmoteStatsRepository _emoteStatsRepo;

        public GetUserEmoteStatsHandler(EmoteStatsRepository emoteStatsRepo)
        {
            _emoteStatsRepo = emoteStatsRepo;
        }

        public async Task Handle(GetUserEmoteStatsRequest request, CancellationToken cancellationToken)
        {
            CommandContext ctx = request.Ctx;
            int page = request.Page;
            DateTime? fromDate = request.FromDate;
            ulong userId = request.UserId;
            string mention = request.Mention;

            ulong guildId = ctx.Guild.Id;

            int pageOffset = page - 1;

            PaginatedResult<ExtendedStatsQueryResult> emoteUsageResult =
                await _emoteStatsRepo.GetBestEmotesForUser(guildId, userId, pageOffset, ResultsPerPage, fromDate);

            List<ExtendedStatsQueryResult> emoteUsageItems = emoteUsageResult.Items;
            int safePageOffset = emoteUsageResult.PageOffset;
            int pageCount = emoteUsageResult.PageCount;
            int totalCount = emoteUsageResult.TotalCount;

            List<ExtendedEmoteStats> emoteUsageList = emoteUsageItems
                .Select(
                    r => new ExtendedEmoteStats
                    {
                        EmoteCode = EmoteHelper.BuildEmoteCode(r.EmoteId, r.IsAnimated),
                        Usage = r.Usage,
                        PercentageOfTotal = (double)r.Usage / r.TotalUsage,
                    })
                .ToList();

            if (emoteUsageList.Count == 0)
            {
                await ctx.RespondAsync("No stats yet");
                return;
            }

            string rangeText;

            if (safePageOffset == 0)
            {
                rangeText = ResultsPerPage.ToString();
            }
            else
            {
                int rangeMin = ResultsPerPage * safePageOffset + 1;
                int rangeMax = Math.Min(ResultsPerPage * safePageOffset + ResultsPerPage, totalCount);
                rangeText = $"{rangeMin} - {rangeMax}";
            }

            var title = $"Top {rangeText} emotes for {mention}";

            if (fromDate != null)
            {
                title += $" from {fromDate.Value:yyyy-MM-dd}";
            }

            int rankOffset = safePageOffset * ResultsPerPage;

            List<string> lines = FormattedResultHelper.FormatExtendedEmoteStats(emoteUsageList, rankOffset);

            string body = FormattedResultHelper.BuildBody(lines);

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
