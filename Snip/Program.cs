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

    public static class Program
    {

        public static SpotifyWebAPI spotify = null;

        public static async void meem()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                    "http://localhost",
                    8000,
                    "x",
                    Scope.UserReadPrivate,
                    TimeSpan.FromSeconds(20)
               );

            spotify = await webApiFactory.GetWebApi();

       
        }

        public static  void Main(String[] args)
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
    }
}
