// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public class ImagePreviewControl : UserControl
    {
        private const int ClicksToDouble = 3;
        private static readonly double ClickZoomFactor = Math.Pow(2, 1.0 / ClicksToDouble);

        private bool currentlyAnimating;
        private Point? dragOffset;
        private Image image;
        private PointF offset;
        private float zoom;

        public ImagePreviewControl()
        {
            this.TabStop = false;
            this.SetStyle(ControlStyles.Opaque | ControlStyles.Selectable, value: false);
            this.DoubleBuffered = true;
        }

        public Image Image
        {
            get => this.image;

            set
            {
                this.StopAnimating();
                this.image = value;
                this.zoom = 1;
                this.offset = PointF.Empty;
                this.Invalidate();
            }
        }

        public PointF Offset
        {
            get => this.offset;

            set
            {
                this.offset = value;
                this.Invalidate();
            }
        }

        public float Zoom
        {
            get => this.zoom;

            set
            {
                this.zoom = value;
                this.Invalidate();
            }
        }

        public void AnimateImage()
        {
            if (!this.currentlyAnimating)
            {
                ImageAnimator.Animate(this.image, this.OnFrameChanged);
                this.currentlyAnimating = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StopAnimating();
            }

            base.Dispose(disposing);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.UpdateAnimationState();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                this.zoom = 1;
                this.offset = PointF.Empty;
                this.Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.dragOffset = e.Location;
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.dragOffset != null)
            {
                var start = this.dragOffset.Value;
                var location = e.Location;
                this.dragOffset = location;
                this.Offset = new PointF(
                    this.Offset.X + (location.X - start.X),
                    this.Offset.Y + (location.Y - start.Y));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.dragOffset = null;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var zoomFactor = (float)Math.Pow(ClickZoomFactor, Math.Sign(e.Delta));
            this.PerformZoom(zoomFactor, new PointF(e.X, e.Y));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var image = this.image;
            if (image != null)
            {
                this.UpdateAnimationState();
                ImageAnimator.UpdateFrames(image);
                var rect = this.GetImageRectangle();
                e.Graphics.DrawImage(image, rect);
            }

            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // TODO: Recalculate offset based on zoom change.
            this.Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            this.UpdateAnimationState();
        }

        private static SizeF ComputeBaseSize(Size imageSize, Size controlSize)
        {
            var baseZoom = ComputeZoom(imageSize, controlSize);
            return new SizeF(imageSize.Width * baseZoom, imageSize.Height * baseZoom);
        }

        private static PointF ComputeCentering(SizeF imageSize, Size controlSize) => new PointF(
            (controlSize.Width - imageSize.Width) / 2,
            (controlSize.Height - imageSize.Height) / 2);

        private static void ComputeSizeAndBaseOffset(Size imageSize, Size controlSize, float zoom, out SizeF size, out PointF baseOffset)
        {
            var baseSize = ComputeBaseSize(imageSize, controlSize);
            size = new SizeF(baseSize.Width * zoom, baseSize.Height * zoom);
            baseOffset = ComputeCentering(size, controlSize);
        }

        private static float ComputeZoom(Size imageSize, Size controlSize) => Math.Min(
            (float)controlSize.Width / imageSize.Width,
            (float)controlSize.Height / imageSize.Height);

        private RectangleF GetImageRectangle()
        {
            var image = this.image;
            if (image == null)
            {
                return RectangleF.Empty;
            }

            ComputeSizeAndBaseOffset(image.Size, this.Size, this.Zoom, out var size, out var baseOffset);
            var offset = new PointF(baseOffset.X + this.Offset.X, baseOffset.Y + this.Offset.Y);
            return new RectangleF(offset, size);
        }

        private void OnFrameChanged(object sender, EventArgs e)
        {
            if (!this.IsDisposed && !this.Disposing)
            {
                this.Invalidate();
            }
        }

        private void UpdateAnimationState()
        {
            if (this.Enabled && this.Visible)
            {
                this.AnimateImage();
            }
            else
            {
                this.StopAnimating();
            }
        }

        private void PerformZoom(float factor, PointF fixedLocation)
        {
            Size imageSize;
            if (this.image is Image image)
            {
                imageSize = image.Size;
            }
            else
            {
                return;
            }

            var controlSize = this.Size;
            var newZoom = this.Zoom * factor;
            ComputeSizeAndBaseOffset(imageSize, controlSize, this.Zoom, out var size, out var baseOffset);
            ComputeSizeAndBaseOffset(imageSize, controlSize, newZoom, out var newSize, out var newBaseOffset);

            this.offset = new PointF(
                fixedLocation.X - ((fixedLocation.X - (this.offset.X + baseOffset.X) - (size.Width / 2)) * factor + (newSize.Width / 2) + newBaseOffset.X),
                fixedLocation.Y - ((fixedLocation.Y - (this.offset.Y + baseOffset.Y) - (size.Height / 2)) * factor + (newSize.Height / 2) + newBaseOffset.Y));
            this.Zoom = newZoom;
        }

        private void StopAnimating()
        {
            if (this.currentlyAnimating)
            {
                ImageAnimator.StopAnimate(this.image, this.OnFrameChanged);
                this.currentlyAnimating = false;
            }
        }
    }
}
