using Newtonsoft.Json;

namespace DurableFunction3CQC
{
    public class ApiResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("firstPageUri")]
        public string FirstPageUri { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("previousPageUri")]
        public object PreviousPageUri { get; set; }

        [JsonProperty("lastPageUri")]
        public string LastPageUri { get; set; }

        [JsonProperty("nextPageUri")]
        public string NextPageUri { get; set; }

        [JsonProperty("perPage")]
        public int PerPage { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("locations")]
        public List<Location> Locations { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class Location
    {
        [JsonProperty("locationId")]
        public string LocationId { get; set; }

        [JsonProperty("locationName")]
        public string LocationName { get; set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }
    }
}
