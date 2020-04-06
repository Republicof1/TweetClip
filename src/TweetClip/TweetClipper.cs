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
            _rawTweets = null;
            _whiteList = null;
        }

        public void ClipMode (string[] dataFiles, string[] configFiles)
        {
            _rawTweets = new RawData(dataFiles[0]);
            
            //---------------------    map all contents, truncate, pseudonomise, and serialise and send to file    -------------------------

            //map data and generate inital descriptive output
            _whiteList = File.ReadAllLines(configFiles[0]);

            List<string> clipTwStr = new List<string>();
            int count = 0;
            foreach (string tw in _rawTweets.Data)
            {
                Console.WriteLine("Discovering tweet \"" + ++count + "\"");
                _tweetObjects.Add(JObject.Parse(tw));
                _tweets.Add(new Tweet(_tweetObjects.Last()));
                _tweets.Last().Index(ref _contents);
            }
            foreach (JObject tweet in _tweetObjects)
            {
                Console.WriteLine("Clipping tweet \"" + ++count + "\"");
                string[] blackList = MakeBlackList(_whiteList);
                _clippedTweets.Add(ClipTweet(tweet, blackList));
                clipTwStr.Add(_clippedTweets.Last().ToString());
            }

            File.WriteAllLines("Data\\out.json", clipTwStr);

        }

        //make a composite list from the whitelist that we'll use to prune the copied tweet
        //TODO: MORE CAPABLE HANLDLING OF ARRAYS
        private string[] MakeBlackList(string[] whiteList)
        {
            foreach (string wlEntry in whiteList)
            {
                if (_contents.Keys.Contains<string>(wlEntry))
                {
                    _contents.Remove(wlEntry);
                }
            }
            //retrieve the list in reverse order to preserve array indices
            return _contents.Keys.Reverse().ToArray();
        }

        //create a copy of the tweet and prune it using a blacklist
        private JObject ClipTweet(JObject src, string[] blackList)
        {
            JObject jo = new JObject(src);
            foreach (string path in blackList)
            {
                //start walking through the blacklist
                ClipAndWalk(jo.SelectToken(path));
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
            }

            //if the parent is a property we need to walk up the tree
            if (parent.Type == JTokenType.Property)
            {
                //
                ClipAndWalk(parent);
            }
            else
            {
                target.Remove();
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

        public void IndexMode (string[] dataFiles)
        {           
            _rawTweets = new RawData(dataFiles[0]);

            //---------------------    map all contents, send to file and exit     -------------------------

            //explore all tweets, establish content nodes and output results in index file and catalogue files
            int count = 0;
            foreach (string tw in _rawTweets.Data)
            {
                Console.WriteLine("Exploring tweet \"" + ++count + "\"");
                JObject tweetObject = JObject.Parse(tw);
                _tweets.Add(new Tweet(tweetObject));
                _tweets.Last<Tweet>().Index(ref _contents);
            }

            List<string> orderedIndexList = _contents.Keys.ToList();
            orderedIndexList.Sort();

            File.WriteAllLines("Data\\index.txt", orderedIndexList);

            List<string> contentList = new List<string>();
            contentList.Add("Node,Value");
            
            foreach (string index in orderedIndexList)
            {

                contentList.Add(index + "," + _contents[index]);
            }
            File.WriteAllLines("Data\\catalogue.csv", contentList.ToArray<string>());
        }

        RawData _rawTweets;
        List<Tweet> _tweets;
        List<JObject> _clippedTweets;
        Dictionary<string, int> _contents;
        List<JObject> _tweetObjects;
        string[] _whiteList;
    }
}
