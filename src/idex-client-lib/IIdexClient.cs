using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idex_client_lib
{
    public interface IIdexClient
    {
        string GetOrderBookRaw(string nativeSymbol, string nativeBaseSymbol);
    }
}
