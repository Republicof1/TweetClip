# TweetClip
### *Version 2.6.5 "Green Sapphire"*
**Authors Benjamin Green, Lamiece Hassan**\
*Copyright © University of Manchester 2020-2021*
-----------------------------------

# Description

TweetClip is a command line tool that takes JSON data and clips it down to the fields you are interested in while maintaining relevant data structures.\
It was built to support work on Twitter data, but will work with data in a JSONL, JSON single line array or JSON multiline array data.\
It can handle very large data files (200,000+ tweets) - on average this will require at least 1GB working memory.\
It can handle multiple data files per run - these must all use the same clipping mode and config file.

# Content Layout
ROOT\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|- **runnable**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--CommandLine.dll**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--Newtonsoft.json.dll**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--full-codex.codex**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--codexHistory.cdxh**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--TweetClip.exe**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--Data**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**|--Readme.txt**\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|--src\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|--Packages\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|--TweetClip -> *src files*\
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|--TweetClip.sln


# Arguments

|flag|command|description|cardinality|
|-|-|-|-|
|**-d** | dataFilePath         | Relative path to the data 									| REQUIRED\(when supplied alone triggers Index mode)|
|**-c** | configFilePath| Relative path to the config file 								| OPTIONAL\(when present triggers Clipping mode)|
**-o** | outputFilename	  | Desired forename product output files							| OPTIONAL\(when not present, dataFilename is used)
**-e** | explicitMatching     | explicit clipping function 									| OPTIONAL\(default matching function)
**-s** | strictMatching       | strict clipping function 									| OPTIONAL 
**-w** | wideMatching         | wide clipping function 										| OPTIONAL 
**-a** | jsonArrayWrapper     | in clipping mode, output JSON is wrapped within an array 					| OPTIONAL
**-k** | elasticsearch	  | outputs newline delineated JSON with metadata sufficent to bulk import into the ELK stack   | OPTIONAL
**-t** | tableOutput          | outputs a CSV instead of JSON output 							| OPTIONAL
**-p** | prototypeOutput      | outputs a list of the all search results based on the current clip mode			| OPTIONAL
**-x** | symbolReplacement    | all usernames are replace in the output - symbols are randomly EACH time			| OPTIONAL
**-r** | refreshSymbols       | clean and restart the history resource if present						| OPTIONAL


# Usage Modes
## Index Mode
The purpose of this mode is to describe the structure of the supplied file(s)

### input files: {*data*}.json *or* {*data*}
All data must be inputted as a standard Twitter .json collection; 1 line per tweet.\
TweetClip will accept {*data*} to indicate either a file or a directory.
{*data*} can indicate an explicit file e.g. {*data*}.json - where this the case TweetClip will use only the specified file as source data.\
{*data*} can also indicate a folder e.g. {*data*}/ - in this case all files with the .json extension within the specified folder will be used as source data.\
In each case the program will close after the attempted processing of all target data.
   
### output files: catalogue.csv, index.txt
catalogue.csv - a comprehensive model description of each field found within the supplied input file. This file includes one row for every array index found.\
index.txt - a simplified form describing every structurally unique field found, e.g. 'a.b[0]' & 'a.b[1]' will produce a single row, 'a.b'.

### EXAMPLE COMMAND
Assuming source data is a single file called example.json that will be run in index mode:\
`tweetclip -d example.json`

## Clip Mode
The purpose of clip mode is to reduce tweets to only the fields or patterns specified within index.txt, *see below for illustrations*.

### input files: {*data*}.json, {*config*}.txt
All data must be input as a standard Twitter .json collection; 1 line / tweet.\
{*data*} can indicate an explicit file e.g. {*data*}.json - where this the case TweetClip will use only the specified file as source data.\
{*data*} can also indicate a folder e.g. {*data*}/ - in this case all files with the .json extension within the specified folder will be used as source data.\
in each case the program will close after the attempted processing of all target data.\
{*config*}.json this file contains a whitelist of fields or patterns that you wish to be present in your export, 1 symbol per line; fields can be copied directly from index.txt.

### output files: catalogue.csv, index.txt
The output filename body can be specified with -o flag, but a suffix will be added dependent on output type - to avoid accidental overwrites when using different output options. If -o is not supplied, the {data} file name will be used.\

There are three clipping functions **explicit**, **strict** and **wide**. Essentially each function varies in terms of the precision of its interpretation of whether a given input matches with any given candidate tweet field; explicit being the most precise and wide being the least.

   **Explicit** - This function simply returns fields that exactly match any requested strings in the config file. Arrays will be handled without needing to specify necessary indices.
   
>ILLUSTRATIONS
```
input "na" ==> null
input "name" ==> null
input "user.name" ==> user.name
input "quoted_status.user.name" ==> quoted_status.user.name 
```

   **Strict**   - This function will find fields whose entities exactly match the requested string. Where multiple terms are supplied (e.g. user.name) each term is evaluated separately but order is respected.\
		i.e. searching user.name will match […].user.name.[…] but would not match […].name.user.[…], […].X.user.[…] or […].name.X.[…].
>ILLUSTRATIONS
```
input "na" ==> null
input "name" ==> […].name.[…] - e.g. quoted_status.user.*name*; place.*name* etc
input "user.name" ==> […].user.name.[…] - e.g. *user*.*name*; quoted_status.*user*.*name* etc
input "quoted_status.user.name" ==> […].quoted_status.user.name.[…] - e.g. *quoted_status*.*user*.*name*; retweeted_status.*quoted_status*.*user*.*name*
```

  **Wide**     - This is the a very basic string search function that will return any field whose path contains the specified search string.
>ILLUSTRATIONS
```
input "na" ==> […]na[…] - e.g. entities.user_mentions.*na*me; entities.user_mentions.screen_*na*me; quoted_status.user.*na*me; place.full_*na*me; place.*na*me etc
input "name" ==> […]name[…] - e.g. entities.user_mentions.screen_*name*; quoted_status.user.*name*; place.full_*name*; place.*name*; in_reply_to_screen_*name* etc
input "user.name" ==> […]user.name[…]
input "quoted_status.user.name" ==> […]quoted_status.user.name[…] - e.g. *quoted_status.user.name*; retweeted_status.*quoted_status*.*user*.*name*
```
### Additional options

**jsonArrayWrapper** (-a) turns {tweet}{tweet}{tweet} into [{tweet},{tweet},{tweet}].\
**tableOutput** (-t) returns a tabular form of the whitelisted fields in UTF-8 format. Note: all speechmarks (") are replaced with u201C unicode characters in order for common table readers to manage comma separation appropriately. \
**elasticsearch** (-k) returns formatted nd-JSON with metadata ready to \_bulk import into elasticsearch.\
**prototype** (-p) returns a text list every search term found with the present config file and clip mode.\
**symbolReplacement** (-x) means that all Twitter handles (found in .screen_name, full_text and text fields) are replaced with randomised human readable terms whereever they appear including within text. Note: this process maintains a history between runs to ensure consistent application of pseudonyms. This will also produce a table (~\_codexKey.csv) containing each pair of original and replaced symbols. Now included in this mode (new from 2.5.0 onwards) is functionality to replace certain fields in product output with the code "EXCLUDED_FROM_OUTPUT". This ensures that commonly identifiable data can be explicitly excluded. The file "exclusionFields.excf" is used to define a new line delineated list of fields to suppress. This can be modified but presently contains a full list of contentious data - user names and descriptions.\
**refreshSymbols** (-r) using this flag means that the system will delete the record of used symbols and will start again. In this case a given replacement symbol is likely to be reused for a new handle but will remain consistent in subsequent runs without this flag.

### EXAMPLE COMMAND
Assuming source data is example.json and run clip mode using config.txt renaming output files as results~ (-o); using wide clip function (-w), csv output mode (-t), and replace all user symbols (-x) after refreshing the historic symbol set (-r):\
`tweetclip -d example.json -c config.txt -o results -w -t -x -r`  

=============================
