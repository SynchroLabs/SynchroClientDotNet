using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient
{
    public class Element
    {
        JObject _element;

        public Element(JObject element)
        {
            _element = element;
        }

        public JToken BindingContext { get; set; }
    }
}
