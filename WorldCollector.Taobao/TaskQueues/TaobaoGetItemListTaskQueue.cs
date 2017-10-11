using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskQueue;
using TaskQueue.CommonTaskQueues.SpiderTaskQueue;

namespace WorldCollector.Taobao.TaskQueues
{
    public class
        TaobaoGetItemListTaskQueue : SpiderTaskQueue<TaobaoGetItemListTaskQueueOptions, TaobaoGetItemListTaskData>
    {
        public TaobaoGetItemListTaskQueue(TaobaoGetItemListTaskQueueOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {
        }

        protected override async Task<List<TaskData>> ExecuteAsyncInternal(TaobaoGetItemListTaskData taskData)
        {
            var client = await GetHttpClient();
            var url = string.Format(Options.UrlTemplate, taskData.Page);
            var rsp = await client.GetAsync(url);
            var html = rsp.StatusCode == HttpStatusCode.Redirect
                ? await client.GetStringAsync(rsp.Headers.Location)
                : await rsp.Content.ReadAsStringAsync();
            var cq = new CQ(html.Replace("\\\"", "\""));
            var searchResultSpan = cq[".search-result span"];
            var count = int.Parse(searchResultSpan.Text().Trim());
            if (count > 0)
            {
                var itemIds = cq[".shop-filter"].NextAll().Children(".item").Select(t => t.GetAttribute("data-id"))
                    .ToList();
                if (itemIds.Any())
                {
                    var itemTaskData = new List<TaskData> {new TaobaoGetItemListTaskData {Page = taskData.Page + 1}};
                    if (Options.CrawlRecordDbFunc != null)
                    {
                        var db = Options.CrawlRecordDbFunc();
                        var soonestCheckDt = DateTime.Now.AddDays(-7);
                        var skippedItems = await db.TaobaoItems
                            .Where(t => itemIds.Contains(t.ItemId) && t.LastCheckDt > soonestCheckDt)
                            .Select(a => a.ItemId).ToListAsync();
                        itemIds.RemoveAll(t => skippedItems.Contains(t));
                    }
                    if (itemIds.Any())
                    {
                        itemTaskData.AddRange(itemIds
                            .Select(t => new TaobaoGetItemTaskData {ItemId = t}).ToList());
                    }
                    return itemTaskData;
                }
                return null;
            }
            else
            {
                Finished = true;
                return null;
            }
        }
    }
}