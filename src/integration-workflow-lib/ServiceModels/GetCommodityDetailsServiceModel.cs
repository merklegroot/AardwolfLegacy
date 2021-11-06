namespace integration_workflow_lib
{
    public class GetCommodityDetailsServiceModel
    {
        public string Symbol { get; set; }

        public string CachePolicy { get; set; }

        public bool ForceRefresh { get; set; }
    }
}
