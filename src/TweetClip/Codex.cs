using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _screenNameList = new List<string>();
            _codexSegments = new List<List<string>>();
            _nameList = new List<string>();
            _screennameProxys = new Dictionary<string, string>();
            _nameProxys = new Dictionary<string, string>();

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
                    _screenNameList.Add(_codexSegments[_lhsIndex][i] + "_" + _codexSegments[_rhsIndex][j]);
                }
            }

            //shuffle the list
            _screenNameList.Shuffle();
        }

        public string Proxy(string handle)
        {
            if (handle.Contains(" "))
            {
                handle.Replace(" ", "_");
            }

            if (_screennameProxys.Keys.Contains(handle))
            {
                return _screennameProxys[handle];
            }
            else
            {
                _screennameProxys.Add(handle, _screenNameList[_currentIndex]);
                if(_currentIndex >= _screenNameList.Count())
                {
                    RefreshProxyList();
                }
                return _screenNameList[_currentIndex++];

            }
        }

        //if we run our of proxy names, then make some more
        private void RefreshProxyList()
        {
            _screenNameList = new List<string>();
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
                    _screenNameList.Add(_codexSegments[_lhsIndex][i] + "_" + _codexSegments[_rhsIndex][j]);
                }
            }

            _screenNameList.Shuffle();
            
        }

        //indices for accessing stuff
        int _currentIndex;
        int _lhsIndex;
        int _rhsIndex;

        //the codex
        List<string> _rawCodex;
        List<List<string>> _codexSegments;
        //collection by original screennames to proxy screennames
        Dictionary<string, string> _screennameProxys;
        //list of all possible screennames
        List<string> _screenNameList;
        //collection by original names to proxy names
        Dictionary<string, string> _nameProxys;
        //collection of all possible
        List<string> _nameList;
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


