using TaskQueue;

namespace WorldCollector.Taobao.TaskQueues
{
    public class TaobaoGetItemListTaskData : TaskData
    {
        public int Page { get; set; }
    }
}
