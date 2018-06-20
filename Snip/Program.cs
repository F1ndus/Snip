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
    using System.Threading;
    using System.Windows.Forms;
    using SpotifyAPI.Web; //Base Namespace
    using SpotifyAPI.Web.Auth; //All Authentication-related classes
    using SpotifyAPI.Web.Enums; //Enums
    using SpotifyAPI.Web.Models; //Models for the JSON-response
    using SpotifyAPI.Web.Auth;
    using System.Net;

    public static class Program
    {

        static AutorizationCodeAuth auth;

        public static SpotifyWebAPI spotify = null;

        public static async void meem()
        {
   
            auth = new AutorizationCodeAuth()
            {
                //Your client Id
                ClientId = "b6ddbd76e2444d3081e2582f1b8b8255",
                //Set this to localhost if you want to use the built-in HTTP Server
                RedirectUri = "http://localhost:8000/callback",
                //How many permissions we need?
                Scope = Scope.UserReadPlaybackState,
            };
            //This will be called, if the user cancled/accept the auth-request

            //auth.OnResponseReceivedEvent += auth_OnResponseReceivedEvent;
            
            //a local HTTP Server will be started (Needed for the response)
            //auth.StartHttpServer(8000);
            //This will open the spotify auth-page. The user can decline/accept the request
            auth.DoAuth();
            Thread.Sleep(5000);
            //auth.StopHttpServer();
            Console.WriteLine("Too long, didnt respond, exiting now...");

            using (WebClient client = new WebClient())
            {
                string htmlCode = client.DownloadString("http://localhost:8000/get_token");
                spotify = new SpotifyWebAPI()
                {
                    TokenType = "Bearer",
                    AccessToken = htmlCode
                };

                new Thread(() =>
                {
                    var lastUpdate = new DateTime(1900, 1, 1);
                    while(true)
                    {
                        try
                        {
                            var currentTime = DateTime.Now;
                            if ((currentTime.ToUnixTimeMillisecondsPoly() -
                            lastUpdate.ToUnixTimeMillisecondsPoly()) > 1800000)
                            {
                                Thread.CurrentThread.IsBackground = true;
                                Console.WriteLine("Started Token refresh Thread");
                                client.DownloadData("http://localhost:8000/token");
                                spotify.AccessToken = client.DownloadString("http://localhost:8000/get_token");
                                lastUpdate = currentTime;
                            }
                        } catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                                            
                        Thread.Sleep(1000);
                    }
                   
                }).Start();
            }
        }


        public static  void Main(String[] args)
        {
            try
            {
                meem();

            
               
        //...

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isNewProcess = false;

            Mutex mutex = null;

            try
            {
                mutex = new Mutex(true, Application.ProductName, out isNewProcess);

                if (isNewProcess)
                {
                    Application.Run(new Snip());
                    mutex.ReleaseMutex();
                }
                // else
                // {
                //     MessageBox.Show("Another instance of " + Application.ProductName + " is already running.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                // }
            } catch (Exception e)
            {
                Console.WriteLine("Exception occured: ", e.Message);
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.Close();
                }
            }

            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e.Message);
            }


        }
    }
}
