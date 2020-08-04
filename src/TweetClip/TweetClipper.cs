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

namespace TweetClip
{
    class TweetClipper
    {
        public TweetClipper(int blocksize = 2000)
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
            _blockSize = blocksize;
            _processCount = 0;

        }

        public delegate string[] MakeBlackList();
        public delegate processStage ProcessOutput(processStage stage);

        public void ClipMode (string[] dataFiles, string[] configFiles, string[] codexFiles, string outFilename, modeFlags mode, outputFlags output)
        {
            _outputFilename = OUTPUT_FOLDER + outFilename;

            //stip off any extensions
            if (outFilename.Contains('.'))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("output file extension removed...");
                Console.ForegroundColor = ConsoleColor.White;
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
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Building proxy symbols from codex");
                Console.ForegroundColor = ConsoleColor.White;

                _codex = new Codex(File.ReadAllLines(codexFiles[0]));
            }

            StreamReader file = null;

            int fileIndex = 0;

            do
            {
                try
                {
                    Console.WriteLine("\nClipping " + dataFiles[fileIndex]);
                    file = File.OpenText(dataFiles[fileIndex]);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Can't read the text in this file");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                //---------------------    map all contents, truncate, pseudonomise, and serialise and send to file    -------------------------

                //map data and generate inital descriptive output
                string[] whiteListCache = File.ReadAllLines(configFiles[0]);

                _processCount = 0;
                processStage pStage = processStage.FIRST;

                do
                {
                    _whiteList = new string[whiteListCache.Length];
                    Array.Copy(whiteListCache, _whiteList, whiteListCache.Length);

                    _rawTweets = new RawData(file, _blockSize);

                    for (int i = 0; i < _rawTweets.Data.Length; ++i)
                    {
                        Console.CursorLeft = 0;
                        Console.Write("Discovering tweet \"" + ++_processCount + "\"");

                        _tweetObjects.Add(JObject.Parse(_rawTweets.Data[i]));
                        _tweets.Add(new Tweet(_tweetObjects.Last(), mode, _codex));
                        _tweets.Last().Index(ref _contents, ref _types);
                    }
                    Console.SetCursorPosition(0, Console.CursorTop + 1);

                    //preparing files 
                    pStage = _processOutputPtr(pStage);

                    if (file.EndOfStream)
                    {
                        pStage = processStage.LAST;

                    }

                    //clear these out now they've done their work for the sweep
                    //force GC to clear up unused object
                    _rawTweets.Clear();
                    _tweets.Clear();
                    _tweetObjects.Clear();
                    _contents.Clear();
                    _types.Clear();
                    _clippedTweets.Clear();
                    _clipTwStr.Clear();

                } while (!file.EndOfStream && pStage != processStage.COMPLETE);

                Console.CursorTop = Console.CursorTop + 1;
                Console.CursorLeft = 0;

                if (codexFiles != null)
                {
                    _codex.WriteHisory();

                    _codex.WriteHisoryToTable(_outputFilename);
                }
            } while (++fileIndex<dataFiles.Length);
        }

        //using the tweet object version of the data
        private processStage ProcessOutput_PrototypeList(processStage stage = processStage.IN_PROGRESS)
        {
            Console.Write("preparing **PROTOTYPE LIST**");
            //process for CSV
            _blackListPtr();

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

            Console.SetCursorPosition(0, Console.CursorTop + 1);
            Console.WriteLine("Prototype list constructed, saving to file");

            List<string> outWhiteList = _whiteList.ToList();
            outWhiteList.Sort();

            File.WriteAllLines(_outputFilename + "_prototype.txt", outWhiteList);

            return processStage.COMPLETE;
        }

        //using the tweet object version of the data
        private processStage ProcessOutput_CSV(processStage stage = processStage.IN_PROGRESS)
        {
            Console.Write("Packaging tweets as **CSV TABLE**");
            //process for CSV
            _blackListPtr();

            int count = 0;
 
            Dictionary<string, int> rowIndex = new Dictionary<string, int>();

            List<string> table = new List<string>();
            List<string> tableRow = new List<string>();

            
            //is this is the first run clear out the file
            if (stage == processStage.FIRST)
            {
                //clear the file
                File.WriteAllText(_outputFilename + "_table.csv", "");

                //add the header
                for (int j = 0; j < _whiteList.Length; ++j)
                {
                    tableRow.Add(_whiteList[j]);
                }
                tableRow[tableRow.Count - 1] = tableRow.Last().TrimEnd(',');
                table.Add(tableRow.Aggregate((a, b) => a + "," + b));
                stage = processStage.IN_PROGRESS;
            }

            Console.SetCursorPosition(0, Console.CursorTop + 1);
            for (int i = 0; i < _tweets.Count; ++i)
            {
                tableRow = new List<string>();
                Dictionary<string,string> tw = _tweets[i].Nodes;
                List<string> twKeys = tw.Keys.ToList();

                Console.CursorLeft = 0;
                Console.Write("preparing sweep row \"" + ++count + "/" + _blockSize + "\"");

                for (int j = 0; j < _whiteList.Length; ++j)
                {
                    string lookUp = _whiteList[j];

                    if (twKeys.Contains(lookUp))
                    {
                        //replace internal " with utf left double quotation to avoid misread of truncated text that contains a "
                        if (lookUp == "full_text")
                        {
                            string ft = tw[lookUp].Replace('"', '\u201C');
                            tableRow.Add("\"" + ft + "\"");
                        }
                        else
                        {
                            tableRow.Add("\"" + tw[lookUp] + "\"");
                        }
                    }
                    else
                    {
                        tableRow.Add("\"\"");
                    }                   
                }
                table.Add(tableRow.Aggregate((a, b) => a + "," + b));
            }
            Console.SetCursorPosition(0, Console.CursorTop + 1);

            //encode the text with UTF-8 BOM, this means Excel will pick up the encoding
            Encoding utf8WithBom = new UTF8Encoding(true);
            File.AppendAllLines(_outputFilename + "_table.csv", table, utf8WithBom);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("sweep written to table");
            Console.ForegroundColor = ConsoleColor.White;

            return stage;
        }

        //process for JSON
        private void ProcessJSON()
        {
            int count = 0;

            foreach (JObject tweet in _tweetObjects)
            {
                Console.CursorLeft = 0;
                Console.Write("Clipping sweep tweet \"" + ++count + "/" + _blockSize + "\"");

                string[] blackList = _blackListPtr();
                _clippedTweets.Add(ClipTweet(tweet, blackList));
                //remove all new line
                _clipTwStr.Add(_clippedTweets.Last().ToString().Replace("\r\n", "").Replace("  ", ""));
            }
            Console.SetCursorPosition(0, Console.CursorTop + 1);
        }

        //using the string version of the data
        private processStage ProcessOutput_Array(processStage stage = processStage.IN_PROGRESS)
        {
            //add the header
            if (stage == processStage.FIRST)
            {
                File.WriteAllText(_outputFilename + "_array.json", "");
            }

            ProcessJSON();

            //concat is too slow
            List<string> liFile = new List<string>();

            string file = _clipTwStr.Aggregate((a, b) => a + ",\r\n" + b);

            if (stage == processStage.FIRST)
            {
                file = "[" + file + ",\r\n";
                stage = processStage.IN_PROGRESS;
            }
            else if (stage == processStage.LAST)
            {
                file = file + "]";
            }
            else if (stage == processStage.IN_PROGRESS)
            {
                file = file + ",\r\n";
            }

            File.AppendAllText(_outputFilename + "_array.json", file);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("sweep written to file as **JSON ARRAY**");
            Console.ForegroundColor = ConsoleColor.White;

            return stage;
        }

        //using the string version of the data
        private processStage ProcessOutput_Elastic(processStage stage = processStage.IN_PROGRESS)
        {
            ProcessJSON();
            StringBuilder sb = new StringBuilder();

            Console.WriteLine("Packaging tweets as **ELK COMPATIBLE JSON**");
            string file = "";

            int count = _processCount - _blockSize;
            file = _clipTwStr.Aggregate((a, b) => a + "\r\n{ \"index\" : { \"_id\" : \"" + ++count + "\" } }\r\n" + b);

            //is this is the first run clear out the file
            if (stage == processStage.FIRST)
            {
                File.WriteAllText(_outputFilename + "_ELK.json", "");
                stage = processStage.IN_PROGRESS;
            }

            //add necessary closing newline
            string trailingInfo = "";
            if (stage == processStage.LAST)
            {
                trailingInfo = "\n";
            }

            //add first and last metadata blocks
            sb.Append("{ \"index\" : { \"_id\" : \"" + (_processCount - _blockSize) + "\" } }\r\n" + file + "{ \"index\" : { \"_id\" : \"" + (_processCount - 1) + "\" } }\r\n" + trailingInfo);
            
            File.AppendAllText(_outputFilename + "_ELK.json", sb.ToString());
            sb.Clear();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("sweep written to file as **ELK COMPATIBLE JSON**");
            Console.ForegroundColor = ConsoleColor.White;

            return stage;
        }

        //using the string version of the data
        private processStage ProcessOutput_RawJSON(processStage stage = processStage.IN_PROGRESS)
        {
            ProcessJSON();

            Console.WriteLine("Packaging tweets as **RAW JSON** and saving to file");

            //concat is too slow - use aggrigate
            string file = "";
            file = _clipTwStr.Aggregate((a, b) => a + "\r\n" + b);

            //is this is the first run clear out the file
            if (stage == processStage.FIRST)
            {
                File.WriteAllText(_outputFilename + "_raw.json", file);
                stage = processStage.IN_PROGRESS;
            }

            File.AppendAllText(_outputFilename + "_raw.json", file);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("sweep written to file as **RAW JSON**");
            Console.ForegroundColor = ConsoleColor.White;

            return stage;
        }
        //make a composite list from the whitelist that we'll use to prune the copied tweet
        
        //partial match ignoring indices
        private string[] MakeBlackList_Wide()
        {

            Console.Write(" [using **Wide** algorithm]");
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
            Console.Write(" [using **Strict** algorithm]");

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

            Console.Write(" [using **Explicit** algorithm]");
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
            
            StreamReader file = null;

            int fileIndex = 0;

            //open file, close file, if files remain { open file }
            do
            {
                try
                {
                    Console.WriteLine("\nIndexing " + dataFiles[fileIndex]);
                    file = File.OpenText(dataFiles[fileIndex]);
                }
                catch (Exception e)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine("Tweetclip - run failed");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Can't read the text in this file");
                    Console.WriteLine("Please check the file is available and not open");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White; ;
                }

                int count = 0;

                do
                {
                    _rawTweets = new RawData(file, _blockSize);

                    //---------------------    map all contents, send to file and exit     -------------------------

                    //explore all tweets, establish content nodes and output results in index file and catalogue files

                    foreach (string tw in _rawTweets.Data)
                    {
                        Console.CursorLeft = 0;
                        Console.Write("Exploring tweet \"" + ++count + "\"");

                        JObject tweetObject = JObject.Parse(tw);
                        _tweets.Add(new Tweet(tweetObject, mode));
                        _tweets.Last<Tweet>().Index(ref _contents, ref _types);
                    }

                    //force GC to clear up unused object
                    _rawTweets.Clear();
                    _tweets.Clear();
                } while (!file.EndOfStream);
            } while (++fileIndex < dataFiles.Length);

            Console.SetCursorPosition(0, Console.CursorTop + 1);
            //this creates a list of element ignoreing array contents
            List<string> orderedPathList = new List<string>();

            //alphabetically order these paths
            List<string> pathList = _contents.Keys.ToList();
            pathList.Sort();
            //optimisation
            string[] pathArray = pathList.ToArray();

            for (int i = 0; i < pathArray.Length; ++i)
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
            Console.WriteLine("Exploration complete!");
            Console.WriteLine("Index file created");
            //this generates a complete list of data fields
            List<string> contentList = new List<string>();
            contentList.Add("Node,Type,Frequency");

            //
            foreach (string index in pathList)
            {
                contentList.Add(index + "," + _types[index] + "," + _contents[index]);
            }
            File.WriteAllLines("Data\\catalogue.csv", contentList.ToArray<string>());
            Console.WriteLine("Catalogue file created");

            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Tweetclip - run success");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            int fff = 0;
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
        int _blockSize;
        int _processCount;
    }
}
