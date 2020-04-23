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
        public Tweet(JToken data, modeFlags mode, Codex codex = null)
        {
            _data = (JObject)data;
            _codex = codex;

            _nodes = new Dictionary<string, string>();

            if (mode == modeFlags.INDEX)
            {
                GenerateMap_Index(data);
            }
            else if (codex == null)
            {
                GenerateMap_Clip(data);
            }
            else
            {
                GenerateMap_Clip_Symbolise(data);
            }

        }

        void GenerateMap_Index(JToken topJToken)
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
                GenerateMap_Index(jtoken);
            };
        }

        void GenerateMap_Clip(JToken topJToken)
        {
            foreach (var jtoken in topJToken.Children())
            {
                if (jtoken.Children().Count() == 0)
                {
                    //if we get a null, this is *technically* a property value
                    //instead we'll call this a property
                    _nodes.Add(jtoken.Path, string.Join(",\n", jtoken.Parent.Values()));
                }
                GenerateMap_Clip(jtoken);
            };
        }

        void GenerateMap_Clip_Symbolise(JToken topJToken)
        {
            
            foreach (var jtoken in topJToken.Children())
            {
                if (jtoken.Children().Count() == 0)
                {
                    //if we get a null, this is *technically* a property value
                    //instead we'll call this a property

                    //nodes to search
                    //*.name, *.screen_name, *full_text
                    string pathValue = string.Join(",\n", jtoken.Parent.Values());
                    string path = jtoken.Path;
                    string value = string.Join(",\n", jtoken.Parent.Values());

                    if (jtoken.Path.Contains("full_text"))
                    {
                        string[] words = pathValue.Split(new char[]{ ' ', '\n'});
                        for(int i = 0; i < words.Length; ++i)
                        {
                            if(words[i].Contains("@"))
                            {
                                //handle the case the last char is a elipsis
                                if (words[i].Last() == '…')
                                {
                                    words[i] = "@" + _codex.Proxy(words[i].Substring(0, words[i].Length-2)) + "…";
                                }
                                else
                                {
                                    words[i] = "@" + _codex.Proxy(words[i].Substring(0));
                                }
                            }
                        }

                        pathValue = string.Join(" ", words);
                        jtoken.Replace((JToken)pathValue);
                    }
                    else if (jtoken.Path.Contains(".screen_name"))
                    {
                        //screen name enforced @ in index for ease
                        var replacement = _codex.Proxy("@" + value);
                        jtoken.Replace((JToken)replacement);
                        pathValue = replacement;
                    }
                    else if (jtoken.Path.Contains(".name"))
                    {
                        //name allows spaces
                        var replacement = _codex.Proxy(value);
                        replacement.Replace('_', ' ');
                        jtoken.Replace((JToken)replacement);
                        pathValue = replacement;
                    }
                    int xx = 0;
                    _nodes.Add(path, pathValue);
                }
                GenerateMap_Clip_Symbolise(jtoken);
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

        public Dictionary<String, String> Nodes
        {
            get { return _nodes; }
        }

        Dictionary<string, string> _nodes;
        Codex _codex;
        JObject _data;
    }
}
