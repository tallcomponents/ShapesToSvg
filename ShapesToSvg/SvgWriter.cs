using System;
using System.IO;

using System.Drawing;
using System.Drawing.Drawing2D;

using TallComponents.PDF.Shapes;

namespace ExtractShapes
{
   public class SvgWriter
   {
      StreamWriter outStream;

      public SvgWriter(StreamWriter outStream)
      {
         this.outStream = outStream;
      }

      private string writePoint(double x, double y, Matrix transform)
      {
         PointF[] points = { new PointF((float)x, (float)y) };

         transform.TransformPoints(points);

         return String.Format("{0} {1}", points[0].X, points[0].Y);
      }

      public void writeShapes(ShapeCollection shapes, Matrix transform)
      {
         foreach (var shape in shapes)
         {
            WriteShape(shape, transform);
         }
      }

      public void writeLayerShape(LayerShape shapes, Matrix transform)
      {
         foreach (var shape in shapes)
         {
            WriteShape(shape, transform);
         }
      }

      private void writeFreeHandPath(FreeHandPath path, Matrix transform)
      {
         foreach (var segment in path.Segments)
         {
            if (segment is FreeHandStartSegment)
            {
               var s = (FreeHandStartSegment)segment;
               outStream.Write("M {0} ", writePoint(s.X, s.Y, transform));
            }
            else if (segment is FreeHandLineSegment)
            {
               var s = (FreeHandLineSegment)segment;
               outStream.Write("L {0} ", writePoint(s.X1, s.Y1, transform));
            }
            else if (segment is FreeHandBezierSegment)
            {
               var s = (FreeHandBezierSegment)segment;
               outStream.Write("C {0} {1} {2} ",
                   writePoint(s.X1, s.Y1, transform),
                   writePoint(s.X2, s.Y2, transform),
                   writePoint(s.X3, s.Y3, transform));
            }
         }

         if (path.Closed)
         {
            outStream.Write("Z ");
         }
      }

      private void writeFreeHandPaths(FreeHandPathCollection paths, Matrix transform)
      {
         foreach (var path in paths)
         {
            writeFreeHandPath(path, transform);
         }
      }

      private void writeFreeHandShape(FreeHandShape freeHandShape, Matrix transform)
      {
         outStream.Write("<path stroke=\"blue\" stroke-wdith=\"3\" fill=\"none\" d=\"");
         writeFreeHandPaths(freeHandShape.Paths, transform);
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
            newTransform.Multiply(transform, MatrixOrder.Append);

            if (shape is TallComponents.PDF.Shapes.FreeHandShape)
            {
               writeFreeHandShape(shape as FreeHandShape, newTransform);
            }
            else if (shape is TallComponents.PDF.Shapes.ShapeCollection)
            {
               writeShapes((ShapeCollection)shape, newTransform);
            }
            else if (shape is TallComponents.PDF.Shapes.LayerShape)
            {
               writeLayerShape((LayerShape)shape, newTransform);
            }
         }
      }
   }
}
