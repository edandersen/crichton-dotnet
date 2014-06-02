using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crichton.Client;
using Crichton.Representors.Serializers;
using Newtonsoft.Json;

namespace ConsoleClientSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new CrichtonClient(new Uri(args[0]), new HalSerializer());

            client = client.NavigateToUrl(args[1]);

            if (args.Length > 2)
            {
                client.AddCustomHeader(args[2], args[3]);
            }

            var result = client.GetAsync().Result;

            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

            Console.WriteLine("");
            Console.WriteLine("Self Link: " + result.SelfLink);

            Console.ReadKey();
        }
    }
}
