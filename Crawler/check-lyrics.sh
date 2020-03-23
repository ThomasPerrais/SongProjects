#!/bin/bash
# This script is used to evaluate the proportion of lyrics that where extracted given the number of csv already created
# This should be run to be sure that all potential lyrics were extracted (some might have been skipped due to time-out for instance)

# Defining base folder of the project
folder=$USERPROFILE/Documents/Projects/Songs

# Defining output filename
outputFilename=$folder/lyrics-report.txt

# Defining titles folder
titles=$folder/titles

# Defining lyrics folder
lyrics=$folder/lyrics

echo build Crawler solution
dotnet build -c Release

echo "creating lyrics report (number of lyrics extracted / total number in csvs - per starting letter)"
dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll check-lyrics -v 1 -p $lyrics -t $titles -o $outputFilename

read -p "All done... Press [ENTER]."