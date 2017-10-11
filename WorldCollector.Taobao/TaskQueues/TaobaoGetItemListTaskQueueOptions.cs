using System;
using TaskQueue.CommonTaskQueues.SpiderTaskQueue;
using WorldCollector.Taobao.Models;

namespace WorldCollector.Taobao.TaskQueues
{
    public class TaobaoGetItemListTaskQueueOptions : SpiderTaskQueueOptions
    {
        public string UrlTemplate { get; set; }
        public Func<TaobaoCollectorDbContext> CrawlRecordDbFunc { get; set; }
    }
}