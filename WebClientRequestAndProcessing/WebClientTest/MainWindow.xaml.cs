using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;

namespace MovieTorrentSharp
{
    public partial class MainWindow : Window
    {
        const string key = "&api_key=34a432c98876257a07c14281733cf631";
        const string imageURL = "https://image.tmdb.org/t/p/w300";
        const string movieMode = "https://api.themoviedb.org/3/movie/";
        const string queryMode = "http://api.themoviedb.org/3/search/movie?query=";
        const string fetchedUrl = "http://www.rutor.info/search/0/0/000/0/";
        const string baseTrackerUrl = "http://www.rutor.info";

        const string movieAudioPattern = "Аудио([^\n\r]*)|Audio([^\n\r]*)";
        const string magnetLinksPattern = "(?s)(magnet).*?(?=&dn=rutor)";
        const string contentLinksPattern = "(?s)(/torrent/\\d{4,6}/).*?(?=\">)";
        const string displayNamePattern = "(?s)(?<=/torrent/\\d{4,6}/).*?(?=\">)";
        const string movieSizePattern = "(?s)(<td align=\"right\">\\d+(?:[\\.\\,]\\d+)).*?(?=</td>)";

        string searchItem; // shared resource
        int movieId;

        public MainWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            SetUpUserInterfaceControlsOnSearchClick();

            if (SearchQuery.Text != "")
            {
                try
                {
                    searchbtn.IsEnabled = false;
                    SearchSpinner.Visibility = Visibility.Visible;
                    SearchSpinner.Spin = true;
                    Instructionslbl.Visibility = Visibility.Collapsed;

                    string baseURL = queryMode + searchItem + key;

                    string searchResult = await GetStringFromWeb(baseURL); //downloading string from web
                    Task<MovieSearchResults.Rootobject> t1 = null;
                    
                    t1 = Task.Run(() => Deserialize<MovieSearchResults.Rootobject>(searchResult)); //deserializing
                    FaultedTask(t1);
                    
                    t1.Wait();
                    if (t1.Result.total_results != 0)
                    {
                        searchbtn.IsEnabled = true;
                        SearchSpinner.Visibility = Visibility.Collapsed;
                        SearchSpinner.Spin = false;

                        for (int i = 0; i < t1.Result.results.Length; i++) // Interacting with UI
                        {
                            DisplayData.Items.Add(new MovieSearchResults.Result
                            {
                                original_title = t1.Result.results[i].original_title,
                                release_date = t1.Result.results[i].release_date,
                                id = t1.Result.results[i].id,
                            });
                        }
                    }
                    else
                    {
                        MovieInfo.Text = "Movie is Not Found"; // interacting with UI
                        SearchSpinner.Visibility = Visibility.Collapsed;
                        SearchSpinner.Spin = false;
                        searchbtn.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    searchbtn.IsEnabled = true;
                    SearchSpinner.Visibility = Visibility.Collapsed;
                    SearchSpinner.Spin = false;
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please Enter a query");
            }
        }


        private async void DispayData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DisplayData.SelectedItem != null)
                {
                    SetUpUserInterfaceControlsOnSelectionChange();
                    var passedMovie = DisplayData.SelectedItem as MovieSearchResults.Result;
                    movieId = passedMovie.id;

                    string baseURL = movieMode + movieId + "?" + key;

                    string searchResult = await GetStringFromWeb(baseURL); //downloading string from web
                    var result = Deserialize<Movie_Tmdb.Rootobject>(searchResult); //deserializing

                    SetUpMovieInfoUserInterface(imageURL, result);
                    CheckIfAvailable.IsEnabled = true;

                }
                MovieInfoSpinner.Visibility = Visibility.Collapsed;
                MovieInfoSpinner.Spin = false;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void CheckIfAvailable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RutorMovieInfo.Items.Clear(); // clearing up list of available movies
                CheckIfAvailable.IsEnabled = false; //disabling the button
                AvailabilitySpinner.Visibility = Visibility.Visible;
                AvailabilitySpinner.Spin = true;

                if (DisplayData.SelectedItem != null)
                {
                    var passedMovie = DisplayData.SelectedItem as MovieSearchResults.Result;
                    string baseUrl = fetchedUrl + passedMovie.original_title;

                    var result = await GetStringFromWeb(baseUrl); //request for url contents

                    var magnetLink_matches = GenerateMatches(magnetLinksPattern, result);
                    var displayName_matches = GenerateMatches(displayNamePattern, result);
                    var contentLink_matches = GenerateMatches(contentLinksPattern, result);
                    var movieSize_match = GenerateMatches(movieSizePattern, result);

                    if (displayName_matches.Count != 0)
                    {
                        for (int i = 0; i < magnetLink_matches.Count; i++)
                        {
                            RutorMovieInfo.Items.Add(new Movie
                            {
                                DisplayName = displayName_matches[i],
                                MagnetLink = magnetLink_matches[i],
                                ContentLink = contentLink_matches[i],
                                Size = movieSize_match[i].Replace("<td align=\"right\">", "").Replace("&nbsp;", " ")
                            });
                        }
                    }
                    else
                    {
                        RutorMovieInfo.Items.Add("No movies were Found");
                        downloadbtn.IsEnabled = false;
                    }
                    AvailabilitySpinner.Visibility = Visibility.Collapsed;
                    AvailabilitySpinner.Spin = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void RutorMovieInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                TechInfoSpinner.Visibility = Visibility.Visible;
                TechInfoSpinner.Spin = true;
                downloadbtn.IsEnabled = true; //enabling download button

                var passedMovie = RutorMovieInfo.SelectedItem as Movie; // selected
                string baseUrl = baseTrackerUrl + passedMovie.ContentLink; //html of insides

                var movieContents = await GetStringFromWeb(baseUrl); //request for url contents

                var movieAudio_match = Regex.Matches(movieContents, movieAudioPattern, RegexOptions.Singleline)
                    .Cast<Match>().Select(m => m.Value).ToList();

                ProcessAudioMatches(passedMovie, movieAudio_match);

                DisplayAudioMatches(passedMovie);

                TechInfoSpinner.Visibility = Visibility.Collapsed;
                TechInfoSpinner.Spin = false;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static Task<string> GetStringFromWeb(string baseUrl)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            return wc.DownloadStringTaskAsync(baseUrl);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                var movie = RutorMovieInfo.SelectedItem as Movie;

                if (movie != null)
                {
                    Task t9 = Task.Run(() => {
                        Process.Start(movie.MagnetLink);
                    });

                    FaultedTask(t9);
                    //Action action = () => Process.Start(movie.MagnetLink);
                    //Dispatcher.BeginInvoke(action);
                }
                else
                {
                    MessageBox.Show("Please Select a Movie from the List");
                }
            }
            catch (Exception ex)
            {
                string s = "Torrent client is not found.";
                MessageBox.Show(s + "\nError:" + ex.Message);
            }
        }

        private static void FaultedTask(Task t)
        {
            Task faulted = t.ContinueWith(antecedent =>
            {
                string exception = "";
                foreach (var ex in antecedent.Exception.Flatten().InnerExceptions)
                {
                    exception += ex.Message + "\n";
                }
                MessageBox.Show(exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void mnuFileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void mnuHowTo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Thanks for using MovieTorrent# !\nTo function properly, MovieTorrent# requires internet connection and installed torrent client on your system.");
        }

        private static T Deserialize<T>(string searchResult)
        {
            var result = new JavaScriptSerializer().Deserialize<T>(searchResult); //deserializing
            return result;
        }

        private void SetUpUserInterfaceControlsOnSearchClick()
        {
            DisplayData.Items.Clear();
            searchItem = SearchQuery.Text;
            RutorMovieInfo.Items.Clear();
            movieImage.Source = null;
            AvailabilitySpinner.Visibility = Visibility.Collapsed;
            AvailabilitySpinner.Spin = false;
            CheckIfAvailable.IsEnabled = false; //disabling the button
        }

        private static void ProcessAudioMatches(Movie passedMovie, List<string> match)
        {
            if (match.ConvertAll(x => x.ToLower()).Any(x => x.Contains("english") || x.Contains("en") || x.Contains("eng") || x.Contains("английский") || x.Contains("англ")))
            {
                passedMovie.EnglishAudio = true;
            }
            else
            {
                passedMovie.EnglishAudio = false;
            }
        }
        private void SetUpUserInterfaceControlsOnSelectionChange()
        {
            MovieInfo.Inlines.Clear(); //clearing up info about the movie
            MovieInfoSpinner.Visibility = Visibility.Visible;
            MovieInfoSpinner.Spin = true;
            TechInfoSpinner.Visibility = Visibility.Collapsed;
            TechInfoSpinner.Spin = false;
            audiolbl.Content = null;
            SizeInfolbl.Content = null;
            movieImage.Source = null; //clearing up image
            downloadbtn.IsEnabled = false;
        }
        private static List<string> GenerateMatches(string Pattern, string antecedent)
        {
            var matches = Regex.Matches(antecedent, Pattern, RegexOptions.Singleline)
             .Cast<Match>().Select(m => m.Value).ToList();
            return matches;
        }

        private void SetUpMovieInfoUserInterface(string imageURL, Movie_Tmdb.Rootobject antecedent)
        {
            MovieInfo.Inlines.Add((new Bold(new Run("Original Title: "))));
            MovieInfo.Inlines.Add(antecedent.original_title);
            MovieInfo.Inlines.Add(new LineBreak());
            MovieInfo.Inlines.Add((new Bold(new Run("Release Date: "))));
            MovieInfo.Inlines.Add(antecedent.release_date);
            MovieInfo.Inlines.Add(new LineBreak());
            MovieInfo.Inlines.Add((new Bold(new Run("Run Time: "))));
            MovieInfo.Inlines.Add(antecedent.runtime + " min");
            MovieInfo.Inlines.Add(new LineBreak());
            MovieInfo.Inlines.Add((new Bold(new Run("Budget: "))));
            MovieInfo.Inlines.Add(antecedent.budget + "$");
            MovieInfo.Inlines.Add(new LineBreak());
            MovieInfo.Inlines.Add((new Bold(new Run("Description: "))));
            MovieInfo.Inlines.Add(new LineBreak());
            MovieInfo.Inlines.Add(new Run(antecedent.overview));

            BitmapImage image = new BitmapImage(); // parsing image from uri
            image.BeginInit();
            image.UriSource = new Uri(imageURL + antecedent.poster_path);
            image.EndInit();
            movieImage.Source = image;

            MovieInfoSpinner.Visibility = Visibility.Collapsed;
            MovieInfoSpinner.Spin = false;
        }

        private void DisplayAudioMatches(Movie passedMovie)
        {
            if (passedMovie.EnglishAudio)
            {
                audiolbl.Content = "English Audio: Yes";
            }
            else
            {
                audiolbl.Content = "English Audio: No";
            }
            SizeInfolbl.Content = ("Size: " + passedMovie.Size);
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}