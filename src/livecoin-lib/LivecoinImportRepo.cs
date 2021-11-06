using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace livecoin_lib
{
    public class LivecoinImportRepo
    {
        public class LivecoinHistoryLineItem
        {
            // Type	Date	Credit	Debit	Fee	Correspondent
            public string Type { get; set; }
            public DateTime Date { get; set; }
            public decimal? CreditQuantity { get; set; }
            public string CreditSymbol { get; set; }
            public decimal? DebitQuantity { get; set; }
            public string DebitSymbol { get; set; }
            public decimal? FeeQuantity { get; set; }
            public string FeeSymbol { get; set; }
            public string TransactionId { get; set; }
            public string Wallet { get; set; }
        }

        public List<LivecoinHistoryLineItem> Get(string fileName)
        {
            var contents = File.ReadAllText(fileName);

            var lines = contents.Split('\r').Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
            var columnLineIndex = Enumerable.Range(0, lines.Count).Single(queryIndex => lines[queryIndex].ToUpper().StartsWith("TYPE"));
            var columnLine = lines[columnLineIndex];
            var columns = SplitByWhitespace(columnLine);

            var dataLineIndexes = Enumerable.Range(columnLineIndex + 1, lines.Count - columnLineIndex - 1)
                .ToList();

            var dataLines = dataLineIndexes.Select(queryIndex => lines[queryIndex]).ToList();

            return dataLines.Select(queryDataLine =>
            {
                var pieces = SplitByWhitespace(queryDataLine);
                var dateText = pieces[1];
                var dateSegments = dateText.Split('.');
                var year = int.Parse(dateSegments[2]);
                var day = int.Parse(dateSegments[0]);
                var month = int.Parse(dateSegments[1]);                               

                var timeText = pieces[2];
                var timeSegments = timeText.Split(':');
                var hour = int.Parse(timeSegments[0]);
                var minute = int.Parse(timeSegments[1]);
                var second = int.Parse(timeSegments[2]);

                var date = new DateTime(year, month, day, hour, minute, second);

                var parseIndexAsDecimal = new Func<int, decimal?>(index =>                
                    index < pieces.Count && decimal.TryParse(pieces[index], out decimal parsedVal)
                        ? parsedVal
                        : (decimal?)null
                );

                var getPiece = new Func<int, string>(index => index < pieces.Count ? pieces[index] : null);

                const string TransactionIndicator = "TxId:";
                var transactionIndex = Enumerable.Range(0, pieces.Count).Where(index =>
                {
                    return pieces[index] != null && pieces[index].ToUpper().StartsWith(TransactionIndicator.ToUpper());
                }).Select(queryIndex => (int?)queryIndex).SingleOrDefault();

                string transactionId = null;
                if (transactionIndex.HasValue)
                {
                    transactionId = pieces[transactionIndex.Value].Substring(TransactionIndicator.Length);
                    pieces[transactionIndex.Value] = null;
                }

                const string WalletIndicator = "Wallet:";
                var walletIndex = Enumerable.Range(0, pieces.Count).Where(index =>
                {
                    return pieces[index] != null && pieces[index].ToUpper().StartsWith(WalletIndicator.ToUpper());
                }).Select(queryIndex => (int?)queryIndex).SingleOrDefault();

                string wallet = null;
                if (walletIndex.HasValue)
                {
                    wallet = pieces[walletIndex.Value].Substring(WalletIndicator.Length);
                    pieces[walletIndex.Value] = null;
                }

                return new LivecoinHistoryLineItem
                {
                    Type = getPiece(0),
                    Date = date,
                    CreditQuantity = parseIndexAsDecimal(3),
                    CreditSymbol = getPiece(4),
                    DebitQuantity = parseIndexAsDecimal(5),
                    DebitSymbol = getPiece(6),
                    FeeQuantity = parseIndexAsDecimal(7),
                    FeeSymbol = getPiece(8),
                    TransactionId = transactionId,
                    Wallet = wallet
                };
            }).ToList();
        }


        private List<string> SplitByWhitespace(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return new List<string>(); }
            return text.Replace('\t', ' ').Split(' ').Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        }
    }
}
