using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace SiliconStudio.Meet.EjpControls
{
	public static class InkTransformerHelper
	{
        public static Stroke PositionsToSquare(Point startPoint, Point endPoint)
        {
            StylusPointCollection stp = new StylusPointCollection();
            stp.Add(new StylusPoint(startPoint.X, startPoint.Y));
            stp.Add(new StylusPoint(endPoint.X, endPoint.Y));
            return InkTransformerHelper.StrokeToSquare(new Stroke(stp));
        }

        public static Stroke PositionsToCircle(Point startPoint, Point endPoint)
        {
            StylusPointCollection stp = new StylusPointCollection();
            stp.Add(new StylusPoint(startPoint.X, startPoint.Y));
            stp.Add(new StylusPoint(endPoint.X, endPoint.Y));
            return InkTransformerHelper.StrokeToCircle(new Stroke(stp));
        }

		public static Stroke StrokeToLine(Stroke originalStroke)
		{
			List<StylusPoint> newLinePointList = new List<StylusPoint>();
			newLinePointList.Add(originalStroke.StylusPoints[0]);
			newLinePointList.Add(originalStroke.StylusPoints[originalStroke.StylusPoints.Count - 1]);
			StylusPointCollection newLinePointCollection = new StylusPointCollection(newLinePointList);
			Stroke newStroke = new Stroke(newLinePointCollection);
			return newStroke;
		}

		public static Stroke StrokeToSquare(Stroke originalStroke)
		{
			List<StylusPoint> newSquarePointList = new List<StylusPoint>();
			Geometry squareGeometry = originalStroke.GetGeometry();
			newSquarePointList.Add(new StylusPoint(squareGeometry.Bounds.Left, squareGeometry.Bounds.Top));
			newSquarePointList.Add(new StylusPoint(squareGeometry.Bounds.Right, squareGeometry.Bounds.Top));
			newSquarePointList.Add(new StylusPoint(squareGeometry.Bounds.Right, squareGeometry.Bounds.Bottom));
			newSquarePointList.Add(new StylusPoint(squareGeometry.Bounds.Left, squareGeometry.Bounds.Bottom));
			newSquarePointList.Add(new StylusPoint(squareGeometry.Bounds.Left, squareGeometry.Bounds.Top));
			StylusPointCollection newSquarePointCollection = new StylusPointCollection(newSquarePointList);
			Stroke newStroke = new Stroke(newSquarePointCollection);
			return newStroke;
		}

		public static Stroke StrokeToCircle(Stroke originalStroke)
		{
			//Fix the center.
			StylusPoint start = originalStroke.StylusPoints[0];
			double radiusX = originalStroke.GetGeometry().Bounds.Width * 0.5;
			double radiusY = originalStroke.GetGeometry().Bounds.Height * 0.5;
			double ycenter = originalStroke.GetGeometry().Bounds.Top + radiusY;
			double xcenter = originalStroke.GetGeometry().Bounds.Left + radiusX;

			StylusPointCollection s = new StylusPointCollection();
			double angle = 0.0f;
			for (int i = 0; i < 360; i++)
			{
				angle = Math.PI * i / 180.0;
				s.Add(new StylusPoint(xcenter+(radiusX*Math.Cos(angle)), ycenter+(radiusY*Math.Sin(angle))));
			}

			//make sure to close the loop
			s.Add(new StylusPoint(s[0].X, s[0].Y));

			Stroke st = new Stroke(s);
			return st;
		}

		public static Stroke LineToOctagon(Stroke originalStroke)
		{
			StylusPoint start = originalStroke.StylusPoints[0];
			double r = originalStroke.GetBounds().Width * 0.5;
			double y = originalStroke.GetBounds().Top;
			double x = start.X;
			StylusPointCollection s = new StylusPointCollection();

			s.Add(new StylusPoint(x + r, y));
			s.Add(new StylusPoint(r + x, -0.4142 * r + y));
			s.Add(new StylusPoint(0.7071 * r + x, -0.7071 * r + y));
			s.Add(new StylusPoint(0.4142 * r + x, -r + y));
			s.Add(new StylusPoint(x, -r + y));

			s.Add(new StylusPoint(-0.4142 * r + x, -r + y));
			s.Add(new StylusPoint(-0.7071 * r + x, -0.7071 * r + y));
			s.Add(new StylusPoint(-r + x, -0.4142 * r + y));
			s.Add(new StylusPoint(-r + x, y));
			s.Add(new StylusPoint(-r + x, 0.4142 * r + y));
			s.Add(new StylusPoint(-0.7071 * r + x, 0.7071 * r + y));
			s.Add(new StylusPoint(-0.4142 * r + x, r + y));
			s.Add(new StylusPoint(x, r + y));
			s.Add(new StylusPoint(0.4142 * r + x, r + y));
			s.Add(new StylusPoint(0.7071 * r + x, 0.7071 * r + y));
			s.Add(new StylusPoint(r + x, 0.4142 * r + y));
			s.Add(new StylusPoint(r + x, y));

			Stroke st = new Stroke(s);
			return st;
		}

		public static Stroke LineToSingleArrow(Stroke originalStroke)
		{
			StylusPoint pB1 = originalStroke.StylusPoints[originalStroke.StylusPoints.Count - 1];
            StylusPoint pB2 = originalStroke.StylusPoints[0];

			double slopy, cosy, siny;
			double Par = 20.0;  //length of Arrow (>)

			slopy = Math.Atan2((pB1.Y - pB2.Y), (pB1.X - pB2.X));
			cosy = Math.Cos(slopy);
			siny = Math.Sin(slopy);

			//side 1
			originalStroke.StylusPoints.Add(new StylusPoint(
				pB1.X + (-Par * cosy - (Par / 2.0 * siny)),
				pB1.Y + (-Par * siny + (Par / 2.0 * cosy))));

			originalStroke.StylusPoints.Add(pB1);

			//side 2
			originalStroke.StylusPoints.Add(new StylusPoint(
				pB1.X + (-Par * cosy + (Par / 2.0 * siny)),
				pB1.Y - (Par / 2.0 * cosy + Par * siny)));

			originalStroke.StylusPoints.Add(pB1);

			return originalStroke;
		}

        public static Stroke LineToDoubleArrow(Stroke originalStroke)
        {
            //end
            StylusPoint pB1 = originalStroke.StylusPoints[originalStroke.StylusPoints.Count - 1];
            StylusPoint pB2 = originalStroke.StylusPoints[0];

            double slopy, cosy, siny;
            double Par = 20.0;  //length of Arrow (>)

            slopy = Math.Atan2((pB1.Y - pB2.Y), (pB1.X - pB2.X));
            cosy = Math.Cos(slopy);
            siny = Math.Sin(slopy);

            //side 1
            originalStroke.StylusPoints.Add(new StylusPoint(
                pB1.X + (-Par * cosy - (Par / 2.0 * siny)),
                pB1.Y + (-Par * siny + (Par / 2.0 * cosy))));

            originalStroke.StylusPoints.Add(pB1);

            //side 2
            originalStroke.StylusPoints.Add(new StylusPoint(
                pB1.X + (-Par * cosy + (Par / 2.0 * siny)),
                pB1.Y - (Par / 2.0 * cosy + Par * siny)));

            originalStroke.StylusPoints.Add(pB1);

            //Start
            StylusPoint eB1 = originalStroke.StylusPoints[0];
            StylusPoint eB2 = originalStroke.StylusPoints[originalStroke.StylusPoints.Count -1];

            slopy = Math.Atan2((eB1.Y - eB2.Y), (eB1.X - eB2.X));
            cosy = Math.Cos(slopy);
            siny = Math.Sin(slopy);

            //side 1
            //originalStroke.StylusPoints.Insert(0, eB1);
            originalStroke.StylusPoints.Insert(0, new StylusPoint(
                eB1.X + (-Par * cosy - (Par / 2.0 * siny)),
                eB1.Y + (-Par * siny + (Par / 2.0 * cosy))));

            //side 2
            originalStroke.StylusPoints.Insert(0, eB1);

            originalStroke.StylusPoints.Insert(0, new StylusPoint(
                eB1.X + (-Par * cosy + (Par / 2.0 * siny)),
                eB1.Y - (Par / 2.0 * cosy + Par * siny)));

            originalStroke.StylusPoints.Insert(0, pB2);

            return originalStroke;
        }
	}
}
