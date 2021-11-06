using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;
using trade_res;

namespace trade_lib
{
    public class DepositAddressValidator : IDepositAddressValidator
    {
        private static List<string> EthTokenSymbols = new List<string>
        {
            "ETH", "EOS", "KNC", "VEN", "SNM", "POE", "LINK", "WTC", "SUB", "CVC", "BLZ", "ENJ", "REQ",
            "PRL", "FOTA", "TUSD", "GUSD", "USDC", "PIX"
        };

        public void Validate(Commodity commodity, DepositAddress depositAddress)
        {
            if (commodity.IsEth || (commodity.IsEthToken.HasValue && commodity.IsEthToken.Value))
            {
                ValidateEthOrEthTokenAddress(depositAddress);
                return;
            }

            Validate(commodity.Symbol, depositAddress);
        }

        public void Validate(string symbol, DepositAddress depositAddress)
        {
            var validationDictionary = new Dictionary<string, Action<DepositAddress>>(StringComparer.Ordinal)
            {
                { "ARK", ValidateArkAddress },
                { "BTC", ValidateBitcoinAddress },
                { "DASH", ValidateDashAddress },
                { "LSK", ValidateLiskAddress },
                { "BCH", ValidateBitcoinCashLegacyAddress },
                { "BCHABC", ValidateBitcoinCashLegacyAddress }, // I think they use the same schema...
                { "ZEN", ValidateZenCashAddress },
                { "LTC", ValidateLitecoinAddress },
                { "ACT", ValidateAChainAddress },
                { "NEO", ValidateNeoAddress },
                { "WAVES", ValidateWavesAddress }
            };

            var ethTokenCommodities = new List<Commodity>
            {
                CommodityRes.LaToken,
                CommodityRes.Ambrosous,
                CommodityRes.Omisego,
                CommodityRes.BancorNetworkToken
            };

            var ethTokenSymbols = new List<string>
            {
                "ETH", "EOS", "KNC", "VEN", "SNM", "POE", "LINK", "WTC", "SUB", "CVC", "BLZ", "ENJ", "REQ",
                "PRL", "FOTA", "TUSD", "GUSD", "USDC"
            };

            ethTokenSymbols.AddRange(ethTokenCommodities.Select(item => item.Symbol));

            ethTokenSymbols.ForEach(token => { validationDictionary[token] = ValidateEthOrEthTokenAddress; });
            
            if (!validationDictionary.ContainsKey(symbol))
            {
                throw new NotImplementedException($"Address validation has not been implemented for \"{symbol}\".");
            }

            validationDictionary[symbol](depositAddress);
        }

        private void ValidateAChainAddress(DepositAddress depositAddress)
        {
            CommonAddressValidation(depositAddress);

            const string ExpectedSymbol = "ACT";
            // const int ExpectedLength = 68;

            if (!depositAddress.Address.ToUpper().StartsWith("ACT")) { throw new ApplicationException($"{ExpectedSymbol} address must start with \"ACT\"."); }

            if (!string.IsNullOrWhiteSpace(depositAddress.Memo))
            {
                throw new ApplicationException("AChain transactions must have a memo.");
            }

            if (string.Equals(depositAddress.Address, depositAddress.Memo, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApplicationException("AChain address and AChain memo must not be equal.");
            }
        }

        private void ValidateNeoAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "NEO";
            const string ExpectedStart = "A";
            const int ExpectedLength = 34;

            CommonAddressValidation(depositAddress);            
            if (!depositAddress.Address.ToUpper().StartsWith(ExpectedStart)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedStart}\"."); }
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} addresses must be {ExpectedLength} characters in length.");  }            
        }

        private void ValidateWavesAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "WAVES";
            const string ExpectedStart = "3P";
            const int ExpectedLength = 35;

            CommonAddressValidation(depositAddress);
            if (!depositAddress.Address.ToUpper().StartsWith(ExpectedStart)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedStart}\"."); }
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} addresses must be {ExpectedLength} characters in length."); }
        }

        private void ValidateArkAddress(DepositAddress depositAddress)
        {
            CommonAddressValidation(depositAddress);

            const string ExpectedSymbol = "ARK";
            const int ExpectedLength = 34;

            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"The deposit address length should be {ExpectedLength} but was {depositAddress.Address.Length}."); }
            if (depositAddress.Address[0] != 'A') { throw new ApplicationException($"{ExpectedSymbol} address must start with \"A\"."); }
        }

        private void ValidateEthOrEthTokenAddress(DepositAddress depositAddress)
        {
            ValidateEthOrEthTokenAddress(depositAddress.Address);
        }

        public void ValidateEthOrEthTokenAddress(string depositAddress)
        {
            CommonAddressValidation(depositAddress);

            if (!depositAddress.StartsWith("0x"))
            {
                throw new ApplicationException("ETH and ETH Token addresses must start with \"0x\"");
            }

            const int ExpectedLength = 42;
            if (depositAddress.Length != ExpectedLength) { throw new ApplicationException($"The deposit address length should be {ExpectedLength} but was {depositAddress.Length}."); }
        }

        // https://thomas.vanhoutte.be/tools/validate-bitcoin-address.php
        //A Bitcoin address is between 25 and 34 characters long;
        // the address always starts with a 1;
        // an address can contain all alphanumeric characters, with the exceptions of 0, O, I, and l.
        private void ValidateBitcoinAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "BTC";

            CommonAddressValidation(depositAddress);

            const int MinLength = 25;
            const int MaxLength = 34;
            const string ExpectedBeginning = "1";
            var ForbiddenChars = new List<char> { '0', 'O', 'I', 'l' };

            if (depositAddress.Address.Length < MinLength) { throw new ApplicationException($"{CommodityName} address must be at least {MinLength} characters."); }
            if (depositAddress.Address.Length > MaxLength) { throw new ApplicationException($"{CommodityName} address must not be more than {MaxLength} characters."); }
            if (!depositAddress.Address.StartsWith(ExpectedBeginning)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedBeginning}\""); }

            ForbiddenChars.ForEach(ch =>
            {
                if (depositAddress.Address.Contains(ch)) { throw new ApplicationException("Bitcoin address contains invalid characters."); }
            });
        }

        private void ValidateDashAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "DASH";

            CommonAddressValidation(depositAddress);

            const int ExpectedLength = 34;
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} address must be {ExpectedLength} characters."); }

            const string ExpectedBeginning = "X";
            if (!depositAddress.Address.StartsWith(ExpectedBeginning)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedBeginning}\""); }
        }

        private void ValidateLiskAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "Lisk";

            CommonAddressValidation(depositAddress);

            const int ExpectedLength = 20;
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} address must be {ExpectedLength} characters."); }

            foreach (var ch in depositAddress.Address.Substring(0, ExpectedLength - 1))
            {
                if (!char.IsDigit(ch)) { throw new ApplicationException($"All characters in a {CommodityName} address must be numbers excecpt for the last character."); }
            }

            const string ExpectedEnding = "L";
            if (!depositAddress.Address.EndsWith(ExpectedEnding)) { throw new ApplicationException($"{CommodityName} address must end with \"{ExpectedEnding}\""); }

        }

        private void ValidateBitcoinCashLegacyAddress(DepositAddress depositAddress)
        {
            // length should be 34
            // must start with a "1" (not sure about this, but go with it for now)

            const string CommodityName = "Bitcoin Cash";

            CommonAddressValidation(depositAddress);

            const int ExpectedLength = 34;
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} address must be {ExpectedLength} characters."); }

            foreach (var ch in depositAddress.Address.Substring(0, ExpectedLength - 1))
            {
                if (!char.IsLetterOrDigit(ch)) { throw new ApplicationException($"All characters in a {CommodityName} address must letters or numbers."); }
            }

            const string ExpectedBeginning = "1";
            if (!depositAddress.Address.StartsWith(ExpectedBeginning)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedBeginning}\""); }
        }

        private void ValidateZenCashAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "ZenCash";

            CommonAddressValidation(depositAddress);

            const int ExpectedLength = 35;
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} address must be {ExpectedLength} characters."); }

            const string ExpectedBeginning = "zn";
            if (!depositAddress.Address.StartsWith(ExpectedBeginning)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedBeginning}\""); }
        }

        private void ValidateLitecoinAddress(DepositAddress depositAddress)
        {
            const string CommodityName = "Litecoin";

            CommonAddressValidation(depositAddress);

            const int ExpectedLength = 34;
            if (depositAddress.Address.Length != ExpectedLength) { throw new ApplicationException($"{CommodityName} address must be {ExpectedLength} characters."); }

            const string ExpectedBeginning = "L";
            if (!depositAddress.Address.StartsWith(ExpectedBeginning)) { throw new ApplicationException($"{CommodityName} address must start with \"{ExpectedBeginning}\""); }
        }

        private void CommonAddressValidation(DepositAddress depositAddress)
        {
            if (depositAddress == null) { throw new ArgumentNullException(nameof(depositAddress)); }
            CommonAddressValidation(depositAddress.Address);
        }

        private void CommonAddressValidation(string depositAddress)
        {
            if (string.IsNullOrWhiteSpace(depositAddress)) { throw new ApplicationException("Address must not be null or whitespace"); }
            if (depositAddress.Trim().Length != depositAddress.Length) { throw new ApplicationException("Address must not be padded with whitespace."); }
        }
    }
}
