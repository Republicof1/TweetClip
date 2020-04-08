using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TweetClip.Program;

namespace TweetClip
{
    class Tweet
    {
        private Tweet() { }
        public Tweet(JToken data, modeFlags mode)
        {
            _nodes = new Dictionary<string, string>();
            GenerateMap(data);
        }

        void GenerateMap(JToken topJToken)
        {
            foreach (var jtoken in topJToken.Children())
            {
                if (jtoken.Children().Count() == 0)
                {
                    //if we get a null, this is *technically* a property value
                    //instead we'll call this a property
                    if (jtoken.Type == JTokenType.Null)
                    {
                        _nodes.Add(jtoken.Path, JTokenType.Property.ToString());
                    }
                    else
                    {
                        _nodes.Add(jtoken.Path, jtoken.Type.ToString());
                    }
                }
                GenerateMap(jtoken);
            };
        }

        public void Index(ref Dictionary<string, int> index, ref Dictionary<string, string> types)
        {
            foreach (KeyValuePair<string, string> kvp in _nodes)
            {
                if (!index.ContainsKey(kvp.Key))
                {
                    index.Add(kvp.Key, 1);
                    types.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    //update the instance frequenchy
                    int instance = index[kvp.Key];
                    index[kvp.Key] = ++instance;

                }
            }
        }

        Dictionary<string, string> _nodes;
    }
}
