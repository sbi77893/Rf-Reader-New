using Nancy.TinyIoc;
using Sentry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RfReader_demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const int MINIMUM_SPLASH_TIME = 1000;
        public string conn = ConfigurationManager.ConnectionStrings["DSN"].ConnectionString;
        protected override void OnStartup(StartupEventArgs e)
        {
            using (SentrySdk.Init(conn))
            {
                SplashScreen splash = new SplashScreen();
                splash.Show();                
                Stopwatch timer = new Stopwatch();
                timer.Start();
                
                base.OnStartup(e);                
                Login main = new Login();
                timer.Stop();

                int remainingTimeToShowSplash = MINIMUM_SPLASH_TIME - (int)timer.ElapsedMilliseconds;
                if (remainingTimeToShowSplash > 0)
                    Thread.Sleep(remainingTimeToShowSplash);

                splash.Close();
            }
        }
    }
}