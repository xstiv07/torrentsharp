Movie downloader.

#Requirements/Notes
- Requires at least .NET 4.5 installed on the host system. <br />
- Requires a torrent application installed on the system, otherwise an exception is thrown when trying to download.
- Sometimes it takes a while for the magnet link to be initialized by the torrent application.
- Most of the time if English audio is present it s not the default track. You should set the default audio track in the 
audio settings of the video player you are using.

#Description
The application fetches movie info from themoviedb api, displays some information about the requested movie (description, budget, etc). You have the ability to check if the requested movie is available on the torrent tracker. If the movie is available, the application generates a list of available torrents with the appropriate description of the movie (English audio presence, file size etc.)

When the download button is pressed, the application passes a magnet link to the installed torrent application on the system (utorrent for example) which handles the download process.
