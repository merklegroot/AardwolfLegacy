using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace bit_z_lib
{
    [Obsolete]
    public class BitzOpenOrder : List<decimal>
    {
        [JsonIgnore]
        public decimal Price
        {
            get { return GetIndexValue(0); }
            set { SetIndexValue(0, value); }
        }

        [JsonIgnore]
        public decimal Quantity
        {
            get { return GetIndexValue(1); }
            set { SetIndexValue(1, value); }
        }

        private decimal GetIndexValue(int index)
        {
            return Count >= index + 1 ? this[index] : default(decimal);
        }

        private void SetIndexValue(int index, decimal value)
        {
            while (Count < index + 1) { Add(default(decimal)); }
            if (Count < index + 1) { Add(value); return; }
            this[index] = value;
        }
    }
}
