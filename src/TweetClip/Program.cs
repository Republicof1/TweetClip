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

            [Option('c', "configFilePath", Required = false, HelpText = "Relative path to the config file")]
            public string ConfigFilePath { get; set; }

            [Option('s', "strictMatching", Required = false, HelpText = "strict match mode")]
            public bool StrictMode { get; set; }

            [Option('w', "wideMatching", Required = false, HelpText = "wide match mode")]
            public bool WideMode { get; set; }

            [Option('e', "explicitMatching", Required = false, HelpText = "explicit match mode (default)")]
            public bool ExplicitMode { get; set; }
        }

        public enum modeFlags
        {
            WIDE = 0,
            STRICT,
            EXPLICIT,
            INDEX
        }

        static void Main(string[] args)
        {
            //-------------------------    read in file(s)    -------------------------
            string[] dataFiles = null;
            string[] configFiles = null;
            TweetClipper tc = new TweetClipper();

            //get the target file from arguments (Options)
            modeFlags cMode = modeFlags.EXPLICIT;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    string directory = Directory.GetCurrentDirectory();

                    dataFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "Data\\" + opts.DataFilePath);
                    if (opts.ConfigFilePath != null)
                    {
                        configFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "Data\\" + opts.ConfigFilePath);
                    }
                    if (opts.WideMode)
                    {
                        cMode = modeFlags.WIDE;
                    }
                    //if both -s & -w go strict
                    if (opts.StrictMode)
                    {
                        cMode = modeFlags.STRICT;
                    }
                    //if -e included
                    if (opts.ExplicitMode)
                    {
                        cMode = modeFlags.EXPLICIT;
                    }
                });

            //index mode
            if (dataFiles != null && configFiles == null)
            {
                Console.WriteLine("config not included; index mode started\nPrcessing \"" + dataFiles[0] + "\"");
                if (cMode != modeFlags.WIDE)
                {
                    Console.WriteLine("note: clip modes ignored in this mode");
                }
                tc.IndexMode(dataFiles, modeFlags.INDEX);
            }
            //clip mode
            else if (dataFiles != null && configFiles != null)
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
