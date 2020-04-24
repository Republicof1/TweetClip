using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CommandLine;
using Newtonsoft.Json.Linq;
using static TweetClip.Program;

//TODO: More capable handling of arrays needed to manage - BLACKLIST 
namespace TweetClip
{
    class TweetClipper
    {
        public TweetClipper()
        {
            _tweets = new List<Tweet>();
            _clippedTweets = new List<JObject>();
            _tweetObjects = new List<JObject>();
            _contents = new Dictionary<string, int>();
            _types = new Dictionary<string, string>();
            _clipTwStr = new List<string>();
            _rawTweets = null;
            _whiteList = null;
            _blackListPtr = null;
            _processOutputPtr = null;
            _codex = null;
            _outputFilename = "";

        }
        public delegate string[] MakeBlackList();
        public delegate void ProcessOutput();

        public void ClipMode (string[] dataFiles, string[] configFiles, string[] codexFiles, string outFilename, modeFlags mode, outputFlags output)
        {
            _outputFilename = OUTPUT_FOLDER + outFilename;

            //stip off any extensions
            if (outFilename.Contains('.'))
            {
                Console.CursorTop = 2;
                Console.WriteLine("output file extension removed...");
                _outputFilename = OUTPUT_FOLDER + outFilename.Split('.')[0];
            }

            //select the search mode
            switch (mode)
            {
                case modeFlags.WIDE:
                    {
                        _blackListPtr = MakeBlackList_Wide;
                    }
                    break;
                case modeFlags.STRICT:
                    {
                        _blackListPtr = MakeBlackList_Strict;
                    }
                    break;
                case modeFlags.EXPLICIT:
                    {
                        _blackListPtr = MakeBlackList_Explicit;
                    }
                    break;
            }

            //select the output mode
            switch (output)
            {
                case outputFlags.RAW_JSON:
                    {
                        _processOutputPtr = ProcessOutput_RawJSON;
                    }
                    break;
                case outputFlags.JSON_ARRAY:
                    {
                        _processOutputPtr = ProcessOutput_Array;
                    }
                    break;
                case outputFlags.CSV:
                    {
                        _processOutputPtr = ProcessOutput_CSV;
                    }
                    break;
                case outputFlags.ELASTIC_JSON:
                    {
                        _processOutputPtr = ProcessOutput_Elastic;
                    }
                    break;
                case outputFlags.PROTOTYPE:
                    {
                        _processOutputPtr = ProcessOutput_PrototypeList;
                    }
                    break;
            }

            //if codex is required boot up the list
            if (codexFiles != null)
            {
                Console.WriteLine("Building proxy symbols from codex");
                _codex = new Codex(File.ReadAllLines(codexFiles[0]));
            }

            _rawTweets = new RawData(dataFiles[0]);
            
            //---------------------    map all contents, truncate, pseudonomise, and serialise and send to file    -------------------------

            //map data and generate inital descriptive output
            _whiteList = File.ReadAllLines(configFiles[0]);

            int count = 0;
            for(int i = 0; i < _rawTweets.Data.Length; ++i)
            {
                Console.CursorTop = 3;
                Console.CursorLeft = 0;
                Console.WriteLine("Discovering tweet \"" + ++count + "\"");
                _tweetObjects.Add(JObject.Parse(_rawTweets.Data[i]));
                _tweets.Add(new Tweet(_tweetObjects.Last(), mode, _codex));
                _tweets.Last().Index(ref _contents, ref _types);
            }
            
            //preparing files 
            _processOutputPtr();
        }

        //using the tweet object version of the data
        private void ProcessOutput_PrototypeList()
        {
            Console.CursorTop = 4;
            Console.CursorLeft = 0;
            Console.WriteLine("preparing **PROTOTYPE LIST**");
            //process for CSV
            _blackListPtr();

            int count = 0;

            Dictionary<string, int> rowIndex = new Dictionary<string, int>();

            //add the header
            List<string> table = new List<string>();
            List<string> tableRow = new List<string>();

            for (int j = 0; j < _whiteList.Length; ++j)
            {
                tableRow.Add(_whiteList[j]);
            }
            tableRow[tableRow.Count - 1] = tableRow.Last().TrimEnd(',');
            table.Add(tableRow.Aggregate((a, b) => a + "," + b));

            Console.WriteLine("Prototype list constructed, saving to file");
            //encode the text with UTF-8 BOM, this means Excel will pick up the encoding

            List<string> outWhiteList = _whiteList.ToList();
            outWhiteList.Sort();

            File.WriteAllLines(_outputFilename + "_prototype.txt", outWhiteList);
        }

        //using the tweet object version of the data
        private void ProcessOutput_CSV()
        {
            Console.CursorTop = 4;
            Console.CursorLeft = 0;
            Console.WriteLine("Packaging tweets as **CSV TABLE** and saving to file");
            //process for CSV
            _blackListPtr();

            int count = 0;
 
            Dictionary<string, int> rowIndex = new Dictionary<string, int>();
            
            //add the header
            List<string> table = new List<string>();
            List<string> tableRow = new List<string>();

            for (int j = 0; j < _whiteList.Length; ++j)
            {
                tableRow.Add(_whiteList[j]);
            }
            tableRow[tableRow.Count-1] = tableRow.Last().TrimEnd(',');
            table.Add(tableRow.Aggregate((a,b) => a + "," + b));
            
            for (int i = 0; i < _tweets.Count; ++i)
            {
                tableRow = new List<string>();
                Dictionary<string,string> tw = _tweets[i].Nodes;
                List<string> twKeys = tw.Keys.ToList();

                Console.CursorLeft = 0;
                Console.CursorTop = 6;
                Console.WriteLine("compiling table row \"" + ++count + "\"");
                for (int j = 0; j < _whiteList.Length; ++j)
                {
                    string lookUp = _whiteList[j];

                    if (twKeys.Contains(lookUp))
                    {
                        tableRow.Add("\"" + tw[lookUp] + "\"");
                    }
                    else
                    {
                        tableRow.Add("\"\"");
                    }                   
                }
                table.Add(tableRow.Aggregate((a, b) => a + "," + b));
            }

            Console.WriteLine("table constructed, saving to file");
            //encode the text with UTF-8 BOM, this means Excel will pick up the encoding
            Encoding utf8WithBom = new UTF8Encoding(true);
            File.WriteAllLines(_outputFilename + "_table.csv", table, utf8WithBom);
        }
        
        //process for JSON
        private void ProcessJSON()
        {
            int count = 0;

            foreach (JObject tweet in _tweetObjects)
            {
                Console.CursorTop = 4;
                Console.CursorLeft = 0;
                Console.WriteLine("Clipping tweet \"" + ++count + "\"");
                string[] blackList = _blackListPtr();
                _clippedTweets.Add(ClipTweet(tweet, blackList));
                //remove all new line
                _clipTwStr.Add(_clippedTweets.Last().ToString().Replace("\r\n", "").Replace("  ", ""));
            }
        }

        //using the string version of the data
        private void ProcessOutput_Array()
        {
            ProcessJSON();

            Console.WriteLine("Packaging tweets as **JSON ARRAY** and saving to file");
            string file = "";
            for (int i = 0; i < _clipTwStr.Count; ++i)
            {
                file += (_clipTwStr[i] + ",\r\n");
            }
            string leadingInfo = "[";
            string trailingInfo = "]";

            file = leadingInfo + file.Remove(file.Length - 3) + trailingInfo;

            
            File.WriteAllText(_outputFilename + "_array.json", file);
        }

        //using the string version of the data
        private void ProcessOutput_Elastic()
        {
            ProcessJSON();

            Console.WriteLine("Packaging tweets as **ELASTIC COMPATIBLE JSON COLLECTION** and saving to file");
            string file = "";
            for (int i = 0; i < _clipTwStr.Count; ++i)
            {
                file += ("{ \"index\" : { \"_id\" : \"" + i + "\" } }\r\n" + _clipTwStr[i] + "\r\n");
            }
            
            string trailingInfo = "\n";

            file = file.Remove(file.Length - 3) + trailingInfo;


            File.WriteAllText(_outputFilename + "_ELK.json", file);
        }

        //using the string version of the data
        private void ProcessOutput_RawJSON()
        {
            ProcessJSON();

            Console.WriteLine("Packaging tweets as **RAW JSON** and saving to file");
            string file = "";
            for (int i = 0; i < _clipTwStr.Count; ++i)
            {
                file += (_clipTwStr[i] + "\r\n");
            }

            File.WriteAllText(_outputFilename + "_raw.json", file);
        }
        //make a composite list from the whitelist that we'll use to prune the copied tweet
        
        //partial match ignoring indices
        private string[] MakeBlackList_Wide()
        {

            Console.WriteLine("searcing for matches using **Wide** algorithm");
            List<string> listr = _contents.Keys.ToList();
            List<string> revisedWhitelist = new List<string>();

            for (int x = 0; x < _whiteList.Length; ++x)
            {
                string[] targetSignature = _whiteList[x].Split('.');
                //get rid of array indices
                for (int i = 0; i < targetSignature.Length; ++i)
                {
                    if (targetSignature[i].Last() == ']')
                    {
                        targetSignature[i] = targetSignature[i].Remove(targetSignature[i].IndexOf('['));
                    }
                } 
                //find all elements
                string[] foundFields = listr.FindAll(
                    delegate (string listValue)
                    {                          
                        for (int i = 0; i < targetSignature.Length; ++i)
                        {
                            if(!listValue.Contains(targetSignature[i]))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                ).ToArray();

                for(int k = 0; k < foundFields.Length; ++k)
                {
                    revisedWhitelist.Add(foundFields[k]);
                    _contents.Remove(foundFields[k]);
                }
            }

            //revise the white list to include all found targets
            _whiteList = revisedWhitelist.ToArray();

            //retrieve the list in reverse order to preserve array indices
            return _contents.Keys.Reverse().ToArray();
        }

        //inclusive (greater than) match ignoring indices
        private string[] MakeBlackList_Strict()
        {
            Console.WriteLine("searcing for matches using **Strict** algorithm");

            List<string> listr = _contents.Keys.ToList();
            List<string> revisedWhitelist = new List<string>();

            for (int x = 0; x < _whiteList.Length; ++x)
            {
                string[] targetSignature = _whiteList[x].Split('.');
                //get rid of array indices
                for (int i = 0; i < targetSignature.Length; ++i)
                {
                    if (targetSignature[i].Last() == ']')
                    {
                        targetSignature[i] = targetSignature[i].Remove(targetSignature[i].IndexOf('['));
                    }
                }
                //find all elements
                string[] foundFields = listr.FindAll(
                    delegate (string listValue)
                    {
                        string[] listVarSegments = listValue.Split('.');
                        //get rid of array indices
                        for (int i = 0; i < listVarSegments.Length; ++i)
                        {
                            if (listVarSegments[i].Last() == ']')
                            {
                                listVarSegments[i] = listVarSegments[i].Remove(listVarSegments[i].IndexOf('['));
                            }
                        }

                        int match = 0;
                        for (int i = 0; i < targetSignature.Length; ++i)
                        {
                            for (int j = 0; j < listVarSegments.Length; ++j)
                            {
                                if (targetSignature[i] == listVarSegments[j])
                                {
                                    ++match;
                                }
                            }
                        }

                        //match strength
                        if (match >= targetSignature.Length)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                ).ToArray();

                for (int k = 0; k < foundFields.Length; ++k)
                {
                    revisedWhitelist.Add(foundFields[k]);
                    _contents.Remove(foundFields[k]);
                }
            }

            //revise the white list to include all found targets
            _whiteList = revisedWhitelist.ToArray();
            //retrieve the list in reverse order to preserve array indices
            return _contents.Keys.Reverse().ToArray();
        }

        //exact match ignoring indices
        private string[] MakeBlackList_Explicit()
        {
            List<string> revisedWhitelist = new List<string>();

            Console.WriteLine("searcing for matches using **Explicit** algorithm");
            List<string> contentArray = _contents.Keys.ToList();
            for(int i = 0; i < _whiteList.Length; ++i)
            {
                //clean the whiteList item of array indices
                string[] whiteListSegments = _whiteList[i].Split('.');
                //get rid of array indices
                string wlp = "";
                for (int j = 0; j < whiteListSegments.Length; ++j)
                {
                    if (whiteListSegments[j].Last() == ']')
                    {
                        whiteListSegments[j] = whiteListSegments[j].Remove(whiteListSegments[j].IndexOf('['));
                    }
                    wlp += whiteListSegments[j] + ".";
                }
                wlp = wlp.Remove(wlp.Length - 1);


                //get all matches to pattern
                string[] contentTargets = contentArray.FindAll(delegate (
                    string contentItem)
                {
                    string[] contentItemSegments = contentItem.Split('.');
                    string clp = "";
                    for (int j = 0; j < contentItemSegments.Length; ++j)
                    {
                        if (contentItemSegments[j].Last() == ']')
                        {
                            contentItemSegments[j] = contentItemSegments[j].Remove(contentItemSegments[j].IndexOf('['));
                        }
                        clp += contentItemSegments[j] + ".";
                    }
                    clp = clp.Remove(clp.Length - 1);

                    if (clp == wlp)
                    {
                        return true;
                    }
                    return false;
                }).ToArray();


                //if (_contents.Keys.Contains<string>(whiteList[i]))
                for (int k = 0; k < contentTargets.Length; ++k)
                {
                    revisedWhitelist.Add(contentTargets[k]);
                    _contents.Remove(contentTargets[k]);
                }
            }

            //revise the white list to include all found targets
            _whiteList = revisedWhitelist.ToArray();

            //retrieve the list in reverse order to preserve array indices
            return _contents.Keys.Reverse().ToArray();
        }

        //create a copy of the tweet and prune it using a blacklist
        private JObject ClipTweet(JObject src, string[] blackList)
        {
            JObject jo = new JObject(src);
            //foreach (string path in blackList)
            for(int i = 0; i < blackList.Length; ++i)
            {
                //start walking through the blacklist
                ClipAndWalk(jo.SelectToken(blackList[i]));
            }
            return jo;
        }

        //this does the work of pruning the tree
        private bool ClipAndWalk(JToken target, JToken master = null)
        {
            //parent container
            JToken parent = null;

            //bailout if for whaterver reason the target is null
            if (target == null)
            {
                return false;
            }

            //pop the path into a container for convenience
            string path = target.Path;

            //get parent
            if (target != null)
            {
                parent = target.Parent;
                if(parent == null)
                {
                    return false;
                }
            }

            if (target.Type != JTokenType.Array)
            {
                //if the parent is a property we need to walk up the tree
                if (parent.Type == JTokenType.Property)
                {
                    ClipAndWalk(parent);
                }
                else
                {
                    target.Remove();
                }
            }
            //only remove arrays where they are empty
            //avoids mistaken clearance of leaves
            else if (target.Children().Count() == 0)
            {
                //if the parent is a property we need to walk up the tree
                if (parent.Type == JTokenType.Property)
                {
                    ClipAndWalk(parent);
                }
                else
                {
                    target.Remove();
                }
            }

            //walk back the tree if the parent of the target is empty
            if (parent.Children().Count() == 0)
            {
                //if parent is empty recurse into the tree
                ClipAndWalk(parent);
            }
            else
            {
                //report removed successfully
                return true;
            }
            return false;
        }

        public void IndexMode (string[] dataFiles, modeFlags mode)
        {           
            _rawTweets = new RawData(dataFiles[0]);

            //---------------------    map all contents, send to file and exit     -------------------------

            //explore all tweets, establish content nodes and output results in index file and catalogue files
            int count = 0;
            foreach (string tw in _rawTweets.Data)
            {
                Console.CursorTop = 4;
                Console.CursorLeft = 0;
                Console.WriteLine("Exploring tweet \"" + ++count + "\"");
                JObject tweetObject = JObject.Parse(tw);
                _tweets.Add(new Tweet(tweetObject, mode));
                _tweets.Last<Tweet>().Index(ref _contents, ref _types);
            }

            //this creates a list of element ignoreing array contents
            List<string> orderedPathList = new List<string>();

            //alphabetically order these paths
            List<string> pathList = _contents.Keys.ToList();
            pathList.Sort();
            //optimisation
            string[] pathArray = pathList.ToArray();
            
            for(int i = 0; i < pathArray.Length; ++i)
            {
                //clean the paths of array indices
                string[] pathSegments = pathArray[i].Split('.');

                string plp = "";
                for (int j = 0; j < pathSegments.Length; ++j)
                {
                    if (pathSegments[j].Last() == ']')
                    {
                        pathSegments[j] = pathSegments[j].Remove(pathSegments[j].IndexOf('['));
                    }
                    plp += pathSegments[j] + ".";
                }
                plp = plp.Remove(plp.Length - 1);

                //only add this if it's not already been added
                if (!orderedPathList.Contains(plp))
                {
                    orderedPathList.Add(plp);
                }
            }
            //save them cleaned paths to file
            File.WriteAllLines("Data\\index.txt", orderedPathList);

            Console.WriteLine("Exploration complete!\nIndex file produced");
            //this generates a complete list of data fields
            List<string> contentList = new List<string>();
            contentList.Add("Node,Type,Frequency");
            
            //
            foreach (string index in pathList)
            {
                contentList.Add(index + "," + _types[index] + "," + _contents[index]);   
            }
            File.WriteAllLines("Data\\catalogue.csv", contentList.ToArray<string>());
            Console.WriteLine("Catalogue file produced");
        }

        MakeBlackList _blackListPtr;
        ProcessOutput _processOutputPtr;

        RawData _rawTweets;
        Codex _codex;
        List<Tweet> _tweets;
        List<JObject> _clippedTweets;
        Dictionary<string, int> _contents;
        Dictionary<string, string> _types;
        List<JObject> _tweetObjects;
        string[] _whiteList;
        List<string> _clipTwStr;
        string _outputFilename;
    }
}
