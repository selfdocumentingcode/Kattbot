using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.CommandModules;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helper;
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
    public class GetUserEmoteStats
    {
        public class GetUserEmoteStatsRequest : CommandRequest
        {
            public ulong UserId { get; set; }
            public string Mention { get; set; } = null!;
            public int Page { get; set; }
            public DateTime? FromDate { get; set; }

            public GetUserEmoteStatsRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class GetUserEmoteStatsHandler : AsyncRequestHandler<GetUserEmoteStatsRequest>
        {
            private const int ResultsPerPage = 10;

            private readonly EmoteStatsRepository _emoteStatsRepo;

            public GetUserEmoteStatsHandler(
                EmoteStatsRepository emoteStatsRepo
                )
            {
                _emoteStatsRepo = emoteStatsRepo;
            }

            protected override async Task Handle(GetUserEmoteStatsRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;                
                var page = request.Page;
                var fromDate = request.FromDate;
                var userId = request.UserId;
                var mention = request.Mention;

                var guildId = ctx.Guild.Id;

                int pageOffset = page - 1;

                var emoteUsageResult = await _emoteStatsRepo.GetBestEmotesForUser(guildId, userId, pageOffset, ResultsPerPage, fromDate);

                var emoteUsageItems = emoteUsageResult.Items;
                var safePageOffset = emoteUsageResult.PageOffset;
                var pageCount = emoteUsageResult.PageCount;
                var totalCount = emoteUsageResult.TotalCount;

                var emoteUsageList = emoteUsageItems
                    .Select(r => new ExtendedEmoteStats()
                    {
                        EmoteCode = EmoteHelper.BuildEmoteCode(r.EmoteId, r.IsAnimated),
                        Usage = r.Usage,
                        PercentageOfTotal = (double)r.Usage / r.TotalUsage

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
                    var rangeMin = ResultsPerPage * safePageOffset + 1;
                    var rangeMax = Math.Min(ResultsPerPage * safePageOffset + ResultsPerPage, totalCount);
                    rangeText = $"{rangeMin} - {rangeMax}";
                }

                var title = $"Top {rangeText} emotes for {mention}";

                if (fromDate != null)
                {
                    title += $" from {fromDate.Value:yyyy-MM-dd}";
                }

                var rankOffset = safePageOffset * ResultsPerPage;

                var lines = FormattedResultHelper.FormatExtendedEmoteStats(emoteUsageList, rankOffset);

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
