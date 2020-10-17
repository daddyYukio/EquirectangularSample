using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace EquirectangularSample
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
		int index = 0;
        public MainPage()
        {
            InitializeComponent();

			equirectanglarView.TextureImage = LoadBinaryResource("image.jpg");

		}

		protected byte[] LoadBinaryResource(string name)
		{
			byte[] resource = null;
			var assembly = typeof(App).GetTypeInfo().Assembly;
			using (var stream = assembly.GetManifestResourceStream("EquirectangularSample.Resources." + name))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				resource = ms.ToArray();
			}
			return resource;
		}
	}
}
