/////////////////////////////////////////////////////////////
////                                                     ////
////  ¬¬¬¬¬ |   | |¬¬¬¬ |¬¬¬¬ ¬¬¬¬¬ /¬¬¬¬ |     | |¬¬¬\  ////         
////    |   | | | |¬¬   |¬¬     |   |     |     | |¬¬¬/  ////                      
////    |   \¬¬¬/ \¬¬¬¬ \¬¬¬¬   |   \¬¬¬¬ \¬¬¬¬ | |      ////
////                                                     ////
/////////////////////////////////////////////////////////////

Copyright © University of Manchester 2020

Authors Benjamin Green, Lamiece Hassan

Version 1.4.0
==============================

][Description][

TweetClip takes json and clips it down to the fields you are intersted while maintaining relevant structures.

It was built to support work on twitter data, but will work with any json data.

==============================

][Contents][

root
 |--CommandLine.dll
 |--Newtonsoft.json.dll
 |--TweetClip.exe
 |--Data
     |--Readme.txt

==============================

][Arguments][

-d | dataFilePath     | Relative path to the data json | REQUIRED (when supplied alone triggers Index mode)
-c | configFilePath   | Relative path to the config file | OPTIONAL (when present triggers Clipping mode)
-e | explicitMatching | explicit clipping mode | OPTIONAL (default matching algorithm)
-s | strictMatching   | strict clipping mode | OPTIONAL 
-w | wideMatching     | wide clipping mode | OPTIONAL 
-a | jsonArrayWrapper | in clipping mode, output JSON is wrapped within an array 
-t | tableOutput      | outputs a CSV instead of json output

=============================

][Core Modes][

- - index mode - -

   \\input files: {data}.json//
    {data}.json contains your json data

   This mode describes the structure of the supplied file
   
   //output files: catalogue.csv, index.txt\\
   catalogue.csv - a comprehensive model description of each filed found within the supplied json. This file includes one row for every array index found
   index.txt - a simplified form describing every structurally unique field found, e.g. 'a.b[0]' & 'a.b[1]' will produce a single row, 'a.b'

- - clip mode - -
   
   \\input files: {data}.json, {cofig}.txt//
    {data}.json contains your json data
    {config}.json this file contains a whitelist of fields or patterns that you wish to be present in your export, 1 symbol per line; Fields can be copied directly from index.txt.

   //output files: catalogue.csv, index.txt\\
   clip mode will reduce the tweet to only the fields or patters specified within index.txt.
   There are three clipping modes explicit, strict and wide

   Explicit - this mode simply returns exctly what is requested in the config file. In this sense you need to supply the fields you want to see. Arrays will be handled without needing to specifiy necessary indices.
   Strict   - this mode will find fields that match the supplied pattern and all patterns that contain that pattern. e.g. a.b will return a.b, x.a.b, a.b.z, z.d.a.b.s etc || it will not return b.a b or a. 
   Wide     - more of a prosepctive search, this will find any field that partially matches the pattern e.g. do will return do, a.b.do, dado, dado.a.b, a.b.cadon, z.adonis.s etc.

   using jsonArrayWrapper turns {tweet}{tweet}{tweet} into [{tweet},{tweet},{tweet}]
   using tableOutput returns a tabular form of the whitelisted fields in UTF-8 format 

=============================

][TODOs][

Jobs left to do:

batch job
annonimity

