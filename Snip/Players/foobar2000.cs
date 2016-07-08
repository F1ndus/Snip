#region File Information
/*
 * Copyright (C) 2012-2016 David Rudie
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02111, USA.
 */
#endregion

namespace Winter
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using SimpleJson;
    using System.Web;
    using System.Windows.Forms;
    using TagLib;

    internal sealed class foobar2000 : MediaPlayer
    {

        private string json = string.Empty;

        public override void Update()
        {
            if (!this.Found)
            {
                System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("foobar2000");

                if (processes.Length > 0)
                {
                    this.Handle = processes[0].MainWindowHandle;
                }

                foreach (var process in processes)
                {
                    process.Dispose();
                }

                processes = null;

                this.Found = true;
                this.NotRunning = false;
            }
            else
            {
                // Make sure the process is still valid.
                if (this.Handle != IntPtr.Zero && this.Handle != null)
                {
                    int windowTextLength = UnsafeNativeMethods.GetWindowText(this.Handle, this.Title, this.Title.Capacity);

                    string foobar2000Title = this.Title.ToString();

                    this.Title.Clear();

                    // If the window title length is 0 then the process handle is not valid.
                    if (windowTextLength > 0)
                    {
                        // Only update the system tray text and text file text if the title changes.
                        if (foobar2000Title != this.LastTitle)
                        {
                            if (foobar2000Title.StartsWith("foobar2000", StringComparison.OrdinalIgnoreCase))
                            {
                                TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("NoTrackPlaying"));
                            }
                            else
                            {
                                // Winamp window titles look like "[%album artist% - ]['['%album%[ CD%discnumber%][ #%tracknumber%]']' ]%title%[ '//' %track artist%]".
                                // Require that the user use ATF and replace the format with something like:
                                // %artist% – %title%
                                string windowTitleFull = System.Text.RegularExpressions.Regex.Replace(foobar2000Title, @"\s+\[foobar2000 v\d+\.\d+\.\d+\]", string.Empty);
                                string path = windowTitleFull;
                                

                                TagLib.File file = TagLib.File.Create(path);
                                Console.WriteLine(String.Format("Song: {0} {1} {2}",file.Tag.Title,file.Tag.FirstPerformer,file.Tag.Album));
                                TextHandler.UpdateText(file.Tag.Title, file.Tag.FirstPerformer, file.Tag.Album);

                                // Album artwork not supported by foobar2000
                                if (Globals.SaveAlbumArtwork)
                                {
    
                                    string folderCoverPath = null;
                                    string artworkDirectory = @Application.StartupPath + @"\";
                                    string artworkImagePath = string.Format(CultureInfo.InvariantCulture, @"{0}\{1}.jpg", artworkDirectory, "Snip_Artwork");
                                    if (file.Tag.Pictures.Length > 0)
                                    {
                                        Console.WriteLine("Cover from ID3 Tag");
                                        IPicture pic = file.Tag.Pictures[0];
                                        System.IO.File.WriteAllBytes(artworkImagePath, pic.Data.Data);                                                                       
                                    } else if((folderCoverPath = getFolderCover(path)) != null)
                                    {
                                        Console.WriteLine("Cover from Folder");
                                        System.IO.File.Copy(folderCoverPath, artworkImagePath,true);
                                    } else
                                    {
                                        this.DownloadJson(file.Tag.Title + " - " + file.Tag.FirstPerformer);

                                        dynamic jsonSummary = SimpleJson.DeserializeObject(this.json);

                                        if (jsonSummary != null)
                                        {
                                            var numberOfResults = jsonSummary.tracks.total;

                                            if (numberOfResults > 0)
                                            {
                                                jsonSummary = SimpleJson.DeserializeObject(jsonSummary.tracks["items"].ToString());

                                                if (Globals.SaveAlbumArtwork)
                                                {
                                                    Console.WriteLine("Cover from Spotify");
                                                    this.HandleSpotifyAlbumArtwork(jsonSummary[0].name.ToString());
                                                }
                                            }
                                            else
                                            {
                                                // In the event of an advertisement (or any song that returns 0 results)
                                                // then we'll just write the whole title as a single string instead.
                                                Console.WriteLine("No Cover");
                                                this.SaveBlankImage();
                                                TextHandler.UpdateText(windowTitleFull);
                                            }
                                        }
                                    }
                                }
                            }

                            this.LastTitle = foobar2000Title;
                        }
                    }
                    else
                    {
                        if (!this.NotRunning)
                        {
                            if (Globals.SaveAlbumArtwork)
                            {
                                this.SaveBlankImage();
                            }

                            TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("foobar2000IsNotRunning"));

                            this.Found = false;
                            this.NotRunning = true;
                        }
                    }
                }
                else
                {
                    if (!this.NotRunning)
                    {
                        if (Globals.SaveAlbumArtwork)
                        {
                            this.SaveBlankImage();
                        }

                        TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("foobar2000IsNotRunning"));

                        this.Found = false;
                        this.NotRunning = true;
                    }
                }
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

        public override void Unload()
        {
            base.Unload();
        }

        public override void ChangeToNextTrack()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.NextTrack));
        }

        public override void ChangeToPreviousTrack()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.PreviousTrack));
        }

        public override void IncreasePlayerVolume()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.VolumeUp));
        }

        public override void DecreasePlayerVolume()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.VolumeDown));
        }

        public override void MutePlayerAudio()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.MuteTrack));
        }

        public override void PlayOrPauseTrack()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.PlayPauseTrack));
        }

        public override void StopTrack()
        {
            UnsafeNativeMethods.SendMessage(this.Handle, (uint)Globals.WindowMessage.AppCommand, IntPtr.Zero, new IntPtr((long)Globals.MediaCommand.StopTrack));
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
                    dynamic jsonSummary = SimpleJson.DeserializeObject(json);

                    if (jsonSummary != null)
                    {
                        jsonSummary = SimpleJson.DeserializeObject(jsonSummary.tracks["items"].ToString());

                        foreach (dynamic jsonTrack in jsonSummary)
                        {
                            string modifiedTitle = TextHandler.UnifyTitles(songTitle);
                            string foundTitle = TextHandler.UnifyTitles(jsonTrack.name.ToString());

                            if (foundTitle == modifiedTitle)
                            {
                                dynamic jsonAlbum = SimpleJson.DeserializeObject(jsonTrack["album"].ToString());
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
                            dynamic jsonSummary = SimpleJson.DeserializeObject(downloadedJson);

                            string imageUrl = jsonSummary.thumbnail_url.ToString().Replace("cover", string.Format(CultureInfo.InvariantCulture, "{0}", (int)Globals.ArtworkResolution));

                            if (Globals.KeepSpotifyAlbumArtwork)
                            {
                                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadSpotifyFileCompleted);
                                webClient.DownloadFileAsync(new Uri(imageUrl), artworkImagePath, artworkImagePath);
                            }
                            else
                            {
                                webClient.DownloadFileAsync(new Uri(imageUrl), this.DefaultArtworkFilePath);
                            }

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
                try
                {
                    System.IO.File.Copy((string)e.UserState, this.DefaultArtworkFilePath, true);
                }
                catch (IOException)
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
    }
}
