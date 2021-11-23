using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                string errorVal = e.Message;
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
                    //get the first line of the file
                    string line = file.ReadLine();
                    
                    //if the file contains only one line it's *probably* an array
                    if (file.EndOfStream && i == 0)
                    {
                        Object obj = JsonConvert.DeserializeObject(line);
                        //test if this is actually an array
                        if(obj.GetType() == typeof(JArray))
                        {
                            List<JToken> arrayContent = new List<JToken>(((JArray)obj).Children());
                            foreach(JToken contObj in arrayContent)
                            {
                                dataList.Add(JsonConvert.SerializeObject(contObj));
                            }
                        }

                        //this is for the incredibly rare case of someone clipping a single tweet...
                        else if (obj.GetType() == typeof(JObject))
                        {
                            dataList.Add(JsonConvert.SerializeObject(obj));
                        }

                        break;
                    }
                    //if this is not a one line array, but is either JSONL or multiline JSON array
                    else
                    {
                        //no point deserialising and serialising, just trim each line
                        dataList.Add(line.Trim(new char[] { '[', ',', ']' }));
                    }
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
