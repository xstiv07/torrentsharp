Desktop torrent application written in C# (WPF).

#Requirements
Requires at least .NET 4.5 installed on the host system. <br />
Requires a torrent application installed on the system, otherwise an exception is thrown when trying to download.

#Description
The application fetches movie info from themoviedb api, displays information about the asked movie and generates a list of torrent links to download the movie from the torrent tracker. When user hits download, the application passes control to the installed torrent application on the system which handles the download based on the passed magnet link.