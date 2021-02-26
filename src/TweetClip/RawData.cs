using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweetClip
{
    class RawData
    {
        private RawData() { }

        public RawData(string data_filename)
        {
            _fileName = data_filename;
            List<string> dataList = new List<string>();

            try {
                StreamReader sr = File.OpenText(_fileName);

                for (int i = 0; i < 10000; ++i)
                {
                    if (!sr.EndOfStream)
                    {
                        dataList.Add(sr.ReadLine());
                    }
                }
                _data = dataList.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't read the text in this file");
            }
        }

        public RawData(StreamReader file, int blocksize)
        {
            List<string> dataList = new List<string>();

            for (int i = 0; i < blocksize; ++i)
                {
                    if (!file.EndOfStream)
                    {
                        dataList.Add(file.ReadLine().Trim(new char[] { '[', ',', ']' }));
                    }
                }
                _data = dataList.ToArray();
        }

        public string[] Data
        {
            get { return _data; }        
        }

        public void Clear()
        {
            _data = null;
        }

        string _fileName;
        string[] _data;
    }
}
