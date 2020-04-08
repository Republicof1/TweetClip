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
            _rawTweets = null;
            _whiteList = null;
            
        }
        public delegate string[] MakeBlackList(string[] whiteList);

        public void ClipMode (string[] dataFiles, string[] configFiles, modeFlags mode)
        {
            MakeBlackList blackListPtr = null;
            //select the search mode
            switch (mode)
            {
                case modeFlags.WIDE:
                    {
                        blackListPtr = MakeBlackList_Wide;
                    }
                    break;
                case modeFlags.STRICT:
                    {
                        blackListPtr = MakeBlackList_Strict;
                    }
                    break;
                case modeFlags.EXPLICIT:
                    {
                        blackListPtr = MakeBlackList_Explicit;
                    }
                    break;
            }
               
            _rawTweets = new RawData(dataFiles[0]);
            
            //---------------------    map all contents, truncate, pseudonomise, and serialise and send to file    -------------------------

            //map data and generate inital descriptive output
            _whiteList = File.ReadAllLines(configFiles[0]);

            List<string> clipTwStr = new List<string>();
            int count = 0;
            for(int i = 0; i < _rawTweets.Data.Length; ++i)
            {
                Console.WriteLine("Discovering tweet \"" + ++count + "\"");
                _tweetObjects.Add(JObject.Parse(_rawTweets.Data[i]));
                _tweets.Add(new Tweet(_tweetObjects.Last(), mode));
                _tweets.Last().Index(ref _contents, ref _types);
            }
            count = 0;
            foreach (JObject tweet in _tweetObjects)
            {
                Console.WriteLine("Clipping tweet \"" + ++count + "\"");
                string[] blackList = blackListPtr(_whiteList);
                _clippedTweets.Add(ClipTweet(tweet, blackList));
                clipTwStr.Add(_clippedTweets.Last().ToString());
            }

            File.WriteAllLines("Data\\out.json", clipTwStr);
        }

        //make a composite list from the whitelist that we'll use to prune the copied tweet
        
        //partial match ignoring indices
        private string[] MakeBlackList_Wide(string[] whiteList)
        {
            Console.WriteLine("searcing for matches using **Wide** algorithm");
            List<string> listr = _contents.Keys.ToList();

            for (int x = 0; x < whiteList.Length; ++x)
            {
                string[] targetSignature = whiteList[x].Split('.');
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
                    _contents.Remove(foundFields[k]);
                }

                int f = 0;
            }
            //retrieve the list in reverse order to preserve array indices
            return _contents.Keys.Reverse().ToArray();
        }

        //inclusive (greater than) match ignoring indices
        private string[] MakeBlackList_Strict(string[] whiteList)
        {
            Console.WriteLine("searcing for matches using **Strict** algorithm");
            List<string> listr = _contents.Keys.ToList();

            for (int x = 0; x < whiteList.Length; ++x)
            {
                string[] targetSignature = whiteList[x].Split('.');
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
                    _contents.Remove(foundFields[k]);
                }
            }
            //retrieve the list in reverse order to preserve array indices
            return _contents.Keys.Reverse().ToArray();
        }

        //exact match ignoring indices
        private string[] MakeBlackList_Explicit(string[] whiteList)
        {
            Console.WriteLine("searcing for matches using **Explicit** algorithm");
            List<string> contentArray = _contents.Keys.ToList();
            for(int i = 0; i < whiteList.Length; ++i)
            {
                //clean the whiteList item of array indices
                string[] whiteListSegments = whiteList[i].Split('.');
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
                    _contents.Remove(contentTargets[k]);
                }
            }
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

            //if the parent is a property we need to walk up the tree
            if (parent.Type == JTokenType.Property)
            {
                ClipAndWalk(parent);
            }
            else
            {
                if (target.Type != JTokenType.Array)
                {
                    target.Remove();
                }
                //only remove arrays where they are empty
                //avoids mistaken clearance of leaves
                else if(target.Children().Count() == 0)
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

            //this generates a complete list of data fields
            List<string> contentList = new List<string>();
            contentList.Add("Node,Type,Frequency");
            
            //
            foreach (string index in pathList)
            {
                contentList.Add(index + "," + _types[index] + "," + _contents[index]);   
            }
            File.WriteAllLines("Data\\catalogue.csv", contentList.ToArray<string>());
        }
        RawData _rawTweets;
        List<Tweet> _tweets;
        List<JObject> _clippedTweets;
        Dictionary<string, int> _contents;
        Dictionary<string, string> _types;
        List<JObject> _tweetObjects;
        string[] _whiteList;
    }
}
