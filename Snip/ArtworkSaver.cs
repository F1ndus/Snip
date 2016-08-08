using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TagLib;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using System.Windows.Forms;
using TagLib;
using System.Drawing;
using Winter;
using SimpleJson;
using System.Threading;

namespace Winter
{
    class ArtworkSaver
    {

        private readonly string defaultArtworkFile = @Application.StartupPath + @"\Snip_Artwork.jpg";
        private string json = string.Empty;


        public void getCover(string path)
        {
            TagLib.File file = TagLib.File.Create(path);
            string folderCoverPath = null;
            string artworkDirectory = @Application.StartupPath + @"\";
            string artworkImagePath = string.Format(CultureInfo.InvariantCulture, @"{0}\{1}.jpg", artworkDirectory, "Snip_Artwork");
            if (file.Tag.Pictures.Length > 0)
            {
                Console.WriteLine("Cover from ID3 Tag");
                IPicture pic = file.Tag.Pictures[0];
                System.IO.File.WriteAllBytes(artworkImagePath, pic.Data.Data);
                onFinish();
            }
            else if ((folderCoverPath = getFolderCover(path)) != null)
            {
                Console.WriteLine("Cover from Folder");
                System.IO.File.Copy(folderCoverPath, artworkImagePath, true);
                onFinish();
            }
            else
            {
                this.getCover(file.Tag.Title, file.Tag.FirstPerformer);
            }
        }

        public string[] getCover(string title,string interpret)
        {
            this.DownloadJson(title + " - " + interpret);

            dynamic jsonSummary = SimpleJson.SimpleJson.DeserializeObject(this.json);


            if (jsonSummary != null)
            {
                var numberOfResults = jsonSummary.tracks.total;

                if (numberOfResults > 0)
                {
                    jsonSummary = SimpleJson.SimpleJson.DeserializeObject(jsonSummary.tracks["items"].ToString());
                    string albumtitle = jsonSummary[0].album.name;                 
                    if (Globals.SaveAlbumArtwork)
                    {
                        Console.WriteLine("Cover from Spotify");
                        this.HandleSpotifyAlbumArtwork(jsonSummary[0].name.ToString());
                    }
                    return new string[] { albumtitle };
                }           
                else
                {
                    // In the event of an advertisement (or any song that returns 0 results)
                    // then we'll just write the whole title as a single string instead.
                    Console.WriteLine("No Cover");
                    this.SaveBlankImage();
                    TextHandler.UpdateText("xxxxxxxxxxxxx");
                }        
            }
            return null;
        }


        private void onFinish()
        {
            string artworkDirectory1 = @Application.StartupPath + @"\";
            string artworkImagePath1 = string.Format(CultureInfo.InvariantCulture, @"{0}\{1}.jpg", artworkDirectory1, "Snip_Artwork");
            Bitmap color = ArtworkColor.getColor(artworkImagePath1);
            color.Save(artworkDirectory1 + "color.png");
        }

        private void SaveBlankImage()
        {
            try
            {
                System.IO.File.WriteAllBytes(this.defaultArtworkFile, this.blankImage);
                this.SavedBlankImage = true;
            }
            catch (IOException)
            {
                // File is in use... or something.  We can't write so we'll just bail out and hope no one notices.
            }
        }

        private string getFolderCover(string path)
        {
            string[] filepatharray = path.Split('\\');
            path = path.Replace(filepatharray[filepatharray.Length - 1], "");
            string retval = "";
            string[] files = Directory.GetFiles(path, "*.jpg", SearchOption.TopDirectoryOnly);
            string[] file2 = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);

            int array1OriginalLength = files.Length;
            Array.Resize<string>(ref files, array1OriginalLength + file2.Length);
            Array.Copy(file2, 0, files, array1OriginalLength, file2.Length);
            return Array.Find<string>(files, filepath => Path.GetFileName(filepath).ToLower().Contains("folder") || Path.GetFileName(filepath).ToLower().Contains("front"));
        }

        private void DownloadJson(string spotifyTitle)
        {
            using (WebClient jsonWebClient = new WebClient())
            {
                try
                {
                    jsonWebClient.Encoding = System.Text.Encoding.UTF8;

                    var downloadedJson = jsonWebClient.DownloadString(string.Format(
                            CultureInfo.InvariantCulture,
                            "https://api.spotify.com/v1/search?q={0}&type=track",
                            HttpUtility.UrlEncode(spotifyTitle)));

                    Console.WriteLine("https://api.spotify.com/v1/search?q={0}&type=track", (spotifyTitle.Replace('/', ' ')));
                    Console.WriteLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "https://api.spotify.com/v1/search?q={0}&type=track",
                            HttpUtility.UrlEncode(spotifyTitle)));

                    if (!string.IsNullOrEmpty(downloadedJson))
                    {
                        this.json = downloadedJson;
                    }
                }
                catch (WebException)
                {
                    this.json = string.Empty;
                    this.SaveBlankImage();
                }
            }
        }

        // TODO: Re-write this to download the artwork link supplied in the primary JSON file instead of using the old embedded web link.
        private void HandleSpotifyAlbumArtwork(string songTitle)
        {
            string albumId = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(this.json))
                {
                    dynamic jsonSummary = SimpleJson.SimpleJson.DeserializeObject(json);

                    if (jsonSummary != null)
                    {
                        jsonSummary = SimpleJson.SimpleJson.DeserializeObject(jsonSummary.tracks["items"].ToString());

                        foreach (dynamic jsonTrack in jsonSummary)
                        {
                            string modifiedTitle = TextHandler.UnifyTitles(songTitle);
                            string foundTitle = TextHandler.UnifyTitles(jsonTrack.name.ToString());

                            if (foundTitle == modifiedTitle)
                            {
                                dynamic jsonAlbum = SimpleJson.SimpleJson.DeserializeObject(jsonTrack["album"].ToString());
                                albumId = jsonAlbum.uri.ToString();

                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(albumId))
                        {
                            albumId = albumId.Substring(albumId.LastIndexOf(':') + 1);
                            this.DownloadSpotifyAlbumArtwork(albumId);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                this.SaveBlankImage();
            }
        }

        private void DownloadSpotifyAlbumArtwork(string albumId)
        {
            string artworkDirectory = @Application.StartupPath + @"\SpotifyArtwork";
            string artworkImagePath = string.Format(CultureInfo.InvariantCulture, @"{0}\{1}.jpg", artworkDirectory, albumId);

            if (!Directory.Exists(artworkDirectory))
            {
                Directory.CreateDirectory(artworkDirectory);
            }

            FileInfo fileInfo = new FileInfo(artworkImagePath);

            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                fileInfo.CopyTo(this.DefaultArtworkFilePath, true);
            }
            else
            {
                this.SaveBlankImage();

                using (WebClientWithShortTimeout webClient = new WebClientWithShortTimeout())
                {
                    try
                    {
                        webClient.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
                        var downloadedJson = webClient.DownloadString(string.Format(CultureInfo.InvariantCulture, "https://embed.spotify.com/oembed/?url=spotify:album:{0}", albumId));

                        if (!string.IsNullOrEmpty(downloadedJson))
                        {
                            dynamic jsonSummary = SimpleJson.SimpleJson.DeserializeObject(downloadedJson);

                            string imageUrl = jsonSummary.thumbnail_url.ToString().Replace("cover", string.Format(CultureInfo.InvariantCulture, "{0}", (int)Globals.ArtworkResolution));


                            //bad but needs call onfinish will break history logic but idc atm
                            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadSpotifyFileCompleted);
                            webClient.DownloadFileAsync(new Uri(imageUrl), this.DefaultArtworkFilePath);


                            this.SavedBlankImage = false;
                        }
                    }

                    catch (WebException)
                    {
                        this.SaveBlankImage();
                    }
                }
            }
        }

        private void DownloadSpotifyFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if (Globals.KeepSpotifyAlbumArtwork)
                {
                    try
                    {
                        System.IO.File.Copy((string)e.UserState, this.DefaultArtworkFilePath, true);
                    }
                    catch (IOException)
                    {
                        this.SaveBlankImage();
                    }
                
                }
                Thread.Sleep(100);
                onFinish();
            }
            else
            {
                Console.WriteLine(e.Error.Message);
                this.SaveBlankImage();
            }
        }

        private class WebClientWithShortTimeout : WebClient
        {
            // How many seconds before webclient times out and moves on.
            private const int WebClientTimeoutSeconds = 10;

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest webRequest = base.GetWebRequest(address);
                webRequest.Timeout = WebClientTimeoutSeconds * 60 * 1000;
                return webRequest;
            }
        }

        private readonly byte[] blankImage = new byte[]
      {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
      };

        private bool SavedBlankImage { get; set; }

        public string DefaultArtworkFilePath
        {
            get
            {
                return this.defaultArtworkFile;
            }
        }
    }
}
