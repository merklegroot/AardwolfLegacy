using Newtonsoft.Json;
using System;
using System.IO;

namespace test_shared
{
    public static class TestExtensions
    {
        //public static void Dump(this object item)
        //{
        //    if (item == null) { Console.WriteLine("[null]"); return; }
        //    if (item is string itemString)
        //    {
        //        if (itemString == string.Empty)
        //        {
        //            Console.WriteLine("[empty string]");
        //            return;
        //        }

        //        Console.WriteLine(itemString);
        //        return;
        //    }

        //    var contents = JsonConvert.SerializeObject(item, Formatting.Indented);
        //    Console.WriteLine(contents);
        //}

        public static string ToJson(this object item)
        {
            if (item == null) { return null; }
            return JsonConvert.SerializeObject(item, Formatting.Indented);
        }

        public static T FromJson<T>(this string text)
        {
            if (text == null || string.IsNullOrWhiteSpace(text)) { return default(T); }
            return JsonConvert.DeserializeObject<T>(text);
        }

        public static T Save<T>(this T item, string fileName)
        {
            File.WriteAllText(fileName, item.ToJson());
            return item;
        }

        public static T Load<T>(this string fileName)
        {
            return File.Exists(fileName)
                ? FromJson<T>(File.ReadAllText(fileName))
                : default(T);
        }
    }
}
