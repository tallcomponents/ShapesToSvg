using System;
using System.IO;

using System.Drawing;
using System.Drawing.Drawing2D;

using TallComponents.PDF.Shapes;

namespace ExtractShapes
{
   public class ShapeWriter
   {
      StreamWriter outStream;
      Matrix viewerTransform;

      public ShapeWriter(StreamWriter outStream, Matrix viewerTransform)
      {
         this.outStream = outStream;
         this.viewerTransform = viewerTransform;
      }

      private string GenPoint(double x, double y, Matrix transform)
      {
         PointF[] points = { new PointF((float)x, (float)y) };

         transform.TransformPoints(points);
         viewerTransform.TransformPoints(points);

         return String.Format("{0} {1}", points[0].X, points[0].Y);
      }

      private System.Drawing.Rectangle TransformBox(double x, double y, double w, double h, Matrix transform)
      {
         PointF[] points = { new PointF((float)x, (float)y), new PointF((float)(x + w), (float)(y + h)) };

         transform.TransformPoints(points);

         var rect = new System.Drawing.Rectangle(
                 (int)Math.Min(points[0].X, points[1].X),
                 (int)Math.Min(points[0].Y, points[1].Y),
                 (int)Math.Abs(points[0].X - points[1].X),
                 (int)Math.Abs(points[0].Y - points[1].Y));

         return rect;
      }

      private string GenBox(double x, double y, double w, double h, Matrix transform)
      {
         var rect = TransformBox(x, y, w, h, transform);

         return String.Format("{0} {1} W{2} H{3}", rect.X, rect.Y, rect.Width, rect.Height);
      }

      private void DrawBox(double x, double y, double w, double h, Matrix transform)
      {
         var rect = TransformBox(x, y, w, h, transform);
         //pageGraphics.DrawRectangle(Pens.Black, rect);
      }

      public void WriteShapes(ShapeCollection shapes, Matrix transform)
      {
         foreach (var shape in shapes)
         {
            WriteShape(shape, transform);
         }
      }

      public void WriteShapes(LayerShape shapes, Matrix transform)
      {
         foreach (var shape in shapes)
         {
            WriteShape(shape, transform);
         }
      }

      private void WriteFreeHandPath(FreeHandPath path, Matrix transform)
      {
         foreach (var segment in path.Segments)
         {
            if (segment is FreeHandStartSegment)
            {
               var s = (FreeHandStartSegment)segment;
               outStream.Write("M {0} ", GenPoint(s.X, s.Y, transform));
            }
            else if (segment is FreeHandLineSegment)
            {
               var s = (FreeHandLineSegment)segment;
               outStream.Write("L {0} ", GenPoint(s.X1, s.Y1, transform));
            }
            else if (segment is FreeHandBezierSegment)
            {
               var s = (FreeHandBezierSegment)segment;
               outStream.Write("C {0} {1} {2} ",
                   GenPoint(s.X1, s.Y1, transform),
                   GenPoint(s.X2, s.Y2, transform),
                   GenPoint(s.X3, s.Y3, transform));
            }
         }

         if (path.Closed)
         {
            outStream.Write("Z ");
         }
      }

      private void WriteFreeHandPath(FreeHandPathCollection paths, Matrix transform)
      {
         foreach (var path in paths)
         {
            WriteFreeHandPath(path, transform);
         }
      }

      private void write(FreeHandShape freeHandShape, Matrix transform)
      {
         outStream.Write("<path stroke=\"blue\" stroke-wdith=\"3\" fill=\"none\" d=\"");
         WriteFreeHandPath(freeHandShape.Paths, transform);
         outStream.Write("\"/>\n");
      }

      public void Start(double width, double height)
      {
         outStream.Write("<!DOCTYPE html>\n<html>\n<body>\n<svg width=\"{0}\" height=\"{1}\">\n", width, height);
      }

      public void End()
      {
         outStream.Write("</svg></body></html>");
      }

      public void WriteShape(Shape shape, Matrix transform)
      {
         // ContentShape is a bse class of most shapes. It has an associated transformation. 
         // First it is merged with the root one.
         // The exact shaped based behavior is handled after that.
         if (shape is TallComponents.PDF.Shapes.ContentShape)
         {
            ContentShape contentshape = (ContentShape)shape;

            // Matrix multiplication is not commutative, we need to accumulate
            // transformations from down to up in the tree (from leafs to root), so swap the matrices.
            // This method works because matrix multiplication is associative.
            Matrix newTransform = contentshape.Transform.CreateGdiMatrix();
            newTransform.Multiply(transform);

            if (shape is TallComponents.PDF.Shapes.FreeHandShape)
            {
               write(shape as FreeHandShape, newTransform);
            }
            else if (shape is TallComponents.PDF.Shapes.ShapeCollection)
            {
               WriteShapes((ShapeCollection)shape, newTransform);
            }
            else if (shape is TallComponents.PDF.Shapes.LayerShape)
            {
               WriteShapes((LayerShape)shape, newTransform);
            }
            
            return;
            
            if (shape is TallComponents.PDF.Shapes.BezierShape)
            {
               var s = (BezierShape)shape;
               outStream.Write("BezierShape: ");
               outStream.Write("{{{0} {1} {2} {3}}}",
                   GenPoint(s.X0, s.Y0, newTransform), GenPoint(s.X1, s.Y1, newTransform),
                   GenPoint(s.X2, s.Y2, newTransform), GenPoint(s.X3, s.Y3, newTransform));
               outStream.WriteLine();
            }
            else if (shape is TallComponents.PDF.Shapes.ClipShape)
            {
               var s = (ClipShape)shape;
               outStream.Write("ClipShape: ");
               WriteFreeHandPath(s.Paths, newTransform);
               outStream.WriteLine();
            }
            else if (shape is TallComponents.PDF.Shapes.EllipseShape)
            {
               var s = (EllipseShape)shape;
               outStream.Write("EllipseShape: ");
               outStream.Write("{{C{0} RX{1} RY{2}}}",
                   GenPoint(s.CenterX, s.CenterY, newTransform), s.RadiusX, s.RadiusY);
               outStream.WriteLine();
            }
            else if (shape is TallComponents.PDF.Shapes.ImageShape)
            {
               var s = (ImageShape)shape;
               outStream.Write("ImageShape: ");
               outStream.Write("{{S{0}}}", GenBox(s.X, s.Y, s.Width, s.Height, newTransform));
               outStream.WriteLine();
            }
            else if (shape is TallComponents.PDF.Shapes.LineShape)
            {
               var s = (LineShape)shape;
               outStream.Write("LineShape: ");
               outStream.Write("{{S{0} L{1}}}",
                   GenPoint(s.StartX, s.StartY, newTransform),
                   GenPoint(s.EndX, s.EndY, newTransform));
               outStream.WriteLine();
            }
            else if (shape is TallComponents.PDF.Shapes.RectangleShape)
            {
               var s = (RectangleShape)shape;
               outStream.Write("RectangleShape: ");
               outStream.Write("{{S{0}}}", GenBox(s.X, s.Y, s.Width, s.Height, newTransform));
               outStream.WriteLine();
            }
            else if (shape is TallComponents.PDF.Shapes.TextShape)
            {
               var s = (TextShape)shape;
               outStream.Write("TextShape: ");
               outStream.Write("{{S{0} \"{1}\"}}",
                   GenBox(s.BoundingBox.X, s.BoundingBox.Y, s.BoundingBox.Width, s.BoundingBox.Height, newTransform), s.Text);
               outStream.WriteLine();

               DrawBox(s.BoundingBox.X, s.BoundingBox.Y, s.BoundingBox.Width, s.BoundingBox.Height, newTransform);
            }
            else
            {
               outStream.Write("Unhandled shape: ");
               outStream.WriteLine(shape.GetType().Name);
            }
         }
      }
   }
}
