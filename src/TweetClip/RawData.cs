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

            try {
                _data = File.ReadAllLines(_fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't read the text in this file");
            }
        }

        public string[] Data
        {
            get { return _data; }        
        }

        string _fileName;
        string[] _data;
    }
}
