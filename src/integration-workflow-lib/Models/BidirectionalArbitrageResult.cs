using trade_model;

namespace integration_workflow_lib.Models
{
    public class BidirectionalArbitrageResult
    {
        public ArbitrageResult ResultA { get; set; }
        public ArbitrageResult ResultB { get; set; }
    }
}
