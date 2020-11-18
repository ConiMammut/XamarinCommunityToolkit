﻿using Xamarin.Forms.Platform.MacOS;
using AppKit;
using Foundation;
using Xamarin.Forms;
using Xamarin.CommunityToolkit.macOS.Effects;
using Xamarin.CommunityToolkit.Effects;

[assembly: ExportEffect(typeof(PlatformTouchEffect), nameof(TouchEffect))]

namespace Xamarin.CommunityToolkit.macOS.Effects
{
	[Preserve(AllMembers = true)]
	public class PlatformTouchEffect : PlatformEffect
	{
		NSGestureRecognizer gesture;
		TouchEffect effect;
		MouseTrackingView mouseTrackingView;

		protected override void OnAttached()
		{
			effect = TouchEffect.PickFrom(Element);
			if (effect?.IsDisabled ?? true)
				return;

			effect.Control = Element as VisualElement;

			if (Container != null)
			{
				gesture = new TouchNSClickGestureRecognizer(effect, Container);
				Container.AddGestureRecognizer(gesture);
				Container.AddSubview(mouseTrackingView = new MouseTrackingView(effect));
			}
		}

		protected override void OnDetached()
		{
			if (effect?.Control == null)
				return;

			mouseTrackingView?.RemoveFromSuperview();
			mouseTrackingView?.Dispose();
			mouseTrackingView = null;
			effect.Control = null;
			effect = null;
			if (gesture != null)
				Container?.RemoveGestureRecognizer(gesture);

			gesture?.Dispose();
			gesture = null;
		}
	}

	sealed class MouseTrackingView : NSView
	{
		NSTrackingArea trackingArea;
		TouchEffect effect;

		public MouseTrackingView(TouchEffect effect)
		{
			this.effect = effect;
			AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
		}

		public override void UpdateTrackingAreas()
		{
			if (trackingArea != null)
			{
				RemoveTrackingArea(trackingArea);
				trackingArea.Dispose();
			}
			trackingArea = new NSTrackingArea(Frame, NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveAlways, this, null);
			AddTrackingArea(trackingArea);
		}

		public override void MouseEntered(NSEvent theEvent)
		{
			if (effect?.IsDisabled ?? true)
				return;

			effect?.HandleHover(HoverStatus.Entered);
		}

		public override void MouseExited(NSEvent theEvent)
		{
			if (effect?.IsDisabled ?? true)
				return;

			effect?.HandleHover(HoverStatus.Exited);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (trackingArea != null)
				{
					RemoveTrackingArea(trackingArea);
					trackingArea.Dispose();
				}
				effect = null;
			}
			base.Dispose(disposing);
		}
	}

	sealed class TouchNSClickGestureRecognizer : NSGestureRecognizer
	{
		TouchEffect effect;
		NSView container;

		public TouchNSClickGestureRecognizer(TouchEffect effect, NSView container)
		{
			this.effect = effect;
			this.container = container;
		}

		Rectangle ViewRect
		{
			get
			{
				var frame = container.Frame;
				var parent = container.Superview;
				while (parent != null)
				{
					frame = new CoreGraphics.CGRect(frame.X + parent.Frame.X, frame.Y + parent.Frame.Y, frame.Width, frame.Height);
					parent = parent.Superview;
				}
				return frame.ToRectangle();
			}
		}

		public override void MouseDown(NSEvent mouseEvent)
		{
			if (effect?.IsDisabled ?? true)
				return;

			effect?.HandleUserInteraction(TouchInteractionStatus.Started);
			effect?.HandleTouch(TouchStatus.Started);
			base.MouseDown(mouseEvent);
		}

		public override void MouseUp(NSEvent mouseEvent)
		{
			if (effect?.IsDisabled ?? true)
				return;

			if (effect.HoverStatus == HoverStatus.Entered)
			{
				var touchPoint = mouseEvent.LocationInWindow.ToPoint();
				var status = ViewRect.Contains(touchPoint)
					? TouchStatus.Completed
					: TouchStatus.Canceled;

				effect?.HandleTouch(status);
			}
			effect?.HandleUserInteraction(TouchInteractionStatus.Completed);

			base.MouseUp(mouseEvent);
		}

		public override void MouseDragged(NSEvent mouseEvent)
		{
			if (effect?.IsDisabled ?? true)
				return;

			var status = ViewRect.Contains(mouseEvent.LocationInWindow.ToPoint()) ? TouchStatus.Started : TouchStatus.Canceled;

			if ((status == TouchStatus.Canceled && effect.HoverStatus == HoverStatus.Entered) ||
				(status == TouchStatus.Started && effect.HoverStatus == HoverStatus.Exited))
				effect?.HandleHover(status == TouchStatus.Started ? HoverStatus.Entered : HoverStatus.Exited);

			if (effect.Status != status)
				effect?.HandleTouch(status);

			base.MouseDragged(mouseEvent);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				effect = null;
				container = null;
			}
			base.Dispose(disposing);
		}
	}
}