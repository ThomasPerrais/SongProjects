# SongProjects

This projects contains various codes and projects arround music generation and music quiz.

## Table of Contents

* [Requirements] (#requirements)
	* [Crawler] (#requirements-crawler)
* [Crawler](#crawler)
* [SongNLG] (#nlg)
* [SongTrad] (#song-trad)
* [Licence] (#licence)

## Requirements

### Crawler

Crawler is a .Net Core 3.1 project using the following libraries available through [nuget](http://nuget.org) :

* CsvHelper (15.0.0)
* NDesk.Options (0.2.1)

## Crawler

The Crawler solution is a C# project to extract song lyrics.
Crawler is a .Net Core 3.1 project and should run seemlessly on Windows, Linux and MacOS. 
Currently the project contains a functionnal crawler for the website [paroles.net](https://paroles.net)
More information concerning this project can be found in Crawler/README.md

## SongNLG

SongNLG is meant to contain models of Neural Language Generation applied to song lyrics.

## SongTrad

SongTrad is meant to contain models of Neural Machine Translation applied to song lyrics

## Licence

The SongProjects is Apache2-licenced.