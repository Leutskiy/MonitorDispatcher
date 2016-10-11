using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Sender.EXControls
{
    using Sender.Entities;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;

    public class ImageListView : ListViewEx
    {
        public string DirImageFiles { get; set; }

        public  List<string> Images { get; set; }

        public void Refresh(List<string> addedImages)
        {
            foreach (var img in addedImages)
	        {
                this.AddRow(img, "", "");
	        }
        }

        public void GetSourceListImages()
        {

            Refresh(Images);
        }

        public ImageListView() : base()
        {
            Images = new List<string>();
            this.AddRow("", "", "");
        }

        public ImageListView(string serializableImagelistPath)
            : this()
        {
            if (!File.Exists(serializableImagelistPath))
            {
                return;
            }

            using (Stream fs = new FileStream(serializableImagelistPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                if (fs.Length == 0)
                {
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();

                List<string> imagesObjectGraph = formatter.Deserialize(fs) as List<string>;

                if (imagesObjectGraph != null)
                {
                    Images = imagesObjectGraph;

                    this.GetSourceListImages();
                }
            }
        }
    }
}
