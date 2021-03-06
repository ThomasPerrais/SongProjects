#!/bin/bash
# This script is used to extract the lyrics of the songs

# Defining letter: e.g: a;b;c;0_9
letters=$1

# Defining timeout
timeout=10000

# Defining base folder of the project
folder=$USERPROFILE/Documents/Projects/Songs

# Defining proxies lists
proxy=$folder/proxies/proxy-list/proxy-list-raw.txt

# Defining input folder containing csvs
input=$folder/titles

# Defining output folder containing lyrics
output=$folder/lyrics

echo build Crawler solution
dotnet build -c Release

echo extracting lyrics
if [[ $letters = "" ]]
then
    dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll lyrics -v 1 -p $proxy -o $output -i $input -t $timeout
else
    dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll lyrics -v 1 -p $proxy -l $letters -o $output -i $input -t $timeout
fi



read -p "All done... Press [ENTER]."