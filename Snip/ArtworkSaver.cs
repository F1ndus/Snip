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
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Winter
{
    class ArtworkSaver
    {

        private readonly string defaultArtworkFile = @Application.StartupPath + @"\Snip_Artwork.jpg";
        private string json = string.Empty;

        public void getCover(PlaybackContext con)
        {
            DownloadSpotifyAlbumArtwork(con.Item.Album.Images.First());
        }


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
             
            }
            else if ((folderCoverPath = getFolderCover(path)) != null)
            {
                Console.WriteLine("Cover from Folder");
                System.IO.File.Copy(folderCoverPath, artworkImagePath, true);
              
            }
            else
            {
                if (file.Tag.Album == null)
                    Console.WriteLine("No Album string found");
                DownloadSpotifyAlbumArtwork("artist:" + file.Tag.FirstArtist + " album:" + file.Tag.Album);
  
            }

            var d = ArtworkColor.getColor(artworkImagePath);
            d.Save(artworkDirectory + "color.png");
        }


        public void SaveBlankImage()
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
            string pref = Array.Find<string>(files, filepath => Path.GetFileName(filepath).ToLower().Contains("folder") || Path.GetFileName(filepath).ToLower().Contains("front"));
            if (pref != null)
                return pref;
            else if (files.Length > 0)
                return files.First();
            else
                return null;
        }

        private void DownloadSpotifyAlbumArtwork(SpotifyAPI.Web.Models.Image image)
        {
            using (WebClientWithShortTimeout webClient = new WebClientWithShortTimeout())
                try
            {
                webClient.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
                webClient.DownloadFile(image.Url.ToString(), this.defaultArtworkFile);
                Thread.Sleep(10);
            }

            catch (Exception)
            {
                this.SaveBlankImage();
            }
           
        }


        private void DownloadSpotifyAlbumArtwork(string searchString)
        {

            Console.WriteLine("Cover from spotify");
                using (WebClientWithShortTimeout webClient = new WebClientWithShortTimeout())
                {
                    try
                    {
                        Paging<SimpleAlbum> album = Program.spotify.SearchItems(searchString, SearchType.All).Albums;
                        var meem = album.Items[0].Images[0];

                    DownloadSpotifyAlbumArtwork(meem);
                    }

                    catch (Exception)
                    {
                        this.SaveBlankImage();
                    }
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
