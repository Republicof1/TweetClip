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
        //global folder address
        public static string OUTPUT_FOLDER = "Data\\";

        //args definitions
        public class Options
        {
            [Option('d', "dataFilePath", Required = true, HelpText = "Relative path to the data json")]
            public string DataFilePath { get; set; }

            [Option('c', "configFilePath", Required = false, HelpText = "Relative path to the config file")]
            public string ConfigFilePath { get; set; }

            [Option('o', "outputFilePath", Required = false, HelpText = "if prenset, the name of the output file, note extesions will be ignored")]
            public string OutputFilePath { get; set; }

            [Option('s', "strictMatching", Required = false, HelpText = "strict match mode")]
            public bool StrictMode { get; set; }

            [Option('w', "wideMatching", Required = false, HelpText = "wide match mode")]
            public bool WideMode { get; set; }

            [Option('e', "explicitMatching", Required = false, HelpText = "explicit match mode (default)")]
            public bool ExplicitMode { get; set; }

            [Option('a', "outputArray", Required = false, HelpText = "if present, output json within an annonyous array")]
            public bool ArrayOutput { get; set; }

            [Option('k', "outputArray", Required = false, HelpText = "if present, output json in a format ready for upload to ELK stack")]
            public bool ElasticOutput { get; set; }

            [Option('t', "outputTable", Required = false, HelpText = "if present, output csv table")]
            public bool CSVOutput { get; set; }

            [Option('p', "prototype run", Required = false, HelpText = "if present, produces a list of the fields returned given a specified search")]
            public bool PrototypeOutput { get; set; }

            [Option('x', "symbolise", Required = false, HelpText = "if present, replaces screen names and @names with proxy symbols")]
            public bool Symbolise { get; set; }

            
        }

        public enum modeFlags
        {
            WIDE = 0,
            STRICT,
            EXPLICIT,
            INDEX
        }

        public enum outputFlags
        {
            RAW_JSON = 0,
            JSON_ARRAY,
            ELASTIC_JSON,
            CSV,
            PROTOTYPE
        }

        static void Main(string[] args)
        {
            //-------------------------    read in file(s)    -------------------------
            string[] dataFiles = null;
            string[] configFiles = null;
            string[] codexFiles = null;
            string outputFilename = "";
            TweetClipper tc = new TweetClipper();
            bool symbolise = false;

            //get the target file from arguments (Options)
            modeFlags cMode = modeFlags.EXPLICIT;
            outputFlags oMode = outputFlags.RAW_JSON;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    string directory = Directory.GetCurrentDirectory();

                    dataFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), OUTPUT_FOLDER + opts.DataFilePath);
                    if (opts.ConfigFilePath != null)
                    {
                        configFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), OUTPUT_FOLDER + opts.ConfigFilePath);
                    }
                    if(opts.OutputFilePath != null)
                    {
                        outputFilename = opts.OutputFilePath;
                    }
                    else
                    {
                        Console.CursorTop = 2;
                        Console.WriteLine("using datafile name as output name");
                        outputFilename = opts.DataFilePath;
                        Console.CursorTop = 0;
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
                    //if -k included
                    if (opts.ElasticOutput)
                    {
                        oMode = outputFlags.ELASTIC_JSON;
                    }
                    //if -a included
                    if (opts.ArrayOutput)
                    {
                        oMode = outputFlags.JSON_ARRAY;
                    }
                    //if -t, overwrite -a
                    if (opts.CSVOutput)
                    {
                        oMode = outputFlags.CSV;
                    }
                    //if -t, overwrite -a
                    if (opts.PrototypeOutput)
                    {
                        oMode = outputFlags.PROTOTYPE;
                    }

                    symbolise = opts.Symbolise;
                });

            if (symbolise)
            {
                codexFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "full-codex.codex");
            }

            Console.Clear();
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
                tc.ClipMode(dataFiles, configFiles, codexFiles, outputFilename, cMode, oMode);
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
