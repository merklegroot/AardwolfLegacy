namespace config_model
{
    public class CossAgentConfig
    {
        private const decimal MinimumEthThreshold = 0.5m;
        private const decimal DefaultEthThreshold = 1.2m;

        private const decimal MinimumTokenThreshold = 0.5m;
        private const decimal DefaultTokenThreshold = 3.5m;

        public bool IsCossAutoTradingEnabled { get; set; }

        private decimal _ethThreshold = DefaultEthThreshold;
        public decimal EthThreshold
        {
            get { return _ethThreshold >= MinimumEthThreshold ? _ethThreshold : MinimumEthThreshold; }
            set { _ethThreshold = (value >= MinimumEthThreshold) ? value : MinimumEthThreshold; }
        }

        private decimal _tokenThreshold = DefaultTokenThreshold;
        public decimal TokenThreshold
        {
            get { return _tokenThreshold >= MinimumTokenThreshold ? _tokenThreshold : MinimumTokenThreshold; }
            set { _tokenThreshold = (value >= MinimumTokenThreshold) ? value : MinimumTokenThreshold; }
        }
    }
}
