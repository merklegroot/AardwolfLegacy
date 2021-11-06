using System;
using System.Collections.Generic;
using System.Linq;

namespace hitbtc_lib.Models
{
    public class HitBtcHealthStatusItem
    {
        public HitBtcHealthStatusItem() { }
        public HitBtcHealthStatusItem(List<List<string>> megaCells)
        {
            if (megaCells == null || !megaCells.Any()) { return; }

            Symbol = JoinMega(megaCells, 0);
            DepositStatusText = JoinMega(megaCells, 1);
            PendingDepositsText = JoinMega(megaCells, 2);
            LastSuccessfulDepositDateTimeText = JoinMega(megaCells, 3);
            TransfersStatusText = JoinMega(megaCells, 4);
            WithdrawalStatusText = JoinMega(megaCells, 6);
            PendingWithdrawalsText = JoinMega(megaCells, 8);
            ProcessingTimeForLast100TransactionsText = JoinMega(megaCells, 10);
            ProcessingTimeLowText = GetProcessingTimeForLine("low");
            ProcessingTimeLow = ParseHitBtcTimeSpan(ProcessingTimeLowText);
            ProcessingTimeHighText = GetProcessingTimeForLine("high");
            ProcessingTimeHigh = ParseHitBtcTimeSpan(ProcessingTimeHighText);
            ProcessingTimeAverageText = GetProcessingTimeForLine("avg");
            ProcessingTimeAverage = ParseHitBtcTimeSpan(ProcessingTimeAverageText);
        }

        private string JoinMega(List<List<string>> megaCells, int index)
        {
            return megaCells != null && megaCells.Count > index
                ? string.Join(Environment.NewLine, megaCells[index])
                : null;
        }

        public string Symbol { get; set; }

        public string DepositStatusText { get; set; }

        public string WithdrawalStatusText { get; set; }

        public string PendingDepositsText { get; set; }        

        public string LastSuccessfulDepositDateTimeText { get; set; }
        
        public string TransfersStatusText { get; set; }        

        public string PendingWithdrawalsText { get; set; }

        public string ProcessingTimeForLast100TransactionsText { get; set; }

        public string ProcessingTimeLowText { get; set; }

        public TimeSpan? ProcessingTimeLow { get; set; }
        
        public string ProcessingTimeHighText { get; set; }

        public TimeSpan? ProcessingTimeHigh { get; set; }

        public string ProcessingTimeAverageText { get; set; }

        public TimeSpan? ProcessingTimeAverage { get; set; }

        private string GetProcessingTimeForLine(string indicator)
        {
            var lines = (ProcessingTimeForLast100TransactionsText ?? string.Empty)
                .Replace("\r\n", "\r").Replace("\n", "\r").Split('\r')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList();

            foreach (var line in lines)
            {
                var pieces = line.Split(':').Select(item => item.Trim()).ToList();
                if (pieces.Count != 2) { continue; }
                if (pieces[0].ToUpper().StartsWith(indicator.ToUpper()))
                {
                    return pieces[1];
                }
            }

            return null;
        }

        private TimeSpan? ParseHitBtcTimeSpan(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return null; }

            var pieces = text.Split(' ')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList();

            if (pieces.Count != 2) { return null; }

            double quantity;
            if (!double.TryParse(pieces[0], out quantity)) { return null; }


            if (string.Equals(pieces[1], "sec", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(pieces[1], "secs", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromSeconds(quantity);
            }

            if (string.Equals(pieces[1], "min", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(pieces[1], "mins", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromMinutes(quantity);
            }

            if (string.Equals(pieces[1], "hour", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(pieces[1], "hours", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromHours(quantity);
            }

            if (string.Equals(pieces[1], "day", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(pieces[1], "days", StringComparison.InvariantCultureIgnoreCase))
            {
                return TimeSpan.FromDays(quantity);
            }

            return null;
        }
    }
}
