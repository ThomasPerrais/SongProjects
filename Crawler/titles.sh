#!/bin/bash
# This script is used to extract the csv containing authors / titles and associated urls

# Defining letter: e.g: a;b;c;0_9
letters=$1

# Defining base folder of the project
folder=$USERPROFILE/Documents/Projects/Songs

# Defining proxies lists
proxy=$folder/proxies/proxies.txt

# Defining output folder
output=$folder/titles

echo build Crawler solution
dotnet build -c Release

echo extracting titles
dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll titles -v 1 -p $proxy -l $letters -f $output -t 10000

read -p "All done... Press [ENTER]."