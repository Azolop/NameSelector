using System.Collections.Generic;
using Newtonsoft.Json;

namespace NameSelector.Models
{
    /// <summary>
    /// data.json 的根数据。
    /// </summary>
    public class AppData
    {
        [JsonProperty("Students")]
        public List<Student> Students { get; set; } = new List<Student>();

        /// <summary>下一个点名次序，初始 1。</summary>
        [JsonProperty("NextOrder")]
        public int NextOrder { get; set; } = 1;
    }
}
