using config_connection_string_lib;
using etherscan_lib.Models;
using mongo_lib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using parse_lib;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace etherscan_lib
{
    public class EtherscanHoldingRepo : IEtherscanHoldingRepo
    {
        private const string DefaultDatabaseName = "etherscan";
        private readonly string _databaseName;

        private readonly IGetConnectionString _getConnectionString;

        public EtherscanHoldingRepo(IGetConnectionString connectionStringGetter) : this(connectionStringGetter, null)
        {
        }

        public EtherscanHoldingRepo(IGetConnectionString connectionStringGetter, string databaseName = null)
        {
            _databaseName = !string.IsNullOrWhiteSpace(databaseName) ? databaseName.Trim() : DefaultDatabaseName;
            _getConnectionString = connectionStringGetter;
        }

        private IMongoCollectionContext TokenHoldingsCollectionContext
        {
            get { return new MongoCollectionContext(_getConnectionString.GetConnectionString(), _databaseName, "etherscan--token-holdings"); }
        }

        public void Insert(EtherScanTokenHoldingContainer container)
        {
            TokenHoldingsCollectionContext.Insert(container);
        }

        public HoldingInfo Get()
        {
            var info = new HoldingInfo();

            var native = GetNative();
            if (native == null)
            {
                return info;
            }

            info.TimeStampUtc = native.TimeStampUtc;
            info.Holdings = native.Rows.Select(item => RowToHolding(item))
                .Where(item => item != null).ToList();

            return info;
        }

        private Holding RowToHolding(List<string> row)
        {
            if (row == null || row.Count < 7) { return null; }

            var holding = new Holding();
            
            var assetText = row[1];
            var quantityCell = row[3];
            var quantityPieces = (quantityCell ?? string.Empty).Trim().Split(' ')
                .Where(piece => !string.IsNullOrWhiteSpace(piece))
                .Select(piece => piece.Trim())
                .ToList();

            if (quantityPieces.Count >= 2)
            {
                var quantityAmountText = quantityPieces[0];
                var quantity = ParseUtil.DecimalTryParse(quantityPieces[0]);
                var symbol = quantityPieces[1];

                holding.Asset = symbol;
                holding.Total = quantity ?? 0;
                holding.Available = quantity ?? 0;

                return holding;
            }

            return null;
        }

        private EtherScanTokenHoldingContainer GetNative()
        {
            return TokenHoldingsCollectionContext.GetCollection<EtherScanTokenHoldingContainer>()
                .AsQueryable()
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();
        }

    }
}
