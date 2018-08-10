using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Bgoon;

namespace DeltaEditor {
	public class GPoint {
		private static MainWindow MainWindow => MainWindow.Instance;
		private static Canvas GraphContext => MainWindow.GraphContext;
		private static LoopCore Core => MainWindow.core;
		
		private static SolidColorBrush HandleColor = "f0dfb5".ToBrush();
		
		private const float HandleSize = 14f;
		private const float HalfHandleSize = HandleSize * 0.5f;

		public Property<Vector2>[] points;
		private Shape[] handles;
		private Vector2[] anchorDelta;
		private Line[] handleLines;

		public GPoint() {
			CreateComponent();
			SetEvent();
		}
		public void Destroy() {
			for(int i=0; i<handleLines.Length; ++i) {
				GraphContext.Children.Remove(handleLines[i]);
			}
			for(int i=0; i<handles.Length; ++i) {
				GraphContext.Children.Remove(handles[i]);
			}
		}
		private void CreateComponent() {
			points = new Property<Vector2>[3];
			anchorDelta = new Vector2[2];
			handles = new Shape[3];
			handleLines = new Line[2];

			for (int i = 0; i < points.Length; ++i) {
				points[i] = new Property<Vector2>();
			}
			handles[0] = new Ellipse();
			handles[1] = new Rectangle();
			handles[2] = new Ellipse();
			handleLines[0] = new Line();
			handleLines[1] = new Line();

			//Create Line 
			for (int i = 0; i < handleLines.Length; ++i) {
				Line line = handleLines[i] = new Line();
				line.Stroke = HandleColor;
				line.StrokeThickness = 1f;
				GraphContext.Children.Add(line);
			}

			//Create Handle
			for (int i = 0; i < handles.Length; ++i) {
				int index = i;
				Shape handle;
				if (i == 1) {
					handle = handles[i] = new Rectangle();
				} else {
					handle = handles[i] = new Ellipse();
					handle.Opacity = 0.5f;
				}

				handle.Fill = HandleColor;
				handle.Width = handle.Height = HandleSize;
				handle.HorizontalAlignment = HorizontalAlignment.Left;
				handle.VerticalAlignment = VerticalAlignment.Top;
				handle.SetBtnColor("f0dfb5".ToBrush(), "f0dfb5".ToColor().Light(30).ToBrush(), "f0dfb5".ToColor().Light(-30).ToBrush());
				Canvas.SetZIndex(handle, 3);
				GraphContext.Children.Add(handle);
			}
		}
		private void SetEvent() {
			for(int i=0; i<handles.Length; ++i) {
				int index = i;
				Shape handle = handles[i];

				if (i == 1) {
					handle.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
						Core.AddTask(() => {
							Vector2 graphCursorPos = MainWindow.GraphCursorPos;
							if (!graphCursorPos.CheckOverlap(0, (float)GraphContext.ActualWidth, 0, (float)GraphContext.ActualHeight))
								return;

							SetPoint(index, MainWindow.ScreenToViewport(graphCursorPos), false);
							SetPoint(0, points[1].Value + anchorDelta[0], false);
							SetPoint(2, points[1].Value + anchorDelta[1], true);
						}, TaskPriolity.EveryFrame, TaskEvent.MouseUpRemove);
					};
				} else {
					handle.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
						Core.AddTask(() => {
							Vector2 graphCursorPos = MainWindow.GraphCursorPos;
							if (!graphCursorPos.CheckOverlap(0, (float)GraphContext.ActualWidth, 0, (float)GraphContext.ActualHeight))
								return;

							SetPoint(index, MainWindow.ScreenToViewport(graphCursorPos));
						}, TaskPriolity.EveryFrame, TaskEvent.MouseUpRemove);
					};
				}
			}
		}

		public void SetDefaultAnchor(float offset = 0.3f) {
			SetPoint(0, points[1].Value + new Vector2(-offset, 0f), false);
			SetPoint(2, points[1].Value + new Vector2(offset, 0f), true);
		}
		public void SetPoint(int index, Vector2 pos, bool update = true) {
			points[index].Value = pos;

			if(index != 1) {
				int handleIndex = index == 0 ? 0 : 1;

				anchorDelta[handleIndex] = pos - points[1].Value;
			}
			if (update) {
				UpdateHandlePos();
				MainWindow.RenderGraph();
			}

			MainWindow.SetSaveDirtyFlag();
		}

		public void UpdateHandlePos() {
			//Handle
			for (int i = 0; i < handles.Length; ++i) {
				Shape handle = handles[i];
				Vector2 graphPos = MainWindow.ViewportToScreen(points[i].Value);

				Canvas.SetLeft(handle, graphPos.x - HalfHandleSize);
				Canvas.SetTop(handle, graphPos.y - HalfHandleSize);
			}
			//Line
			for (int i = 0; i < handleLines.Length; ++i) {
				Line line = handleLines[i];
				Shape handle = handles[i];
				Shape nextHandle = handles[i + 1];
				line.X1 = Canvas.GetLeft(handle) + HalfHandleSize;
				line.Y1 = Canvas.GetTop(handle) + HalfHandleSize;
				line.X2 = Canvas.GetLeft(nextHandle) + HalfHandleSize;
				line.Y2 = Canvas.GetTop(nextHandle) + HalfHandleSize;
			}
		}
	}
}
