using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweetClip
{
    class Tweet
    {
        private Tweet() { }
        public Tweet(JToken data)
        {
            _nodes = new Dictionary<string, string>();
            GenerateMap(data);
        }

        //public Tweet(JToken data, string[] whiteList)
        //{
        //    _nodes = new Dictionary<string, string>();
        //    GenerateMap(data, whiteList);
        //}

        void GenerateMap(JToken topJToken)
        {
            foreach (var jtoken in topJToken.Children())
            {
                if (jtoken.Children().Count() == 0)
                {
                    //Console.WriteLine("Data found {0} +---->> {1}", jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
                    _nodes.Add(jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
                }
                GenerateMap(jtoken);
            };
        }

        //void GenerateMap(JToken topJToken, string[] whiteList)
        //{
            
        //    foreach (var jtoken in topJToken.Children())
        //    {
        //        if (jtoken.Children().Count() == 0)
        //        {
        //            //if the item is on the whiteList add it otherwise don't!
        //            if (whiteList.Contains<string>(jtoken.Path))
        //            {

        //                //Console.WriteLine("Data found {0} +---->> {1}", jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
        //                _nodes.Add(jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
        //            }
        //            else
        //            {
        //                //Console.WriteLine("item: " + jtoken.Path + " omitted as missing from whitelist");
        //            }
        //        }
        //        GenerateMap(jtoken, whiteList);
        //    };
        //}

        public void Index(ref Dictionary<string, int> index)
        {
            foreach (KeyValuePair<string, string> kvp in _nodes)
            {
                if (!index.ContainsKey(kvp.Key))
                {
                    index.Add(kvp.Key, 1);
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
