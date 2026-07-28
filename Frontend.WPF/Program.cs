using System.Windows;
using Velopack;

namespace Frontend.WPF
{
    public class Program
    {
        [STAThread]
        static void Main()
       {
            try
            {
                SplashScreen splashScreen = new SplashScreen(@"\Resources\Images\Splash.png");
                splashScreen.Show(true);
                // Logging is essential for debugging! Ideally you should write it to a file.
                //Log = new MemoryLogger();

                // It's important to Run() the VelopackApp as early as possible in app startup.
                //Add Log inside 'Run();'... so it would be 'Run(Log);'...
                VelopackApp.Build()
                    .OnFirstRun((v) => { /* Your first run code here */ })
                    .Run();

                // We can now launch the WPF application as normal.
                var app = new App();
                app.InitializeComponent();
                app.Run();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Unhandled exception: " + ex.ToString());
            }
        }
    }
}
