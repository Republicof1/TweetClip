using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TweetClip
{
    class Codex
    {
        private Codex() { }
        public Codex(string[] codexList)
        {
            //accessor indices
            _currentIndex = 0;
            _lhsIndex = 0;
            _rhsIndex = 1;

            _rawCodex = new List<string>(codexList);
            _replacementList = new List<string>();
            _codexSegments = new List<List<string>>();
            _proxyPairs = new Dictionary<string, string>();
            _suppliedProxys = new Dictionary<string, string>();

            
            //get the headers out of the codex
            List<string> listLimitSymbols = _rawCodex.FindAll(delegate (string symbol)
                {
                    if (symbol[0] == '=' || symbol[0] == '!')
                    {
                        return true;
                    }
                    return false;
                });

            
            List<int> indeces = new List<int>();

            //get start and end index
            for (int i = 0; i < listLimitSymbols.Count; ++i)
            {
                indeces.Add(_rawCodex.FindIndex(delegate (string symbol)
                   {
                       if (symbol == listLimitSymbols[i])
                       {
                           return true;
                       }
                       return false;
                   }));
            }

            //pick out the segments
            for (int i = 0; i < indeces.Count; i+=2)
            {
                int first = indeces[i] + 1;
                int count = indeces[i + 1] - first;

                _codexSegments.Add(_rawCodex.GetRange(first, count));
            }

            //generate some replacement symbols
            for(int i = 0; i < _codexSegments[_lhsIndex].Count; ++i)
            {
                for (int j = 0; j < _codexSegments[_rhsIndex].Count; ++j)
                {
                    _replacementList.Add(_codexSegments[_lhsIndex][i] + "_" + _codexSegments[_rhsIndex][j]);
                }
            }

            //shuffle the list
            _replacementList.Shuffle();

            //read in any existing pairs
            ReadHisory();
        }

        public string Proxy(string handle)
        {
            //often true with this is a screenName
            if (handle.Contains(" "))
            {
                handle = handle.Replace(" ", "-");
            }

            if (handle.Contains("@"))
            {
                //handle the rare case of inlines possessive appostrphe
                if (handle.Contains("'s"))
                {

                    handle = handle.Replace("'s", "");
                }

                if (handle.Contains("’s"))
                {

                    handle = handle.Replace("’s", "");
                }

                //trim any trailing non alphanumeric characters to avoid hanndle duplication
                //twitter's mysterious use of LRI and PDI characters is also handled here - '\u2066', '\u2069'
                handle = handle.TrimEnd(new char[] { '\u2066', '\u2069', '.', ',', ';', ':', '’', '\'', '\"', '(', ')', '[', ']', '{', '}' });
                handle = handle.TrimStart(new char[] { '\u2066', '\u2069', '.', ',', ';', ':', '’', '\'', '\"', '(', ')', '[', ']', '{', '}' });
            }

            //if we already have a proxy for this, use that
            if (_proxyPairs.Keys.Contains(handle))
            {
                return _proxyPairs[handle];
            }
            //if not allocate a new one - if were over bounds, refresh the list of proxies
            else
            {
                
                if(_currentIndex >= _replacementList.Count())
                {
                    RefreshProxyList();
                }
                _proxyPairs.Add(handle, _replacementList[_currentIndex]);
                return _replacementList[_currentIndex++];

            }
        }

        //if we run our of proxy names, then make some more
        private void RefreshProxyList()
        {
            _replacementList = new List<string>();
            _currentIndex = 0;

            //access more codex segments
            if (_rhsIndex >_codexSegments.Count)
            {
                ++_lhsIndex;
                _rhsIndex = _lhsIndex + 1;
            }
            else
            {
                ++_rhsIndex;
            }

            //generate some replacement symbols
            for (int i = 0; i < _codexSegments[_lhsIndex].Count; ++i)
            {
                for (int j = 0; j < _codexSegments[_rhsIndex].Count; ++j)
                {
                    _replacementList.Add(_codexSegments[_lhsIndex][i] + "_" + _codexSegments[_rhsIndex][j]);
                }
            }

            _replacementList.Shuffle();
            
        }

        public void WriteHisoryToTable(string filename)
        {
            List<string> writeFile = new List<string>();
            foreach (KeyValuePair<string, string> kvp in _proxyPairs)
            {
                //very helpful to me twitter banned the use of | in names
                writeFile.Add("\"" + kvp.Key + "\",\"" + kvp.Value + "\"");
            }

            //encode the text with UTF-8 BOM, this means Excel will pick up the encoding
            Encoding utf8WithBom = new UTF8Encoding(true);
            File.WriteAllLines(filename + "_codexKey.csv", writeFile.ToArray(), utf8WithBom);
        }

        //??will this need tamperproofing
        public void WriteHisory()
        {
            List<string> writeFile = new List<string>();
            foreach(KeyValuePair<string, string> kvp in _proxyPairs)
            {
                //very helpful to me twitter banned the use of | in names
                writeFile.Add(kvp.Key + "|@|" + kvp.Value);
            }

            File.WriteAllLines("codexHistory.cdxh", writeFile.ToArray());
        }


        //read in and clear the current history file
        private void ReadHisory()
        {
            //read the file
            string[] readFile = File.ReadAllLines("codexHistory.cdxh");

            for (int i = 0; i < readFile.Length; ++i)
            {
                //split the pair and add it
                string[] pair = readFile[i].Split(new string[] { "|@|" }, StringSplitOptions.None);
                //add to a collection of items that have been ingested
                _suppliedProxys.Add(pair[0],pair[1]);
                //add to the master proxy index
                _proxyPairs.Add(pair[0], pair[1]);
                //remove this name from the replacement list
                _replacementList.Remove(pair[1]);
            }
        }

        //indices for accessing stuff
        int _currentIndex;
        int _lhsIndex;
        int _rhsIndex;

        //the codex
        List<string> _rawCodex;
        List<List<string>> _codexSegments;
        //collection by original screennames to proxy screennames
        Dictionary<string, string> _proxyPairs;
        //list of all possible replacement names
        List<string> _replacementList;
        //collection by original names to proxy names
       
        //collection supplied original names & proxy name pairs
        Dictionary<string, string> _suppliedProxys;
    }

    //shuffle the names
    //taken from https://stackoverflow.com/questions/273313/randomize-a-listt
    static internal class Croupier
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = TSRandom.LocalRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    static internal class TSRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random LocalRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
}


