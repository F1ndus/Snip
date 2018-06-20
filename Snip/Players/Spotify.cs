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
    using System.Web;
    using System.Windows.Forms;
    using SimpleJson;
    using System.Drawing;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Threading;

    internal sealed class Spotify : MediaPlayer
    {
        private string json = string.Empty;
        Boolean active = false;
        Process proc;

        public override void Update()
        {

            Process[] processlist = Process.GetProcesses();
            foreach (Process p in processlist)
            {
                if (p.ProcessName.Equals("Spotify") && p.MainWindowTitle.Length > 0)
                {
                    this.NotRunning = true;
                    this.Found = true;
                    this.proc = p;
                    break;
                }
                this.proc = null;
                this.Found = true;
                this.NotRunning = false;
            }


            // Make sure the process is still valid.
            if (proc != null)
                {
     
                    string spotifyTitle = this.proc.MainWindowTitle;
                    int windowTextLength = spotifyTitle.Length;

                    this.Title.Clear();

                    // If the window title length is 0 then the process handle is not valid.
                    if (windowTextLength > 0)
                    {
                        // Only update if the title has actually changed.
                        // This prevents unnecessary calls and downloads.
                        if (spotifyTitle != this.LastTitle)
                        {
                        if (spotifyTitle == "Spotify")
                        {
                            if (Globals.SaveAlbumArtwork)
                            {
                                //this.SaveBlankImage();
                            }
                            //this.LastTitle = spotifyTitle;
                            //TextHandler.UpdateText(Globals.ResourceManager.GetString("NoTrackPlaying"));
                        }
                        else
                        {
                            try
                            {
                                Thread.Sleep(1000);
                                ArtworkSaver saver = new ArtworkSaver();
                                var pcon = Program.spotify.GetPlayingTrack();
                                if (pcon != null && pcon.Item != null)
                                {
                                    saver.getCover(pcon);
                                    this.LastTitle = spotifyTitle;
                                    string title = pcon.Item.Name;
                                    string interpret = pcon.Item.Artists[0].Name;
                                    string album = pcon.Item.Album.Name;
                                    TextHandler.UpdateText(title + "\n" + interpret + "\n" + album);
                                }
                                else
                                {

                                    Console.WriteLine("XD NULL XDXDXDXD");
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("meem");
                            }
                        }
                        }
                    }
                    else
                    {
                        if (!this.NotRunning)
                        {
                            this.ResetSinceSpotifyIsNotRunning();
                        }
                    }
                }
                else
                {
                    if (!this.NotRunning)
                    {
                        this.ResetSinceSpotifyIsNotRunning();
                    }
                }
            
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

        private void ResetSinceSpotifyIsNotRunning()
        {
            if (!this.SavedBlankImage)
            {
                if (Globals.SaveAlbumArtwork)
                {
                    this.SaveBlankImage();
                }
            }

            TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("SpotifyIsNotRunning"));

            this.Found = false;
            this.NotRunning = true;
        }
    }
}
