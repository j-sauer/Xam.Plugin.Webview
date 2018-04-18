using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SampleApp.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StringDataSample : ContentPage
    {
        public StringDataSample()
        {
            InitializeComponent();
            if (Device.RuntimePlatform == Device.iOS)
            {
                stringContent.Source = @"
<!doctype html>
<html>
    <body><h1>This is a HTML string</h1><img src='Default.png'/></body>
</html>
            ";
            }
            else
            {
                stringContent.Source = @"
<!doctype html>
<html>
    <body><h1>This is a HTML string</h1></body>
</html>
            ";
            }
        }
    }
}