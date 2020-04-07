using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace TweetClip
{
    class Program
    {
        //args definitions
        public class Options
        {
            [Option('d', "dataFilePath", Required = true, HelpText = "Relative path to the data json")]
            public string DataFilePath { get; set; }

            [Option('c', "dataDictionaryFilePath", Required = false, HelpText = "Relative path to the config file")]
            public string ConfigFilePath { get; set; }

            [Option('s', "dataDictionaryFilePath", Required = false, HelpText = "strict match mode")]
            public bool StrictMode { get; set; }

            [Option('e', "dataDictionaryFilePath", Required = false, HelpText = "explicit match mode")]
            public bool ExplicitMode { get; set; }
        }
        public enum clipMode
        {
            LOOSE = 0,
            STRICT,
            EXPLICIT
        }

        static void Main(string[] args)
        {

            //-------------------------    read in file(s)    -------------------------
            string[] dataFiles = null;
            string[] configFiles = null;
            TweetClipper tc = new TweetClipper();

            //get the target file from arguments (Options)
            clipMode cMode = clipMode.LOOSE;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    string directory = Directory.GetCurrentDirectory();

                    dataFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "Data\\" + opts.DataFilePath);
                    if (opts.ConfigFilePath != null)
                    {
                        configFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "Data\\" + opts.ConfigFilePath);
                    }
                    if(opts.StrictMode)
                    {
                        cMode = clipMode.STRICT;
                    }
                    //if both -s & -e go explicit
                    if(opts.ExplicitMode)
                    {
                        cMode = clipMode.EXPLICIT;
                    }
                });

            if (configFiles != null && configFiles == null)
            {
                
                Console.WriteLine("config not included; index mode started\nPrcessing \"" + dataFiles[0] + "\"");
                if (cMode != clipMode.LOOSE)
                {
                    Console.WriteLine("note: clip mode ignored in this mode");
                }
                tc.IndexMode(dataFiles);
            }
            else if (dataFiles.Length > 0 && configFiles.Length > 0)
            {
                Console.WriteLine("Config found, clipping mode started\nProcessing \"" + dataFiles[0] + "\"");
                tc.ClipMode(dataFiles, configFiles, cMode);
            }
            else
            {
                Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(opts =>
               {
                   Console.WriteLine("file \"" + opts.DataFilePath + "\" could not be found");
               });
            }

        }
    }
}
