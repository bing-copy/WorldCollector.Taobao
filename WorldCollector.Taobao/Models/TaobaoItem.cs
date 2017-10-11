using System;
using System.ComponentModel.DataAnnotations;

namespace WorldCollector.Taobao.Models
{
    public class TaobaoItem
    {
        [Key]
        public string ItemId { get; set; }
        public DateTime LastCheckDt { get; set; }
    }
}
