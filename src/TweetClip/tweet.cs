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

        public Tweet(JToken data, string[] whiteList)
        {
            _nodes = new Dictionary<string, string>();
            GenerateMap(data, whiteList);
        }

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

        void GenerateMap(JToken topJToken, string[] whiteList)
        {
            
            foreach (var jtoken in topJToken.Children())
            {
                if (jtoken.Children().Count() == 0)
                {
                    //if the item is on the whiteList add it otherwise don't!
                    if (whiteList.Contains<string>(jtoken.Path))
                    {

                        //Console.WriteLine("Data found {0} +---->> {1}", jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
                        _nodes.Add(jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
                    }
                    else
                    {
                        //Console.WriteLine("item: " + jtoken.Path + " omitted as missing from whitelist");
                    }
                }
                GenerateMap(jtoken, whiteList);
            };
        }

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

        ////object constructor
        //public void PrepareProduct()
        //{
        //    _product = new JObject();

        //    //container to hold the previous path - Assumption: the tweet remains ordered
        //    List<string> previousPath = new List<string>();
        //    previousPath.Add("");

        //    //serialiastion object
        //    JTokenWriter jw = new JTokenWriter();
        //    jw.WriteStartObject();

        //    foreach (KeyValuePair<string, string> kvp in _nodes)
        //    {
        //        //split the path into segments
        //        List<string> path = kvp.Key.Split('.').ToList<string>();

        //        //begin recursive constrinction
        //        AddNode(jw, path, kvp.Value);
        //    }

        //    jw.WriteEndObject();
        //    _product = (JObject)jw.Token;
        //}

        //public void AddNode(JTokenWriter jw, List<string> path, string val)
        //{
        //    //CASE 1 - x
        //    //if were on the leaf, add the value property
        //    if (path.Count == 1)
        //    {
        //        //insert the value in path
        //        jw.WritePropertyName(path[0]);
        //        jw.WriteValue(val);
        //    }
        //    //case 2 - x[#] - we know there will be more of these
        //    //if were not at the leaf, drill down to the leaf
        //    else
        //    {
        //        if (path[0].Contains('['))
        //        {
        //            string[] str = path[0].Split("[]".ToArray<char>(), StringSplitOptions.RemoveEmptyEntries);
        //            path[0] = str[0];
        //            //then handle arrays
        //            int f = 0;
        //        }
        //        //if the branch doesn't already exist
        //        if (!jw.Path.Contains(path[0]))
        //        {
        //            //construct the branch
        //            jw.WritePropertyName(path[0]);
        //            jw.WriteStartObject();
        //        }

        //        //take the current path and trim the front
        //        string[] pp = new string[path.Count - 1];
        //        path.CopyTo(1, pp, 0, path.Count - 1);

        //        //recurse
        //        AddNode(jw, pp.ToList<string>(), val);
        //    }
        //}

        //public void AddNode1(JTokenWriter jw, List<string> path, string val)
        //{
        //    //if were on the leaf, add the object
        //    if (path.Count == 1)
        //    {
        //        //insert the value in path
        //        jw.WritePropertyName(path[0]);
        //        jw.WriteValue(val);
        //    }
        //    //if were not at the leaf, drill down to the leaf
        //    else
        //    {
        //        if(path[0].Contains('['))
        //        {
        //            string[] str = path[0].Split("[]".ToArray<char>(), StringSplitOptions.RemoveEmptyEntries);
        //            path[0] = str[0];
        //            //then handle arrays
        //            int f = 0;
        //        }
        //        //if the branch doesn't already exist
        //        if (!jw.Path.Contains(path[0]))
        //        {
        //            //construct the branch
        //            jw.WritePropertyName(path[0]);
        //            jw.WriteStartObject();                    
        //        }

        //        //take the current path and trim the front
        //        string[] pp = new string[path.Count - 1];
        //        path.CopyTo(1, pp, 0, path.Count - 1);

        //        //recurse
        //        AddNode(jw, pp.ToList<string>(), val);
        //    }
        //}

        //public string Product
        //{
        //    get { return _product.ToString(); }
        //}
        Dictionary<string, string> _nodes;
        //JObject _product;
    }
}
