using Velopack;

namespace Frontend.WPF
{
    public class Program
    {
        [STAThread]
        static void Main()
       {
            VelopackApp.Build().Run();
            App application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
