using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bgoon;
using Bgoon.MultiThread;
using DeltaEditor.XML.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace DeltaEditor {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		public static MainWindow Instance {
			get; private set;
		}

		//Module
		public LoopCore core;

		//Member
		private const float GridThickness = 1f;
		private const float LineThickness = 2f;
		private const int LineResolution = 60;
		private const int MaxLoopCount = 10;
		private const float GridSpace = 0.25f;
		private const float PreviewBallMargin = 100f;
		private static SolidColorBrush GridSubColor = "666666".ToBrush();
		private static SolidColorBrush GridMainColor = "d2d2d2".ToBrush();
		private static SolidColorBrush LineColor = "FFC000".ToBrush();

		public Vector2 GraphCursorPos {
			get {
				Vector2 cursorAbPos = MouseInput.AbsolutePosition;
				return (Vector2)GraphContext.PointFromScreen(new Point(cursorAbPos.x, cursorAbPos.y));
			}
		}
		private Line[] gridLines_subX;
		private Line[] gridLines_subY;
		private Line[] gridLines_main;
		private Line[] previewLines;
		private Line[] animLines;
		private Rangef timeRange = new Rangef(0f, 1f);
		private Rangef viewportRange = new Rangef(-1f, 2f);
		private float viewportWidth = 0.5f;
		private float viewportOffset = 0f;
		private bool onDrag;
		public List<GPoint> pointList = new List<GPoint>();

		//IO
		public FileInfo dataInfo;
		public bool isSaved;
		



		public MainWindow() {
			Instance = this;
			Loaded += OnLoad;
			Closing += OnClosing;

			InitializeComponent();
		}
		private void OnLoad(object sender, EventArgs e) {
			InitModule();
			Init();
			SetEvent();

			FileConnector.RegistExtension(true);
			CheckArgs();
		}
		private void InitModule() {
			core = new LoopCore();

			core.StartLoop();
		}
		private void Init() {
			PreviewSpeedSlider.SetValue(0.3f);

			core.AddRoutine(UpdateAnim());
			core.AddTask(UpdateFrame);

			CreateGraph();

			CreateDefaultPoint();
			RenderGraph();
			UpdatePoints();

			SetSaveDirtyFlag();


			//string jsonString;
			//using(FileStream stream = new FileStream(System.IO.Path.Combine(IOInfo.AppPath, "TestData.bytes"), FileMode.Open, FileAccess.Read)) {
			//	using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
			//		jsonString = reader.ReadToEnd();
			//	}
			//}
			//BSpline2D.RegistDeltaMotion(jsonString, "TestMotion");
		}
		private void SetEvent() {
			SolidColorBrush onbrush = "#FFFFFF".ToBrush();
			SolidColorBrush overBrush = "#F7F7F7".ToBrush();
			SolidColorBrush downBrush = "#EAEAEA".ToBrush();

			OpenBtn.SetBtnColor(onbrush, overBrush, downBrush);
			SaveBtn.SetBtnColor(onbrush, overBrush, downBrush);

			OpenBtn.SetOnClick(Open);
			SaveBtn.SetOnClick(Save);
		}
		private void OnClosing(object sender, CancelEventArgs e) {
			if (!isSaved) {
				MessageBoxResult result = MessageBox.Show("프로그램을 종료하기 전에 저장할까요?", "저장 확인", MessageBoxButton.YesNoCancel);
				switch (result) {
					case MessageBoxResult.Yes:
						Save();
						break;
					case MessageBoxResult.No:
						break;
					case MessageBoxResult.Cancel:
						e.Cancel = true;
						return;
				}
			}
		}

		private void CreateDefaultPoint() {
			GPoint start = AddPoint(0);
			start.SetPoint(1, new Vector2(0f, 0f), false);

			GPoint end = AddPoint(1);
			end.SetPoint(1, new Vector2(1f, 1f), false);

			start.SetDefaultAnchor();
			end.SetDefaultAnchor();
		}
		public void CreateGraph() {
			//Grid
			gridLines_subY = new Line[(int)(1f / GridSpace)];
			for (int i = 0; i < gridLines_subY.Length; ++i) {
				Line line = gridLines_subY[i] = new Line();
				line.StrokeThickness = GridThickness;
				line.Stroke = GridSubColor;
				GraphContext.Children.Add(line);
			}

			gridLines_main = new Line[2];
			for (int i = 0; i < gridLines_main.Length; ++i) {
				Line line = gridLines_main[i] = new Line();
				line.StrokeThickness = GridThickness;
				line.Stroke = GridMainColor;
				GraphContext.Children.Add(line);
			}

			//Line
			previewLines = new Line[LineResolution];
			for (int i = 0; i < LineResolution; ++i) {
				Line line = previewLines[i] = new Line();
				line.StrokeThickness = LineThickness;
				line.Stroke = LineColor;
				GraphContext.Children.Add(line);
			}

			//Anim
			animLines = new Line[2];
			for(int i=0; i<animLines.Length; ++i) {
				Line line = animLines[i] = new Line();
				line.StrokeThickness = GridThickness;
				line.Stroke = GridMainColor;
				PreviewAnimContext.Children.Add(line);
			}
		}
		public void RenderGraph() {
			viewportRange = new Rangef(0f - viewportWidth + viewportOffset, 1f + viewportWidth + viewportOffset);

			float viewportFactor = (float)GraphContext.ActualHeight / viewportRange.Length;

			//Draw Grid_SubX
			if (gridLines_subX != null) {
				for (int i = 0; i < gridLines_subX.Length; ++i) {
					GraphContext.Children.Remove(gridLines_subX[i]);
				}
			}
			float gridCountF = (viewportRange.Length / GridSpace);
			int gridCount = (int)gridCountF;
			float gridCountRemainder = gridCountF - gridCount;
			if(gridCount % 2 != 0) {
				gridCountRemainder += 1f;
			}
			float viewportOffsetRemainder = viewportOffset - (int)viewportOffset;
			if (gridCountF != 0f) {
				gridLines_subX = new Line[gridCount + 1];
				for (int i = 0; i < gridLines_subX.Length; ++i) {
					Line line = gridLines_subX[i] = new Line();
					line.StrokeThickness = GridThickness;
					line.Stroke = GridSubColor;
					GraphContext.Children.Add(line);

					float halfIndex = i - gridCountF * 0.5f;
					float y = ((((halfIndex + gridCountRemainder * 0.5f) / gridCountF)) * (float)GraphContext.ActualHeight) + (float)GraphContext.ActualHeight * 0.5f;

					line.X1 = 0;
					line.X2 = GraphContext.ActualWidth;
					line.Y1 = y;
					line.Y2 = y;
				}
			}
			//Draw Grid_SubY
			for (int i = 0; i < gridLines_subY.Length; ++i) {
				Line line = gridLines_subY[i];
				float lineX = ((float)i / gridLines_subY.Length) * (float)GraphContext.ActualWidth;

				line.X1 = lineX;
				line.X2 = lineX;
				line.Y1 = 0;
				line.Y2 = GraphContext.ActualHeight;
			}
			
			//Draw Grid_Main
			float[] guideYPoints = new float[] {
				0f,
				1f,
			};
			for (int i = 0; i < gridLines_main.Length; ++i) {
				Line line = gridLines_main[i];
				Canvas.SetZIndex(line, 1);
				float y = guideYPoints[i];
				float lineY = (float)(GraphContext.ActualHeight - (y - viewportRange.min) * viewportFactor);
				line.X1 = 0f;
				line.X2 = GraphContext.ActualWidth;
				line.Y1 = lineY;
				line.Y2 = lineY;
			}

			//Draw Line
			Stopwatch watch = new Stopwatch();
			watch.Start();
			double linePieceWidth = GraphContext.ActualWidth / LineResolution;

			float nextY = CalcY(0f);
			for (int i = 0; i < LineResolution; ++i) {
				Line line = previewLines[i];
				Canvas.SetZIndex(line, 2);

				float nextTime = (float)(i + 1) / (LineResolution);

				float y = nextY;
				nextY = CalcY(nextTime);

				Vector2 start = new Vector2(0f, y);
				Vector2 end = new Vector2((float)linePieceWidth, nextY);

				float offset = i * (float)linePieceWidth;
				line.X1 = start.x + offset;
				line.X2 = end.x + offset;
				line.Y1 = GraphContext.ActualHeight - (start.y - viewportRange.min) * viewportFactor;
				line.Y2 = GraphContext.ActualHeight - (end.y - viewportRange.min) * viewportFactor;
			}

			//Draw AnimGrid
			for (int i = 0; i < gridLines_main.Length; ++i) {
				Line line = animLines[i];

				float x = i == 0 ? PreviewBallMargin : (float)PreviewAnimContext.ActualWidth - (float)PreviewBallMargin;
				line.X1 = x;
				line.X2 = x;
				line.Y1 = 0f;
				line.Y2 = (float)PreviewAnimContext.ActualHeight;
			}

			watch.Stop();

			LogText();
		}

		public GPoint AddPoint(int index) {
			GPoint point = new GPoint();
			pointList.Insert(index, point);
			return point;
		}
		public void RemovePoint(GPoint point) {
			point.Destroy();
			pointList.Remove(point);
		}
		public void ClearPoint() {
			for(int i= pointList.Count-1; i>=0; --i) {
				RemovePoint(pointList[i]);
			}
		}

		private void UpdateFrame() {
			if(MouseInput.RightDown) {
				const float DeltaThreshold = 10f;

				int removeIndex = -1;
				int addIndex = -1;

				//Inspect Exist Point
				Vector2 screenCursorPos = this.GraphCursorPos;
				Vector2 viewportPos = ScreenToViewport(screenCursorPos);
				for (int i=0; i<pointList.Count;++i) {
					Vector2 cursorDelta = ViewportToScreen(pointList[i].points[1].Value) - screenCursorPos;
					if(cursorDelta.magnitude < 10f) {
						if (i != 0 && i != pointList.Count - 1) {
							removeIndex = i;
						}
						break;
					}
				}

				//Inspect Line
				float graphY = CalcY(viewportPos.x);
				float cursorDeltaY = ViewportToScreen(new Vector2(0f, graphY)).y - screenCursorPos.y;
				if(Mathf.Abs(cursorDeltaY) < DeltaThreshold * 2f) {

					int rightIndex = FindRightIndex(viewportPos.x);

					addIndex = rightIndex;
				}

				//Create Menu
				Queue<MenuWindowItem> menuQueue = new Queue<MenuWindowItem>();
				if(addIndex != -1) {
					menuQueue.Enqueue(new MenuWindowItem("포인트 추가", () => {
						GPoint point = AddPoint(addIndex);
						float pointY = CalcY(viewportPos.x);
						point.SetPoint(1, new Vector2(viewportPos.x, pointY));
						point.SetDefaultAnchor(0.1f);

						RenderGraph();
						SetSaveDirtyFlag();
					}));
				}
				if(removeIndex != -1) {
					menuQueue.Enqueue(new MenuWindowItem("포인트 제거", () => {
						RemovePoint(pointList[removeIndex]);

						RenderGraph();
						SetSaveDirtyFlag();
					}));
				}
				if (menuQueue.Count > 0) {
					MenuWindow menu = new MenuWindow(menuQueue.ToArray());
					menu.Show();
				}
			}
			const float MaxOffset = 1.5f;
			if (MouseInput.LeftDown) {
				if (KeyInput.GetKey(WinKey.Space)) {
					if (KeyInput.GetKey(WinKey.LeftControl)) {
						const float ZoomStep = 0.005f;
						const float ZoomMin = -0.1f;
						const float ZoomMax = 1.8f;

						float pressPos = MouseInput.AbsolutePosition.x;
						float pressWidth = viewportWidth;
						bool enable = true;
						core.AddTask(() => {
							if (!enable)
								return;
							if(!MouseInput.LeftAuto) {
								enable = false;
							}

							float delta = MouseInput.AbsolutePosition.x - pressPos;

							viewportWidth = Mathf.Clamp(pressWidth + -delta * ZoomStep, ZoomMin, ZoomMax);

							RenderGraph();
							UpdatePoints();
						}, TaskPriolity.EveryFrame, TaskEvent.MouseUpRemove);
					} else {
						const float OffsetStep = 0.01f;
						

						if (!onDrag) {
							onDrag = true;

							float pressPos = MouseInput.AbsolutePosition.y;
							float pressOffset = viewportOffset;
							bool enable = true;
							core.AddTask(() => {
								if (!enable)
									return;
								if (!MouseInput.LeftAuto) {
									onDrag = false;
									enable = false;
								}

								float delta = MouseInput.AbsolutePosition.y - pressPos;

								viewportOffset = Mathf.Clamp(pressOffset + delta * OffsetStep, -MaxOffset, MaxOffset);

								RenderGraph();
								UpdatePoints();
							}, TaskPriolity.EveryFrame, TaskEvent.MouseUpRemove);
						}
					}
				}
			}
			if(MouseInput.MiddleDown) {
				const float OffsetStep = 0.01f;

				if (!onDrag) {
					onDrag = true;

					float pressPos = MouseInput.AbsolutePosition.y;
					float pressOffset = viewportOffset;
					bool enable = true;
					CoreTask task = null;
					task = core.AddTask(() => {
						if (!enable)
							return;
						if (!MouseInput.MiddleAuto) {
							onDrag = false;
							enable = false;

							core.RemoveTask(task);
						}

						float delta = MouseInput.AbsolutePosition.y - pressPos;

						viewportOffset = Mathf.Clamp(pressOffset + delta * OffsetStep, -MaxOffset, MaxOffset);

						RenderGraph();
						UpdatePoints();
					});
				}
			}
		}
		private IEnumerator UpdateAnim() {
			float time = 0f;

			for(; ;) {
				time += PreviewSpeedSlider.Value * 0.05f;
				if(time > 1f) {
					time -= 1f;
				}

				try {
					float ballPos = CalcY(time);
					//float ballPos = BSpline2D.CalcDeltaMotion("TestMotion", time);
					UpdatePreviewBall(ballPos);
				} catch {

				}

				yield return null;
			}
		}
		private void UpdatePreviewBall(float time) {
			PreviewBall.Margin = new Thickness(PreviewBallMargin + time * (float)(GraphContext.ActualWidth - PreviewBallMargin * 2f - PreviewBall.ActualWidth), 0f, 0f, 0f);
		}
		private void UpdatePoints() {
			for(int i=0; i<pointList.Count; ++i) {
				GPoint point = pointList[i];

				point.UpdateHandlePos();
			}
		}

		private int FindRightIndex(float x) {
			int rightIndex = -1;

			for (int i = 1; i < pointList.Count; ++i) {
				if (pointList[i].points[1].Value.x >= x) {
					rightIndex = i;
					break;
				}
			}
			if (rightIndex == -1) {
				return 1;
			}
			return rightIndex;
		}
		private float CalcY(float x) {
			int rightIndex = FindRightIndex(x);

			GPoint left = pointList[rightIndex - 1];
			GPoint right = pointList[rightIndex];

			return BSpline2D.Bezier3_X2Y(x, left.points[1].Value, left.points[2].Value, right.points[0].Value, right.points[1].Value, MaxLoopCount);
		}
		public Vector2 ScreenToViewport(Vector2 graphPos) {
			return new Vector2(graphPos.x / (float)GraphContext.ActualWidth,
				(((float)GraphContext.ActualHeight - graphPos.y) / (float)GraphContext.ActualHeight).Remap(timeRange, viewportRange));
		}
		public Vector2 ViewportToScreen(Vector2 viewportPos) {
			return new Vector2(viewportPos.x * (float)GraphContext.ActualWidth,
				(float)GraphContext.ActualHeight - (viewportPos.y.Remap(viewportRange, timeRange) * (float)GraphContext.ActualHeight));
		}
		private void LogText() {
			return;
			//string format = "0.0000";
			//StringBuilder builder = new StringBuilder();
			//builder.AppendLine("public static float MotionGraph(float time) {");
			//builder.Append("	return BSpline.Bezier3_X2Y(time, new Vector2(");
			//builder.Append(left.Value.x.ToString(format));
			//builder.Append("f, ");
			//builder.Append(left.Value.y.ToString(format));
			//builder.Append("f), new Vector2(");
			//builder.Append(leftAnchor.Value.x.ToString(format));
			//builder.Append("f, ");
			//builder.Append(leftAnchor.Value.y.ToString(format));
			//builder.Append("f), new Vector2(");
			//builder.Append(rightAnchor.Value.x.ToString(format));
			//builder.Append("f, ");
			//builder.Append(rightAnchor.Value.y.ToString(format));
			//builder.Append("f), new Vector2(");
			//builder.Append(right.Value.x.ToString(format));
			//builder.Append("f, ");
			//builder.Append(right.Value.y.ToString(format));
			//builder.AppendLine("f));");
			//builder.Append("}");
			//ResultText.Text = builder.ToString();
		}


		//Data
		public void Open() {
			if (!ShowSaveMsg())
				return;

			OpenFileDialog dialog = new OpenFileDialog();

			dialog.Title = "모션 데이터 열기";
			dialog.DefaultExt = IOInfo.DataExtension;
			dialog.Filter = IOInfo.DataFilter;
			dialog.InitialDirectory = IOInfo.AppPath;
			bool? result = dialog.ShowDialog(this);

			if (result.HasValue && result.Value) {
				Open(new FileInfo(dialog.FileName));
			}
		}
		public void Open(FileInfo dataInfo) {
			ClearPoint();

			this.dataInfo = dataInfo;

			try {
				string jsonString;

				using (FileStream fileStream = new FileStream(dataInfo.FullName, FileMode.Open, FileAccess.Read)) {
					using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8)) {
						jsonString = reader.ReadToEnd();
					}
				}

				JObject jRoot = JObject.Parse(jsonString);

				JArray jPoints = jRoot.SafeGetValue<JArray>("Points", null);
				for(int i=0; i<jPoints.Count; ++i) {
					JObject jPoint = jPoints[i] as JObject;

					GPoint point = AddPoint(i);
					point.SetPoint(0, Vector2.Parse(jPoint.SafeGetValue<string>("p0", null)), false);
					point.SetPoint(1, Vector2.Parse(jPoint.SafeGetValue<string>("p1", null)), false);
					point.SetPoint(2, Vector2.Parse(jPoint.SafeGetValue<string>("p2", null)), false);
				}
				RenderGraph();
				UpdatePoints();
				SetSavedFlag();
			} catch (Exception ex) {
				MessageBox.Show("파일을 여는 도중 오류가 발생했습니다." + Environment.NewLine + ex.ToString());
			}
		}
		public void SaveAs() {
			SaveFileDialog dialog = new SaveFileDialog();

			dialog.Title = "Delta 데이터 저장";
			dialog.DefaultExt = IOInfo.DataExtension;
			dialog.Filter = IOInfo.DataFilter;
			dialog.InitialDirectory = IOInfo.AppPath;
			bool? result = dialog.ShowDialog(this);

			if (result.HasValue && result.Value) {
				dataInfo = new FileInfo(dialog.FileName);

				Save();
			}
		}
		public void Save() {
			if (dataInfo == null) {
				SaveAs();
			} else {
				try {
					//Success
					JObject jRoot = new JObject();
					jRoot.Add("Version", "1");
					jRoot.Add("GUID", Bgoon.Security.Encrypt.GererateHash(8));

					JArray jPoints = new JArray();
					jRoot.Add("Points", jPoints);
					for(int pointI=0; pointI<pointList.Count; ++pointI) {
						GPoint point = pointList[pointI];

						JObject jPoint = new JObject();
						jPoint.Add("p0", point.points[0].Value.ToString());
						jPoint.Add("p1", point.points[1].Value.ToString());
						jPoint.Add("p2", point.points[2].Value.ToString());

						jPoints.Add(jPoint);
					}
					string jsonString = jRoot.ToString();

					dataInfo.Directory.Create();
					jsonString.SaveText(dataInfo.FullName);
					FileInfo binaryInfo = new FileInfo(Path.Combine(dataInfo.Directory.FullName, Path.GetFileNameWithoutExtension(dataInfo.FullName) + "." + IOInfo.BinaryExtension));
					jsonString.SaveText(binaryInfo.FullName);
					ToastMessage.Show("저장되었습니다.");

					SetSavedFlag();
				} catch (Exception ex) {
					MessageBox.Show("저장하는 도중 오류가 발생했습니다." + Environment.NewLine + ex.ToString());
				}
			}
		}
		public bool ShowSaveMsg() {
			if (!isSaved) {
				MessageBoxResult saveMsgResult = MessageBox.Show("작업 중인 파일을 저장하시겠습니까?", "저장 확인", MessageBoxButton.YesNoCancel);
				switch (saveMsgResult) {
					case MessageBoxResult.Yes:
						Save();
						return true;
					case MessageBoxResult.No:
						return true;
					case MessageBoxResult.Cancel:
						return false;
				}
			} else {
				return true;
			}
			return false;
		}

		public void SetSavedFlag() {
			if(dataInfo == null) {
				SetSaveDirtyFlag();
				return;
			}
			isSaved = true;

			Title = dataInfo.FullName;
		}
		public void SetSaveDirtyFlag() {
			isSaved = false;

			string fileName = dataInfo == null ? "저장되지 않은 데이터" : dataInfo.FullName;
			Title = fileName + "*";
		}

		private void CheckArgs() {
			string[] args = Environment.GetCommandLineArgs();

			if (args.Length >= 2) {
				FileInfo dataInfo = new FileInfo(args[1]);

				core.AddJob(() => {
					Open(dataInfo);
				});
			}
		}
	}
	public static class GraphUtility {
		public static float Remap(this float value, Rangef from, Rangef to) {
			return (value - from.min) / from.Length * to.Length + to.min;
		}
	}
	
	
}
