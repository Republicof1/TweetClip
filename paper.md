---
title: "TweetClip: A command line tool for streamlining Twitter JSON data"
tags:
  - Twitter
  - JSON
  - ethics
authors:
  - name: Benjamin Green^[Custom footnotes for e.g. denoting who the corresponding author is can be included like this.]
    orcid: 0000-0002-6608-9648
    affiliation: 1
  - name: Lamiece Hassan
    orcid: 0000-0002-5888-422X
    affiliation: 1
affiliations:
 - name: Division of Informatics, Imaging and Data Sciences, School of Health Sciences, University of Manchester
   index: 1
date: 08 June 2021

# Summary
Twitter is one of the most popular social media platforms globally, averaging 330 million active users every month (Statista, 2021). Part of Twitter's broad appeal is that users can follow anyone with a public profile, including politicians, celebrities and organisations. 

Increasingly, Twitter is being used by academics for research purposes to reveal insights into attitudes, behaviours and social networks. Twitter offers approved academics access to 'tweets' and associated metadata derived from public profiles for research purposes via an Application Programming Interface (API). However, for a number of reasons extracting and manipulating this data into suitable formats for analysis can be challenging, especially for those without programming skills. 

TweetClip addresses this issue by simplifying the process of extracting a user-defined range of Twitter data fields necessary to complete a given research project. It accepts tweets returned from the Twitter API as JSON objects and prunes datasets down to the data fields of interest in reusable formats, without the need for advanced programming knowledge. 


# Statement of Need
Several useful tools exist to simplify the process of collecting Twitter data, including packages for R and Python (e.g.tweepy, rtweet). Typically these tools allow users to collect new datasets of tweets using user-defined search queries and/or to 'rehydrate' pre-existing datasets using lists of unique numeric tweet identifiers to retrieve the complete tweets. 

Whilst such tools lower the bar to entry for scholars wishing to conduct research using Twitter data, several hurdles arguably remain for programming novices before they can proceed to analysis. First, researchers are often (though not always) required to have a working knowledge of particular software packages and programming languages. Second, tweet retrieval strategies typically require researchers to choose between retrieving more complete data in rich but complex 'nested' structures (e.g. .JSON) or are restricted to a smaller number of developer defined data fields in more familiar file formats (e.g. .csv). Third, data manipulation is also complicated by the fact that - in longitudinal studies especially - Twitter data is often spread across multiple files, due to size and/or data collection methods. Collectively, these issues conspire to impede the ability to access and process data efficiently, keeping only the required fields and reducing processing of identifiable data.

TweetClip is a command line tool that aims to fill this gap, complementing popular tools used for collecting and analysing Twitter data (e.g. hydrator). It accepts data obtained from the Twitter API as JSON objects and prunes datasets down to the most relevant fields of interest while maintaining relevant data structures. The expected benefit is the ability to easily create smaller, more relevant datasets to streamline research ??without a requirement to have any experience of programming or scripting??.

TweetClip's indexing feature allows users to create a comprehensive catalogue of all available data fields, including nested JSON objects. Clipping mode can then be used to select fields of interest, using either explicitly named fields to retrieve exact matches, or in an exploratory manner to obtain all fields containing particular strings. The symbol replacement option replaces identifiable Twitter usernames (e.g. @JOSS_user) with arbitrary pseudonyms (@bluewhale), whilst maintaining a history between runs to ensure consistent application and suppressing the most common fields including person/ally/ identifiable data (including numeric user identifiers).

TweetClip can handle very large data files (200,000+ tweets) and multiple data files simultaneously. Output files of tweets can be exported as a single file in JSON or .csv format, compatible with most standard programming and statistical software packages. It was built to support work on Twitter data, particularly by novices with limited programming knowledge, but it will work with any JSON data.


# Acknowledgements

This work was supported by a grant to LH from the Medical Research Council (Ref: MR/S004025/1). We also acknowledge contributions from xxx during the development of this project.


# References 