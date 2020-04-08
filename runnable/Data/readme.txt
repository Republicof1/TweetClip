/////////////////////////////////////////////////////////////
////                                                     ////
////  ¬¬¬¬¬ |   | |¬¬¬¬ |¬¬¬¬ ¬¬¬¬¬ /¬¬¬¬ |     | |¬¬¬\  ////         
////    |   | | | |¬¬   |¬¬     |   |     |     | |¬¬¬/  ////                      
////    |   \¬¬¬/ \¬¬¬¬ \¬¬¬¬   |   \¬¬¬¬ \¬¬¬¬ | |      ////
////                                                     ////
/////////////////////////////////////////////////////////////

Copyright © University of Manchester 2020

Authors Benjamin Green, Lamiece Hassan

Version 1.1.0
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

-d | dataFilePath | Relative path to the data json | required (when supplied alone triggers Index mode)
-c | configFilePath | Relative path to the config file | optional (when present triggers Clipping mode)
-e | explicitMatching | explicit clipping mode | optional (default matching algorithm)
-s | strictMatching | strict clipping mode | optional
-w | wideMatching | wide clipping mode | optional

=============================

][TODOs][

batch job
annonimity
csv output option

