/////////////////////////////////////////////////////////////
////                                                     ////
////  ¬¬¬¬¬ |   | |¬¬¬¬ |¬¬¬¬ ¬¬¬¬¬ /¬¬¬¬ |     | |¬¬¬\  ////         
////    |   | | | |¬¬   |¬¬     |   |     |     | |¬¬¬/  ////                      
////    |   \¬¬¬/ \¬¬¬¬ \¬¬¬¬   |   \¬¬¬¬ \¬¬¬¬ | |      ////
////                                                     ////
/////////////////////////////////////////////////////////////

Copyright © University of Manchester 2020

Authors Benjamin Green, Lamiece Hassan

Version 2.3.0 "Gypsum"
==============================

][Description][

TweetClip takes json and clips it down to the fields you are intersted while maintaining relevant structures.

It was built to support work on twitter data, but will work with any json data.

It can handle very large data files (200,000+ tweets) on average this will require at least 1gb working memory.

It can handle multiple data files per run - must all use the same clipping mode and config file

==============================

][Contents][

root
 |--CommandLine.dll
 |--Newtonsoft.json.dll
 |--full-codex.codex
 |--codexHistory.cdxh
 |--TweetClip.exe
 |--Data
     |--Readme.txt

==============================

][Arguments][

-d | dataFilePath         | Relative path to the data 									| REQUIRED (when supplied alone triggers Index mode)
-c | configFilePath       | Relative path to the config file 								| OPTIONAL (when present triggers Clipping mode)
-o | outputFilename	  | Desired forename product output files							| OPTIONAL (when not present, dataFilename is used)
-e | explicitMatching     | explicit clipping mode 									| OPTIONAL (default matching algorithm)
-s | strictMatching       | strict clipping mode 									| OPTIONAL 
-w | wideMatching         | wide clipping mode 										| OPTIONAL 
-a | jsonArrayWrapper     | in clipping mode, output JSON is wrapped within an array 					| OPTIONAL
-k | elasticsearch	  | outputs newline delineated JSON with metadata sufficent to bulk import into the ELK stack   | OPTIONAL
-t | tableOutput          | outputs a CSV instead of json output 							| OPTIONAL
-p | prototypeOutput      | outputs a list of the all search results based on the current clip mode			| OPTIONAL
-x | symbolReplacement    | all usernames are replace in the output - symbols are randomly EACH time			| OPTIONAL
-r | refreshSymbols       | clean and restart the history resource if present						| OPTIONAL

=============================

][Core Modes][

- - index mode - -

   \\input files: {data}//
    All data must be input as a standard twitter .json collection; 1 line / tweet
    {data} can indicate an explicit file e.g. {data}.json - where this the case tweet clip will use only the specified file as source data
    {data} can also indicate a folder e.g. {data}/ - in this case all files with the .json extension within the specified folder will be used as source data
    in each case the program will close after the attempted processing of all target data

   This mode describes the structure of the supplied file
   
   //output files: catalogue.csv, index.txt\\
   catalogue.csv - a comprehensive model description of each filed found within the supplied json. This file includes one row for every array index found
   index.txt - a simplified form describing every structurally unique field found, e.g. 'a.b[0]' & 'a.b[1]' will produce a single row, 'a.b'

   >>EXAMPLE COMMAND<<
   take data.json and run index mode: 
	tweetclip -d data.json

- - clip mode - -
     
   \\input files: {data}.json, {cofig}.txt//
    All data must be input as a standard twitter .json collection; 1 line / tweet
    {data} can indicate an explicit file e.g. {data}.json - where this the case tweet clip will use only the specified file as source data
    {data} can also indicate a folder e.g. {data}/ - in this case all files with the .json extension within the specified folder will be used as source data
    in each case the program will close after the attempted processing of all target data
    {config}.json this file contains a whitelist of fields or patterns that you wish to be present in your export, 1 symbol per line; Fields can be copied directly from index.txt.

   //output files: catalogue.csv, index.txt\\
   clip mode will reduce the tweet to only the fields or patters specified within index.txt.
   The output filename body can be spcified with -o flag, but _### suffix will be added depenedent on output type. If this is not supplied the data file name will be used.
	
   There are three clipping modes explicit, strict and wide

   Explicit - this mode simply returns fields that exctly match any requested strings in the config file. Arrays will be handled without needing to specifiy necessary indices.
		>>EXAMPLES<<
		search "na" ==> null
		search "name" ==> null
		search "user.name" ==> user.name
		search "quoted_status.user.name" ==> quoted_status.user.name 

   Strict   - this mode will find fields whose entities exactly match the requested string. Where multiple terms are supplied (e.g. user.name) each term is evaluated separatly but order is respected 
		i.e. searching user.name will match […].user.name.[…] but would not match […].name.user.[…], […].X.user.[…] or […].name.X.[…].
		>>EXAMPLES<<
		search "na" ==> null
		search "name" ==> […].name.[…] - e.g. quoted_status.user.*name*; place.*name* etc
		search "user.name" ==> […].user.name.[…] - e.g. *user*.*name*; quoted_status.*user*.*name* etc
		search "quoted_status.user.name" ==> […].quoted_status.user.name.[…] - e.g. *quoted_status*.*user*.*name*; retweeted_status.*quoted_status*.*user*.*name*

   Wide     - this is the a very basic string search that will return any field whose path contains the specified search string.
		>>EXAMPLES<<
		search "na" ==> […]na[…] - e.g. entities.user_mentions.*na*me; entities.user_mentions.screen_*na*me; quoted_status.user.*na*me; place.full_*na*me; place.*na*me etc
		search "name" ==> […]name[…] - e.g. entities.user_mentions.screen_*name*; quoted_status.user.*name*; place.full_*name*; place.*name*; in_reply_to_screen_*name* etc
		search "user.name" ==> […]user.name[…]
		search "quoted_status.user.name" ==> […]quoted_status.user.name[…] - e.g. *quoted_status.user.name*; retweeted_status.*quoted_status*.*user*.*name*

   using jsonArrayWrapper turns {tweet}{tweet}{tweet} into [{tweet},{tweet},{tweet}].
   using tableOutput returns a tabular form of the whitelisted fields in UTF-8 format.
   using elasticsearch returns formatted nd-JSON with metadata ready to _bulk import into elasticsearch.
   using prototype returns a text list every serach term found with the present config file and clip mode.

   using symbolReplacement means that all handles and screen names are replaced with randomisesd human readable terms whereever they appear including within text. Note: this process maintains a history between runs to ensure consistent application of pseudonyms. This will also produce a table (~_codexKey.csv) containing each pair of original and replaced symbols.
   using refreshSymbols means that the system will delete the record of used symbols and will start again. In this case a given replacement symbol is likely to be reused for a new handle but will remain consistent in subsequent runs without this flag.

    >>EXAMPLE COMMAND<<
   take data.json and run clip mode using config.txt renaming output files as results~ (-o); using wide clip mode (-w) and csv output mode (-t) and replace all user symbols (-x) with consistent historic symbol set (-r): 
	tweetclip -d data.json -c config.txt -o results -w -t -x -r

=============================

][TODOs][

Jobs left to do:

Evaluate with users

