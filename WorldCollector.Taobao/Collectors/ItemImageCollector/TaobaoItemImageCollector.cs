using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProxyProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.DownloadTaskQueue;
using WorldCollector.Taobao.Collectors.ItemImageCollector.TaskQueues;
using WorldCollector.Taobao.Models;
using WorldCollector.Taobao.TaskQueues;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector
{
    public abstract class TaobaoItemImageCollector : TaskQueuePool
    {
        private const string SearchUri = "/search.htm";
        private const string AsyncSearchUriElementId = "J_ShopAsynSearchURL";
        private const string PageParameterName = "pageNo";
        private readonly TaobaoItemImageCollectorOptions _options;
        private const string ProxyPurpose = "Taobao";

        private const string ItemUrlTemplate = "https://item.taobao.com/item.htm?id={0}";

        protected TaobaoItemImageCollector(TaobaoItemImageCollectorOptions options, ILoggerFactory loggerFactory) :
            base(
                new TaskQueuePoolOptions {MaxThreads = options.MaxThreads, MinInterval = options.MinInterval},
                loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _options = options;
        }

        /// <summary>
        /// By shopUrl.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<string> GetListUrlTemplate()
        {
            var client =
                await new HttpClientProvider(_options.HttpClientProviderDbConnectionString).GetClient(ProxyPurpose);
            var shopUri = new Uri(_options.ShopUrl);
            var searchAllUrl = new Uri(shopUri, SearchUri);
            var rsp = await client.GetAsync(searchAllUrl);
            var html = rsp.StatusCode == HttpStatusCode.Redirect
                ? await client.GetStringAsync(rsp.Headers.Location)
                : await rsp.Content.ReadAsStringAsync();
            var asyncSearchUri = new CQ(html)[$"#{AsyncSearchUriElementId}"].Val();
            if (string.IsNullOrEmpty(asyncSearchUri))
            {
                throw new ArgumentNullException(nameof(asyncSearchUri));
            }
            var asyncSearchUrl = new Uri(shopUri, asyncSearchUri).ToString();
            asyncSearchUrl += (asyncSearchUrl.Contains("?") ? "&" : "?") + $"{PageParameterName}={{0}}";
            return asyncSearchUrl;
        }

        public override async Task Start()
        {
            Func<TaobaoCollectorDbContext> getRecordDbFunc = null;
            if (!string.IsNullOrEmpty(_options.CrawlRecordDbConnectionString))
            {
                getRecordDbFunc = () =>
                    new TaobaoCollectorDbContext(new DbContextOptionsBuilder<TaobaoCollectorDbContext>()
                        .UseMySql(_options.CrawlRecordDbConnectionString).Options);
                await getRecordDbFunc().Database.MigrateAsync();
            }
            var listUrlTemplate = await GetListUrlTemplate();
            Add(new TaobaoGetItemListTaskQueue(new TaobaoGetItemListTaskQueueOptions
            {
                MaxThreads = _options.ListThreads,
                Interval = _options.ListInterval,
                UrlTemplate = listUrlTemplate,
                CrawlRecordDbFunc = getRecordDbFunc,
                HttpClientProviderDbConnectionString = _options.HttpClientProviderDbConnectionString,
                Purpose = ProxyPurpose
            }, LoggerFactory));

            Add(new TaobaoGetImageUrlListTaskQueue(new TaobaoGetImageUrlListTaskQueueOptions
            {
                MaxThreads = _options.ItemThreads,
                Interval = _options.ItemInterval,
                CrawlRecordDbFunc = getRecordDbFunc,
                GetImageTaskDataFunc = GetImageTaskDataFromDesc,
                HttpClientProviderDbConnectionString = _options.HttpClientProviderDbConnectionString,
                Purpose = ProxyPurpose,
                UrlTemplate = ItemUrlTemplate
            }, LoggerFactory));

            Add(new DownloadImageTaskQueue(new DownloadImageTaskQueueOptions
            {
                MaxThreads = _options.DownloadThreads,
                Interval = _options.DownloadInterval,
                DownloadPath = _options.DownloadPath,
                Filter = FilterImage,
                HttpClientProviderDbConnectionString = _options.HttpClientProviderDbConnectionString,
                Purpose = ProxyPurpose
            }, LoggerFactory));
            Enqueue(new TaobaoGetItemListTaskData {Page = 1});
            await base.Start();
        }

        protected abstract Task<bool> FilterImage(Stream image);

        protected abstract Task<List<DownloadImageTaskData>> GetImageTaskDataFromDesc(string title, CQ descCq);
    }
}