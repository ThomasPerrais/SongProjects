#!/bin/bash
# This script is used to create the csv datatset containing titles /authors and lyrics

# Defining letter: e.g: a;b;c;0_9
letters=$1

# Defining base folder of the project
folder=$USERPROFILE/Documents/Projects/Songs

# Defining titles folder
titles=$folder/titles

# Defining lyrics folder
lyrics=$folder/lyrics

# Defining output folder
output=$folder/dataset

echo build Crawler solution
dotnet build -c Release

echo creating dataset
dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll dataset -v 1 -l $letters -o $output -p $lyrics -t $titles

read -p "All done... Press [ENTER]."