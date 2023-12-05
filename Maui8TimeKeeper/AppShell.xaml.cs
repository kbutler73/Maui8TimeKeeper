using Maui8TimeKeeper.Views;

namespace Maui8TimeKeeper
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(TimeCardDetailView), typeof(TimeCardDetailView));
        }
    }
}