#!/bin/bash
# This script is used to create a list of available proxies extracted from given
# - list of files
# - list of URLs

# Defining default folder
folder=$USERPROFILE/Documents/Projects/Songs

# Defining files containing raw proxies lists
files=$folder/proxies/proxy-list/proxy-list-raw.txt

# Defining output file name
output=$folder/proxies/proxies.txt

# Defining default urls containing potential proxies
urls="http://free-proxy.cz/fr/proxylist/country/FR/all/ping/all;http://proxy-list.org/french/index.php;http://spys.one/free-proxy-list/FR/;https://www.proxynova.com/proxy-server-list/country-fr/;https://www.proxydocker.com/fr/proxylist/country/France;https://us-proxy.org/"


echo build Crawler solution
dotnet build -c Release

echo finding available proxies
dotnet Crawler/bin/Release/netcoreapp3.1/Crawler.dll proxy -v 1 -f $files -o $output -u $urls -r 3

read -p "All done... Press [ENTER]."