using System;
using System.Collections.Generic;

namespace balance_lib
{
    public class HoldingInfoViewModel
    {
        public string Exchange { get; set; }

        public DateTime? TimeStampUtc { get; set; }

        public List<HoldingWithValueViewModel> Holdings { get; set; }

        public decimal TotalValue { get; set; }
    }
}
