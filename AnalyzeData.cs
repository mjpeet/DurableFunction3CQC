using Newtonsoft.Json;

namespace DurableFunction3CQC
{
    public class AnalyzeData
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("locations")]
        public List<PartialPostCodeCount> data { get; set; }
    }

    public class PartialPostCodeCount
    {
        [JsonProperty("postcodeprefix")]
        public string PostCodePrefix { get; set; }

        [JsonProperty("count")]
        public string Count { get; set; }


    }
}
