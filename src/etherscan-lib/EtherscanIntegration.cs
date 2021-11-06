using config_client_lib;
using Newtonsoft.Json;
using res_util_lib;
using System.Collections.Generic;
using System.IO;
using System.Text;
using web_util;

namespace etherscan_lib
{
    // https://hexdocs.pm/etherscan/api-reference.html
    // https://etherscan.io/apis
    // https://etherscan.io/tokens - shows a list of tokens
    public class EtherscanIntegration
    {
        private readonly IConfigClient _configClient;

        private static object InitLocker = new object();
        private static bool HasCopiedFiles = false;

        public EtherscanIntegration(
            IConfigClient configClient,
            IWebUtil webUtil)
        {
            _configClient = configClient;
            CopyFilesIfNeeded();
        }

        private void CopyFilesIfNeeded()
        {
            if (HasCopiedFiles) { return; }
            lock (InitLocker)
            {
                if (HasCopiedFiles) { return; }

                UnpackageFiles();

                HasCopiedFiles = true;
            }
        }

        //public void Stuff()
        //{
        //    Console.WriteLine("Before Edge!");

        //    var script = Edgeify(ResUtil.Get("stuff.js"));
        //    var func = Edge.Func(script);

        //    var edgeTask = func(".NET");
        //    var edgeResult = edgeTask.Result;
        //    Console.WriteLine(edgeResult);
        //}

        public class FileAndContents
        {
            public string FileName { get; set; }
            public string Contents { get; set; }
        }

        private void UnpackageFiles()
        {
            var modules = JsonConvert.DeserializeObject<List<FileAndContents>>(ResUtil.Get("node-modules.json"));
            foreach (var item in modules)
            {
                var fileInfo = new FileInfo(item.FileName);

                if (!fileInfo.Directory.Exists) { fileInfo.Directory.Create(); }
                File.WriteAllText(item.FileName, item.Contents);
            }
        }

        private string Edgeify(string originalScript)
        {
            return new StringBuilder()
                .AppendLine("return function (data, callback) {")
                .AppendLine(originalScript)
                .AppendLine("}")
                .ToString();
        }
    }
}
