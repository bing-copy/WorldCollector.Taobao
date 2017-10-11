using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskQueue;
using TaskQueue.CommonTaskQueues.SpiderTaskQueue;
using WorldCollector.Taobao.Models;
using WorldCollector.Taobao.TaskQueues;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector.TaskQueues
{
    public class
        TaobaoGetImageUrlListTaskQueue : SpiderTaskQueue<TaobaoGetImageUrlListTaskQueueOptions,
            TaobaoGetItemTaskData>
    {
        public TaobaoGetImageUrlListTaskQueue(TaobaoGetImageUrlListTaskQueueOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {
        }



        protected override async Task<List<TaskData>> ExecuteAsyncInternal(TaobaoGetItemTaskData taskData)
        {
            var client = await GetHttpClient();
            var url = string.Format(Options.UrlTemplate, taskData.ItemId);
            var html = await client.GetStringAsync(url);
            var cq = new CQ(html);
            var title = cq["#J_Title h3"].Attr("data-title").Trim();
            //china url
            var match = Regex.Match(html, @"descUrl\s*\:\slocation.*(?<url>\/\/.*?)',").Groups["url"];
            //world url
            if (!match.Success)
            {
                match = Regex.Match(html, "descUrlSSL\\s*\\:\\s*\"(?<url>\\/\\/.*?)\".*").Groups["url"];
            }
            var descUrl = match.Value;
            if (descUrl.StartsWith("//"))
            {
                descUrl = $"https:{descUrl}";
            }
            var descJsonp = await client.GetStringAsync(descUrl);
            var descHtml = Regex.Match(descJsonp, @"var\s*desc\s*=\s*'\s*(?<html>[\s\S]*)\s*'\s*;")
                .Groups["html"].Value;
            var descCq = new CQ(descHtml);
            var images = (await Options.GetImageTaskDataFunc(title, descCq)).Cast<TaskData>().ToList();

            if (Options.CrawlRecordDbFunc != null)
            {
                var db = Options.CrawlRecordDbFunc();
                var record = await db.TaobaoItems.FirstOrDefaultAsync(t => t.ItemId == taskData.ItemId);
                if (record == null)
                {
                    record = new TaobaoItem
                    {
                        ItemId = taskData.ItemId
                    };
                    db.Add(record);
                }
                record.LastCheckDt = DateTime.Now;
                await db.SaveChangesAsync();
            }

            return images;
        }
    }
}