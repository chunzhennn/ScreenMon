using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ScreenMonServer
{
    public class ImageManager
    {
        private readonly string _saveDirectory;

        public ImageManager(string saveDirectory)
        {
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);
            _saveDirectory = saveDirectory;
        }

        public void SaveImage(Guid id, byte[] imageDataBytes)
        {
            var imageDirectory = Path.Combine(_saveDirectory, id.ToString());
            if (!Directory.Exists(imageDirectory))
                Directory.CreateDirectory(imageDirectory);
            var filePath = Path.Combine(_saveDirectory, id.ToString(),
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            using var file = File.Create(filePath);
            file.Write(imageDataBytes);
        }

        public string GetImageDirectory(Guid id)
        {
            return Path.Combine(_saveDirectory, id.ToString());
        }
    }
}
