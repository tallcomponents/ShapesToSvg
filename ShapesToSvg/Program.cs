using System;
using System.IO;

using System.Drawing;
using System.Drawing.Drawing2D;

using TallComponents.PDF;
using R = TallComponents.PDF.Rasterizer;

namespace ExtractShapes
{
   class Program
   {
      static void Main(string[] args)
      {
         string[] fileNames = new string[]  
         {
            "oval", "3BigPreview90", "0703 Base-11", "5242_C3.0-0_Bid_Set"
         };

         foreach (string fileName in fileNames)
         {
            convert(fileName);
         }
      }

      static void convert(string fileName)
      {
         using (FileStream inFile = new FileStream(string.Format(@"..\..\{0}.pdf", fileName), FileMode.Open, FileAccess.Read))
         {
            Document document = new Document(inFile);

            foreach (var page in document.Pages)
            {
               var viewerTransform = GetViewerTransform(page);

               int pagenr = page.Index + 1;

               using (FileStream outFile = new FileStream(string.Format(@"..\..\{0}_{1}.html", fileName, pagenr), FileMode.Create, FileAccess.Write))
               {
                  using (StreamWriter outStream = new StreamWriter(outFile))
                  {
                     double viewerWidth = page.Orientation == Orientation.Rotate0 || page.Orientation == Orientation.Rotate180 ? page.Width : page.Height;
                     double viewerHeight = page.Orientation == Orientation.Rotate0 || page.Orientation == Orientation.Rotate180 ? page.Height : page.Width;
                     var writer = new ShapeWriter(outStream);
                     writer.Start(viewerWidth, viewerHeight);
                     var shapes = page.CreateShapes();
                     writer.WriteShape(shapes, viewerTransform);
                     writer.End();
                  }
               }
            }
         }
      }

      // It creates a tranformation which handles rotation and
      // transforms PDF coordinate space into scree ncoordinate space
      private static Matrix GetViewerTransform(Page page)
      {
         var mediaBox = page.MediaBox;

         double height;
         double dx, dy;
         int rotate;

         switch (page.Orientation)
         {
            case Orientation.Rotate0:
            default:
               height = mediaBox.Height;
               rotate = 0;
               dx = 0;
               dy = 0;
               break;
            case Orientation.Rotate90:
               height = mediaBox.Width;
               rotate = 90;
               dx = 0;
               dy = -mediaBox.Height;
               break;
            case Orientation.Rotate180:
               height = mediaBox.Height;
               rotate = 180;
               dx = -mediaBox.Width;
               dy = -mediaBox.Height;
               break;
            case Orientation.Rotate270:
               height = mediaBox.Width;
               rotate = 270;
               dx = -mediaBox.Width;
               dy = 0;
               break;
         }

         Matrix matrix = new Matrix();

         // From PDF coordinate system (zero is at the bottom) to screen (zero is at the top)
         matrix.Translate(0, (float)height);
         matrix.Scale(1, -1);

         // Rotation
         matrix.Rotate(rotate);
         matrix.Translate((float)dx, (float)dy);


         // TODO: cropbox translation should be handled as well
         return matrix;
      }
   }
}
