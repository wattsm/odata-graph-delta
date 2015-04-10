using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ODataGraphDelta {
    public class GraphDelta<T> : TypedGraphDelta {

        public GraphDelta(JObject json)
            : base(typeof(T), json) {
        }

    }
}
