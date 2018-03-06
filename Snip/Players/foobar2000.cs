﻿#region File Information
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
    using System.Drawing;
    using Winter;
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
                                try
                                {
                                    // Winamp window titles look like "[%album artist% - ]['['%album%[ CD%discnumber%][ #%tracknumber%]']' ]%title%[ '//' %track artist%]".
                                    // Require that the user use ATF and replace the format with something like:
                                    // %artist% – %title%
                                    string windowTitleFull = System.Text.RegularExpressions.Regex.Replace(foobar2000Title, @"\s+\[foobar2000 v\d+\.\d+\.\d+\]", string.Empty);
                                    string path = windowTitleFull;

                                    try
                                    {
                                        TagLib.File file = TagLib.File.Create(path);
                                        Console.WriteLine(String.Format("Song: {0} {1} {2}", file.Tag.Title, file.Tag.FirstPerformer, file.Tag.Album));
                                        // TextHandler.UpdateText(file.Tag.Title, file.Tag.FirstPerformer, file.Tag.Album);

                                        // Album artwork not supported by foobar2000
                                        if (Globals.SaveAlbumArtwork)
                                        {
                                            ArtworkSaver saver = new ArtworkSaver();
                                            saver.getCover(path);
                                        }
                                    }
                                    catch (UnsupportedFormatException e)
                                    {
                                        Console.WriteLine(e.Message);
                                        ArtworkSaver saver = new ArtworkSaver();
                                        saver.SaveBlankImage();
                                    }

                                }
                                catch ( Exception e)
                                {

                                    Console.WriteLine(e);
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

    }
}
