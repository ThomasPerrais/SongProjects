# Crawler

Crawler is C# solution targeting .Net Core 3.1 and currently containing a crawler for [paroles.net](http://paroles.net)
The code contains 6 shell script that will allow to create a dataset of lyrics along with titles and authors.

* proxy-list.sh: given urls and potential proxies, test the proxies against a test URL to check their sanity
* titles.sh: extract csv with titles / authors and associated urls
* lyrics.sh: using csv created using titles.sh, extract the lyrics associated with the songs
* dataset.sh: using outputs of titles.sh and lyrics.sh, create a dataset containing titles, authors and lyrics
* check-titles.sh: sanity check to evaluate the number of csv extracted and the total expected - per first letter
* check-lyrics.sh: sanity check to evaluate the number of lyrics extracted and the total expected - per first letter
