using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Crichton.Representors;
using Crichton.Representors.Serializers;

namespace Crichton.Client
{
    public class CrichtonClient
    {
        public readonly IList<Func<CrichtonRepresentor, HttpClient, ISerializer, Task<CrichtonRepresentor>>> steps;
        public Uri BaseUri { get; private set; }
        public ISerializer Serializer { get; private set; }
        public Dictionary<string, string> CustomHeaders { get; set; } 

        public CrichtonClient(Uri baseUri, ISerializer serializer)
        {
            BaseUri = baseUri;
            Serializer = serializer;
            CustomHeaders = new Dictionary<string, string>();
            steps = new List<Func<CrichtonRepresentor, HttpClient, ISerializer, Task<CrichtonRepresentor>>>();
        }

        internal CrichtonClient(CrichtonClient existingClient,
            Func<CrichtonRepresentor, HttpClient, ISerializer, Task<CrichtonRepresentor>> newStep)
        {
            BaseUri = existingClient.BaseUri;
            Serializer = existingClient.Serializer;
            CustomHeaders = existingClient.CustomHeaders;
            steps = new List<Func<CrichtonRepresentor, HttpClient, ISerializer, Task<CrichtonRepresentor>>>(existingClient.steps);
            steps.Add(newStep);
        }

        public void AddCustomHeader(string key, string value)
        {
            CustomHeaders.Add(key, value);
        }

        public CrichtonClient NavigateToUrl(string relativeUrl)
        {
            Func<CrichtonRepresentor, HttpClient, ISerializer, Task<CrichtonRepresentor>> newStep =
                async delegate(CrichtonRepresentor representor, HttpClient client, ISerializer serializer)
                {
                    var result = await client.GetStringAsync(relativeUrl);
                    var builder = serializer.DeserializeToNewBuilder(result,() => new RepresentorBuilder());
                    return builder.ToRepresentor();

                };

            return new CrichtonClient(this, newStep);
        }

        public CrichtonClient NavigateTransition(string rel)
        {
            Func<CrichtonRepresentor, HttpClient, ISerializer, Task<CrichtonRepresentor>> newStep =
            async delegate(CrichtonRepresentor representor, HttpClient client, ISerializer serializer)
            {
                var transition = representor.Transitions.FirstOrDefault(t => t.Rel == rel);
                if (transition == null) throw new Exception("No transition for rel " + rel);
                var result = await client.GetStringAsync(transition.Uri);
                var builder = serializer.DeserializeToNewBuilder(result, () => new RepresentorBuilder());
                return builder.ToRepresentor();

            };

            return new CrichtonClient(this, newStep);
        }

        public async Task<CrichtonRepresentor> GetAsync()
        {
            var client = new HttpClient {BaseAddress = BaseUri};
            foreach (var header in CustomHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            client.DefaultRequestHeaders.Add("Accept", Serializer.ContentType);
            
            var representor = new CrichtonRepresentor();
            
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var step in steps)
            {
                representor = await step(representor, client, Serializer);
            }

            return representor;
        }
    }
}
