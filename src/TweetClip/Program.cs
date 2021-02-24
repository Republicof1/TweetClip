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
        //JSON format flexibility - array or JSONL
        public static string VERSION = "v2.6.0; \"Sapphire\"";
        //global folder address
        public static string OUTPUT_FOLDER = "Data\\";

        //args definitions
        public class Options
        {
            [Option('d', "dataFilePath", Required = true, HelpText = "Relative path to the data json")]
            public string DataFilePath { get; set; }

            [Option('c', "configFilePath", Required = false, HelpText = "Relative path to the config file")]
            public string ConfigFilePath { get; set; }

            [Option('o', "outputFilePath", Required = false, HelpText = "if present, the name of the output file, note extesions will be ignored")]
            public string OutputFilePath { get; set; }

            [Option('s', "strictMatching", Required = false, HelpText = "strict match mode")]
            public bool StrictMode { get; set; }

            [Option('w', "wideMatching", Required = false, HelpText = "wide match mode")]
            public bool WideMode { get; set; }

            [Option('e', "explicitMatching", Required = false, HelpText = "explicit match mode (default)")]
            public bool ExplicitMode { get; set; }

            [Option('a', "outputArray", Required = false, HelpText = "if present, output json within an annonyous array")]
            public bool ArrayOutput { get; set; }

            [Option('k', "outputELKComplient", Required = false, HelpText = "if present, output json in a format ready for upload to ELK stack")]
            public bool ElasticOutput { get; set; }

            [Option('t', "outputTable", Required = false, HelpText = "if present, output csv table")]
            public bool CSVOutput { get; set; }

            [Option('p', "prototype run", Required = false, HelpText = "if present, produces a list of the fields returned given a specified search")]
            public bool PrototypeOutput { get; set; }

            [Option('x', "symbolise", Required = false, HelpText = "if present, replaces screen names and @names with proxy symbols")]
            public bool Symbolise { get; set; }

            [Option('r', "RefreshSymbols", Required = false, HelpText = "if present, refreshes the symbol history meaining before running")]
            public bool RefreshSymbols { get; set; }
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

        public enum processStage
        {
            FIRST = 0,
            IN_PROGRESS,
            LAST,
            COMPLETE,
            TOTAL
        }

        static void Main(string[] args)
        {
            //-------------------------    read in file(s)    -------------------------
            string[] dataFiles = null;
            string[] configFiles = null;
            string[] codexFiles = null;
            string[] exclusionList = null; //this list contains elements that should not be exported even when requested when -x symbolisation is enabled
            string outputFilename = "";
            TweetClipper tc = new TweetClipper();
            bool symbolise = false;
            bool datafileAsOutput = false;

            //get the target file from arguments (Options)
            modeFlags cMode = modeFlags.EXPLICIT;
            outputFlags oMode = outputFlags.JSON_ARRAY;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    string directory = Directory.GetCurrentDirectory();

                    if (opts.DataFilePath.Contains("json"))
                    {
                        dataFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), OUTPUT_FOLDER + opts.DataFilePath);
                    }
                    else
                    {
                        if (opts.DataFilePath.Last() == '\\' || opts.DataFilePath.Last() == '/')
                        {
                            dataFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), OUTPUT_FOLDER + opts.DataFilePath + "*.json*");
                        }
                        else
                        {
                            dataFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), OUTPUT_FOLDER + opts.DataFilePath + "\\*.json*");
                        }
                    }                   

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
                        datafileAsOutput = true;
                        outputFilename = opts.DataFilePath;
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

                    if (opts.RefreshSymbols)
                    {
                        File.WriteAllLines("codexHistory.cdxh", new string[] { });
                    }

                    symbolise = opts.Symbolise;
                });

            if (symbolise)
            {
                codexFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "full-codex.codex");
                exclusionList = Directory.GetFiles(Directory.GetCurrentDirectory(), "exclusionFields.excf");
            }

            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(">> Tweetclip - " + VERSION + " <<");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            if(datafileAsOutput)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("using datafile name as output name");
                Console.ForegroundColor = ConsoleColor.White;
            }

            //index mode
            if (dataFiles != null && configFiles == null)
            {
                if (dataFiles.Length == 0)
                {
                    Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(opts =>
                   {
                       Console.BackgroundColor = ConsoleColor.Red;
                       Console.ForegroundColor = ConsoleColor.Black;
                       Console.WriteLine("Tweetclip - run failed");
                       Console.BackgroundColor = ConsoleColor.Black;
                       Console.ForegroundColor = ConsoleColor.Red;
                       Console.WriteLine("file \"" + opts.DataFilePath + "\" could not be found!");
                       Console.WriteLine("Please check filepath is correct before running again");
                       Console.BackgroundColor = ConsoleColor.Black;
                       Console.ForegroundColor = ConsoleColor.White;
                   });
                   return;
                }

                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Index mode started: Prcessing \"" + dataFiles[0] + "\"");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;

                tc.IndexMode(dataFiles, modeFlags.INDEX);

                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Tweetclip - index run success!");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
            }
            //clip mode
            else if (dataFiles != null && configFiles != null)
            {
                if (dataFiles.Length == 0 || configFiles.Length == 0)
                {
                    Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(opts =>
                   {
                       Console.BackgroundColor = ConsoleColor.Red;
                       Console.ForegroundColor = ConsoleColor.Black;
                       Console.WriteLine("Tweetclip - run failed");
                       Console.BackgroundColor = ConsoleColor.Black;
                       Console.ForegroundColor = ConsoleColor.Red;
                       if (configFiles.Length == 0)
                       {
                           Console.WriteLine("file \"" + opts.ConfigFilePath + "\" could not be found!");
                       }

                       if (dataFiles.Length == 0)
                       {
                           Console.WriteLine("file \"" + opts.DataFilePath + "\" could not be found!");
                       }
                       Console.WriteLine("Please check filepath(s) before running again");
                       Console.BackgroundColor = ConsoleColor.Black;
                       Console.ForegroundColor = ConsoleColor.White;
                   });
                   return;
                }

                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Clipping mode started: Processing \"" + dataFiles[0] + "\"");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                tc.ClipMode(dataFiles, configFiles, codexFiles, exclusionList, outputFilename, cMode, oMode);

                Console.BackgroundColor = ConsoleColor.Green;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Tweetclip - clipping run success!");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
