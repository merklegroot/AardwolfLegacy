﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trade_contracts.Messages.Exchange
{
    public class GetCachedOrderBooksRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
    }
}
