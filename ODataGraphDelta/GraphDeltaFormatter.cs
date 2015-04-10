using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ODataGraphDelta {
    public class GraphDeltaFormatter : MediaTypeFormatter {

        public GraphDeltaFormatter() {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
        }

        public override bool CanReadType(Type type) {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GraphDelta<>));
        }

        public override bool CanWriteType(Type type) {
            return false;
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger) {
            return Task.Factory.StartNew(() => {

                object value = null;
                var json = String.Empty;

                using(var reader = new StreamReader(readStream)) {
                    json = reader.ReadToEnd();
                }

                var token = JToken.Parse(json) as JObject;

                if(token != null) {

                    var constructor = type.GetConstructor(new Type[] { typeof(JObject) });
                    value = constructor.Invoke(new object[] { token });

                }

                return value;
            });
        }
    }
}
