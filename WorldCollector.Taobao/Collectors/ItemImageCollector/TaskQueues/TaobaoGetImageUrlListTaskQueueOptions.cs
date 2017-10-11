using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsQuery;
using TaskQueue.CommonTaskQueues.DownloadTaskQueue;
using TaskQueue.CommonTaskQueues.SpiderTaskQueue;
using WorldCollector.Taobao.Models;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector.TaskQueues
{
    public class TaobaoGetImageUrlListTaskQueueOptions : SpiderTaskQueueOptions
    {
        public string UrlTemplate { get; set; }
        public Func<string, CQ, Task<List<DownloadImageTaskData>>> GetImageTaskDataFunc { get; set; }
        public Func<TaobaoCollectorDbContext> CrawlRecordDbFunc { get; set; }
    }
}
