#!/bin/bash
# This script is used to check the proportion of csv that were created compared to the actual number of potential csv on paroles.netcoreapp3
# This should be run on a regular basis to evaluate the number of new songs are run the csv and lyrics extraction once new songs are added

# Defining letter: e.g: a;b;c;0_9
letters=$1

# Defining base folder of the project
folder=$USERPROFILE/Documents/Projects/Songs

# Defining proxies lists
proxy=$folder/proxies/proxy-list/proxy-list-raw.txt

# Defining titles folder
titles=$folder/titles

# Defining output filename
outputFilename=$folder/titles-report.txt

echo build Crawler solution
dotnet build -c Release

echo "creating titles report (number of csv extracted / actual number of pages - per starting letter)"
dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll check-titles -v 1 -p $proxy -i $titles -o $outputFilename -e -t 10000

read -p "All done... Press [ENTER]."