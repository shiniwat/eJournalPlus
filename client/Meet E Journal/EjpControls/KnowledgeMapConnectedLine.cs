using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace SiliconStudio.Meet.EjpControls
{
	[Serializable]
	public enum ConnectedLinePosition
	{
		Start,
		End
	}

	[Serializable]
	public class KnowledgeMapConnectedLine : KnowledgeMapEntityBase
	{
		public Stroke Stroke;
		public StrokeArrowMode ArrowMode;
		public KnowledgeMapEntityBase SourceEntity;
		public KnowledgeMapEntityBase TargetEntity;

        private SolidColorBrush _color;
        public SolidColorBrush Color
        {
            set { this.SetEntityColor(value); }
            get { return this._color; }
        }

        public override void SetEntityColor(System.Windows.Media.SolidColorBrush newColor)
        {
            DrawingAttributes d = this.Stroke.DrawingAttributes.Clone();
            d.Color = newColor.Color;
            this.Stroke.DrawingAttributes = d;
            this._color = newColor;
        }

        public override Color GetEntityColor()
        {
            return this.Stroke.DrawingAttributes.Color;
        }

		public KnowledgeMapConnectedLine(Stroke stroke, StrokeArrowMode arrowMode, KnowledgeMapEntityBase sourceEntity, KnowledgeMapEntityBase targetEntity)
		{
			this.Stroke = stroke;
			this.ArrowMode = arrowMode;
			this.SourceEntity = sourceEntity;
			this.TargetEntity = targetEntity;
		}

		public void Update()
		{
			this.UpdateStart();
			this.UpdateEnd();
		}

		private void UpdateStart()
		{
			Rect entityBoundingBox = this.SourceEntity.GetBounds(2);
			StylusPoint finalPoint = new StylusPoint();

			
			Point SeCenter = new Point(
				entityBoundingBox.Left + entityBoundingBox.Width * 0.5,
				entityBoundingBox.Top + entityBoundingBox.Height * 0.5)
			;

			Point TeCenter = new Point(this.Stroke.StylusPoints[this.Stroke.StylusPoints.Count - 1].X, this.Stroke.StylusPoints[this.Stroke.StylusPoints.Count - 1].Y);
			if (this.TargetEntity != null)
			{
				Rect targetEntityBoundingBox = this.TargetEntity.GetBounds(2);
				TeCenter = new Point(
					targetEntityBoundingBox.Left + targetEntityBoundingBox.Width * 0.5,
					targetEntityBoundingBox.Top + targetEntityBoundingBox.Height * 0.5)
				;
			}

			int LR = 0;
			if ((SeCenter.X - TeCenter.X) < -(entityBoundingBox.Width * 0.8))
				LR = 1;
			else if ((SeCenter.X - TeCenter.X) > (entityBoundingBox.Width * 0.8))
				LR = -1;
			else
				LR = 0;

			int UD = 0;
			if ((SeCenter.Y - TeCenter.Y) < -(entityBoundingBox.Width * 0.8))
				UD = -1;
			else if ((SeCenter.Y - TeCenter.Y) > (entityBoundingBox.Width * 0.8))
				UD = 1;
			else
				UD = 0;

			if (LR == -1)
				finalPoint.X = entityBoundingBox.Left - 2;
			else if (LR == 1)
				finalPoint.X = entityBoundingBox.Right + 2;
			else if (LR == 0)
				finalPoint.X = SeCenter.X;

			if (UD == -1)
				finalPoint.Y = entityBoundingBox.Bottom + 2;
			else if (UD == 1)
				finalPoint.Y = entityBoundingBox.Top - 2;
			else if (UD == 0)
				finalPoint.Y = SeCenter.Y;

			this.Stroke.StylusPoints[0] = finalPoint;

            if (this.ArrowMode == StrokeArrowMode.Double)
            {
                this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
                this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
                this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
                this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);

                for (int i = 0; i < 4; i++)
                    this.Stroke.StylusPoints.RemoveAt(1);

                this.Stroke = InkTransformerHelper.LineToDoubleArrow(this.Stroke);
            }
            this.Stroke.DrawingAttributes.Height = 2;
            this.Stroke.DrawingAttributes.Width = 2;
            this.Stroke.DrawingAttributes.IgnorePressure = true;
		}

		private void UpdateEnd()
		{
			if (this.TargetEntity != null)
			{
				Rect sourceEntityBoundingBox = this.SourceEntity.GetBounds(2);
				StylusPoint finalPoint = new StylusPoint();

				Rect targetEntityBoundingBox = this.TargetEntity.GetBounds(2);

				Point SeCenter = new Point(
					sourceEntityBoundingBox.Left + sourceEntityBoundingBox.Width * 0.5,
					sourceEntityBoundingBox.Top + sourceEntityBoundingBox.Height * 0.5)
				;

				Point TeCenter = new Point(
					targetEntityBoundingBox.Left + targetEntityBoundingBox.Width * 0.5,
					targetEntityBoundingBox.Top + targetEntityBoundingBox.Height * 0.5)
				;

				int LR = 0;
				if ((SeCenter.X - TeCenter.X) < -(sourceEntityBoundingBox.Width * 0.8))
					LR = -1;
				else if ((SeCenter.X - TeCenter.X) > (sourceEntityBoundingBox.Width * 0.8))
					LR = 1;
				else
					LR = 0;

				int UD = 0;
				if ((SeCenter.Y - TeCenter.Y) < -(sourceEntityBoundingBox.Width * 0.8))
					UD = 1;
				else if ((SeCenter.Y - TeCenter.Y) > (sourceEntityBoundingBox.Width * 0.8))
					UD = -1;
				else
					UD = 0;

				if (LR == -1)
					finalPoint.X = targetEntityBoundingBox.Left - 2;
				else if (LR == 1)
					finalPoint.X = targetEntityBoundingBox.Right + 2;
				else if (LR == 0)
					finalPoint.X = TeCenter.X;

				if (UD == -1)
					finalPoint.Y = targetEntityBoundingBox.Bottom + 2;
				else if (UD == 1)
					finalPoint.Y = targetEntityBoundingBox.Top - 2;
				else if (UD == 0)
					finalPoint.Y = TeCenter.Y;

				this.Stroke.StylusPoints[this.Stroke.StylusPoints.Count - 1] = finalPoint;
			}

			if (this.ArrowMode == StrokeArrowMode.Single || this.ArrowMode == StrokeArrowMode.Double)
			{
				this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
				this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
				this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
				this.Stroke.StylusPoints.RemoveAt(this.Stroke.StylusPoints.Count - 2);
				this.Stroke = InkTransformerHelper.LineToSingleArrow(this.Stroke);
			}
            this.Stroke.DrawingAttributes.Height = 2;
            this.Stroke.DrawingAttributes.Width = 2;
            this.Stroke.DrawingAttributes.IgnorePressure = true;
		}

		public override void DisplayAnchorPoint(Rect location)
		{
			//Connected Lines have no AnchorPoints
		}

		public override void HideAnchorPoints()
		{
			//Connected Lines have no AnchorPoints
		}

        public override void ShowCommentNote(object sender, RoutedEventArgs e)
        {
            //Connected Lines have no comments
        }

        public override void DrawCommentConnectorLine()
        {
            //Connected Lines have no comments
        }

		public override void Release()
		{
			//Do nothing...
		}
	}
}
