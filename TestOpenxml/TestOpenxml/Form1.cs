using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace TestOpenxml
{
    public partial class Form1 : Form
    {
        TestOpenXMlEntities DB = new TestOpenXMlEntities();
        public List<string> ListImg = new List<string>();
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpentFile_Click(object sender, EventArgs e)
        {
            SelectWordFile();
        }
        private string SelectWordFile()
        {
            string fileName = null;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Word document (*.docx)|*.docx";
                dialog.InitialDirectory = Environment.CurrentDirectory;


                // Retore the directory before closing 
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = dialog.FileName;
                    txtFilePath.Text = dialog.FileName;
                }
            }


            return fileName;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            Bitmap bm;
            using (Package package = Package.Open(txtFilePath.Text, System.IO.FileMode.Open))
            {
                foreach (var c in package.GetParts())
                {
                    Console.WriteLine(c.Uri.ToString());
                    if (c.Uri.ToString().Contains("media"))
                    {
                        int length = c.Uri.ToString().Length;
                        int startlen = c.Uri.ToString().LastIndexOf('/') + 1;

                        string str = c.Uri.ToString().Substring(startlen, length - startlen);

                        Image img = Image.FromStream(c.GetStream(), true);

                        bm = new Bitmap(img);
                        bm.SetResolution(img.Width, img.Height);
                        Image image1 = new Bitmap(bm);
                        if (image1.Width > 480)
                        {
                            if (c.ContentType.ToString().Equals("image/x-wmf"))
                            {
                                image1 = ResizeImage(480, 23, image1);
                            }
                            else
                            {
                                image1 = ResizeImage(480, 320, image1);
                            }
                        }
                        else
                        {
                            if (c.ContentType.ToString().Equals("image/x-wmf"))
                            {
                                image1 = ResizeImage(480, 23, image1);
                            }
                        }
                        img = PutOnCanvas(image1, image1.Width, image1.Height, System.Drawing.Color.White);

                        var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = qualityParam;
                        var jpegCodec = GetEncoderInfo("image/bmp");
                        ListImg.Add(@"E:\Admin\OpenXml\SaveFile\" + str.Replace("wmf", "jpg").Replace(".emf", ".jpg"));
                        img.Save(@"E:\Admin\OpenXml\SaveFile\" + str.Replace("wmf", "jpg").Replace(".emf", ".jpg"));
                    }
                }
            }
            using (WordprocessingDocument doc = WordprocessingDocument.Open(txtFilePath.Text, true))
            {
                int i = 0;
                string a = "";
                var body = doc.MainDocumentPart.Document.Body;
                var paras = body.Elements();

                foreach (var para in paras)
                {
                    Question q = new Question();
                    foreach (var run in para.Elements())
                    {
                        foreach (var text in run.Elements())
                        {
                            
                            if (text.InnerXml.Contains("v:shape") || text.InnerXml.Contains("w:drawing"))
                            {
                                a = ListImg[i];
                                i++;
                            }
                            q.Contents += text.InnerText + a;
                            a = "";
                        }
                    }
                    DB.Questions.Add(q);
                    DB.SaveChanges();
                }
            }
        }
        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(t => t.MimeType == mimeType);
        }
        public static Image PutOnCanvas(Image image, int width, int height, Color canvasColor)
        {
            var res = new Bitmap(width, height);
            using (var g = Graphics.FromImage(res))
            {
                g.Clear(canvasColor);
                var x = (width - image.Width) / 2;
                var y = (height - image.Height) / 2;
                g.DrawImageUnscaled(image, x, y, image.Width, image.Height);
            }
            return res;
        }
        public static Image ResizeImage(int MaxWidth, int MaxHeight, Image img)
        {
            Bitmap OrigImg = (System.Drawing.Bitmap)img.Clone(); // clone the original image
            Size ResizedDimensions = GetDimensions(MaxWidth, MaxHeight, ref OrigImg); // generate the new resized dimension
            Bitmap NewImg = new Bitmap(OrigImg, ResizedDimensions); // create a new image accordingly
            img.Dispose();
            OrigImg.Dispose();

            return NewImg;
            //NewImg.Dispose();
        }
        public static Size GetDimensions(int MaxWidth, int MaxHeight, ref Bitmap Img)
        {
            int Height; int Width; float Multiplier;
            Height = Img.Height; Width = Img.Width; // retrieve original height and width
            if (Height <= MaxHeight && Width <= MaxWidth) // if the original dimensions are smaller than the new dimensions
                return new Size(Width, Height); // return the original dimensions [do nothing on size]
            Multiplier = (float)((float)MaxWidth / (float)Width); // otherwise we calculate the multiplier (maxwidth to width ratio)
            if ((Height * Multiplier) <= MaxHeight) // generate the new height if that's the case - if it must be resized
            {
                Height = (int)(Height * Multiplier);
                return new Size(MaxWidth, Height);
            }
            Multiplier = (float)MaxHeight / (float)Height; // calculate another multiplier (maxheight to height ratio)
            Width = (int)(Width * Multiplier); // generate the new width using the multiplier
            return new Size(Width, MaxHeight); // return the newly generated dimensions as Size
        }
    }
}
