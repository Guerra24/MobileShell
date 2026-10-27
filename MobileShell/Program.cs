using XamlHostingKit;

namespace MobileShell;

internal partial class Program
{

    [STAThread]
    public static void Main(string[] args)
    {
        XamlApplication.Start((p) => new App());
    }

}
