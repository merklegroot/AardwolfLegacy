using System;

namespace bit_z_lib.Repos
{
    public interface IBitzCoinListRepo
    {
        void Update(DateTime timeStampUtc, string contents);
    }

    public class BitzCoinListRepo : IBitzCoinListRepo
    {
        public void Update(DateTime timeStampUtc, string contents)
        {
            throw new NotImplementedException();
        }
    }
}
