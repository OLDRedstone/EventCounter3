using EvtCtr3.Animation;
using EvtCtr3.Assets;
using EvtCtr3.Core;
using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Events;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics.CodeAnalysis;
using static EvtCtr3.Extensions;
using System.ComponentModel;
using System.Diagnostics;
using OpenTK;
using System.Reflection;
using System.Text;
namespace EvtCtr3
{
	public partial class EventsUI : SKControl
	{
		private Tabs CurrentTab = Tabs.Sounds;
		private const int gap = 1;
		private const int tabWidth = 15;
		private const int tabCloseWidth = 19;
		private const int tabHeight = 13;
		private const int tabContentWidth = 6;
		private const int tabContentOpenWidth = 15;
		private const string themeKey = "themes";
		private const int scrollSensitivity = 14;
		private static readonly Counter Counter = new();
		private readonly Tabs[] tabs = [Tabs.Sounds, Tabs.Rows, Tabs.Actions, Tabs.Rooms, Tabs.Decorations, Tabs.Windows, Tabs.Unknown];
		private readonly List<EventType> showingTypes = [];
		private readonly AnimationTimer timer;
		private readonly List<EventInfoBox> showingInfoBoxes = [];
		private readonly EaseFloat viewStartTop;
		private SKPointI pivot1 = default; // 功能栏锚点
		private SKPointI pivotf = default; // Tab 栏锚点
		private SKPointI pivot2 = default; // 事件栏锚点
		private SKPointI pivot3 = default; // 信息栏锚点1
		private SKPointI pivot4 = default; // 信息栏锚点2
		private int infoCountMax;
		private int themeVariant = 0;
		private bool infoCountMaxUpdated = false;
		private bool muted = false;
		private bool tabOpen = false;
		private bool expandedAll = false;
		private bool reverseButtonReversed = false;
		private string statusText = "";
		private SKPointI tabStartPoint;
		private EventType? Hovering;
		private CountingMethod countingMethod = CountingMethod.Detailed;
		private Dictionary<EventType, ICounterResultItem> counterItems = [];
		private ReadOnlyEnumCollection<EventType> eventTypesThatHasData = new(2);
		private Config config = new();
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public int PixelScale
		{
			get => ps;
			set
			{
				if (value < 1)
					value = 1;
				if (ps != value)
				{
					ps = value;
					ReloadTheme();
				}
			}
		}
		private int ps = 2;
		public EventsUI()
		{
			InitializeComponent();
			timer = GlobalAnimationTimer;
			viewStartTop = new(timer, TimeSpan.FromMilliseconds(500));
			viewStartTop.ValueChanging += (s, e) => Invalidate();
			//for (int i = 0; i < 5; i++)
			//{
			//	EventInfoBox item = new(timer)
			//	{
			//		ResultItem = new CounterResultItemSimply
			//		{
			//			Type = (EventType)i,
			//			Count = Random.Shared.Next(150, 200)
			//		}
			//	};
			//	item.StateChanged += (s, e) => Invalidate();
			//	infoBoxes.Add(item);
			//	item.IsHovering = false;
			//}
			//for (int i = 10; i < 15; i++)
			//{
			//	EventInfoBox item = new(timer)
			//	{
			//		ResultItem = new CounterResultItemDetailed(20)
			//		{
			//			Type = (EventType)i,
			//			CountsPerBar = [.. Enumerable.Range(0, 20).Select(_ => Random.Shared.Next(1, 20))],
			//		}
			//	};
			//	item.ResultItem.Count = (item.ResultItem as CounterResultItemDetailed)?.CountsPerBar.Sum() ?? 0;
			//	item.StateChanged += (s, e) => Invalidate();
			//	infoBoxes.Add(item);
			//	item.IsHovering = false;
			//}
			ReloadTheme();
			timer.Interval = 2;
			timer.Start();
		}
		[MemberNotNull(nameof(typeface), nameof(font), nameof(fontPaint))]
		public void ReloadTheme()
		{
			config = AssetManager.LoadConfig();
			themeVariant = config.Theme;
			ps = config.PixelSize;
			countingMethod = config.CountingMethod;
			Localization.CurrentKey = config.Language;
			Localization.Reload();
			backColor = AssetManager.GetColor(themeKey, new(0, themeVariant));
			meshBackColor = AssetManager.GetColor(themeKey, new(1, themeVariant));
			meshForeColor = AssetManager.GetColor(themeKey, new(2, themeVariant));
			iconBackColor = AssetManager.GetColor(themeKey, new(3, themeVariant));
			rulerBarColor = AssetManager.GetColor(themeKey, new(4, themeVariant));
			barBackColor = AssetManager.GetColor(themeKey, new(5, themeVariant));
			textColor = AssetManager.GetColor(themeKey, new(6, themeVariant));
			foreach (Tabs tab in tabs)
			{
				SKColor color1 = AssetManager.GetColor(TabColorKey(tab), new(0, themeVariant));
				SKColor color2 = AssetManager.GetColor(TabColorKey(tab), new(1, themeVariant));
				tabColors[tab] = (color1, color2);
			}
			typeface?.Dispose();
			typeface = SKTypeface.FromFamilyName(config.FontFamily);
			font?.Dispose();
			font = new()
			{
				Typeface = typeface,
				Size = 10 * ps,
				Subpixel = true,
			};
			fontPaint?.Dispose();
			fontPaint = new()
			{
				Color = textColor,
				IsAntialias = false,
				Style = SKPaintStyle.Fill,
			};
			tabStartPoint = new SKPointI(2 * ps, 2 * ps);
			Invalidate();
		}
		protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
		{
			var cvs = e.Surface.Canvas;
			cvs.Clear(backColor);
			DrawTabs(cvs, out pivot1, out pivot3, out pivotf);
			if (tabOpen)
				DrawTabFunctions(pivotf, cvs);
			DrawBackground(pivot1, pivot3.Y, cvs, out pivot2);
			DrawEvents(pivot2, cvs);
			DrawStatus(pivot3, cvs, out pivot4);
			DrawShownEvent(pivot4, cvs);
			//DrawWaterMark(new(Width / 2, Height / 2), cvs);
		}
		private void DrawTabs(SKCanvas cvs, out SKPointI rightTop, out SKPointI leftBottom, out SKPointI tabFuncs)
		{
			SKPointI sp = tabStartPoint;
			int indent = 3;
			SKRectI rect = default;
			foreach (var tab in tabs)
			{
				bool isActive = CurrentTab == tab;
				if (isActive)
				{
					cvs.DrawSlice("content_2", SKRectI.Create(tabCloseWidth * ps, tabHeight * ps) with { Location = sp }, tabColors[tab].open, ps);
					rect = cvs.DrawSlice(TabIconKey(tab), sp with { X = sp.X + ps * indent }, tabColors[tab].open, ps);
				}
				else
				{
					cvs.DrawSlice("content_2", SKRectI.Create(tabWidth * ps, tabHeight * ps) with { Location = sp }, tabColors[tab].close, ps);
					rect = cvs.DrawSlice(TabIconKey(tab), sp, tabColors[tab].close, ps);
				}
				sp.Y += rect.Height + gap * ps;
			}
			int height = sp.Y - tabStartPoint.Y - gap * ps;
			leftBottom = new SKPointI(tabStartPoint.X, sp.Y);
			SKPointI tabContentStart = new(tabStartPoint.X + 1 * ps + rect.Width, tabStartPoint.Y);
			int tcw = tabOpen ? tabContentOpenWidth : tabContentWidth;
			SKPointI tabFuncArea = new(tabStartPoint.X + 1 * ps + rect.Width, tabStartPoint.Y);
			cvs.DrawSlice("content_1", SKRectI.Create(tabFuncArea.X, tabFuncArea.Y, tcw * ps, height), tabColors[CurrentTab].open, ps);
			tabFuncs = new SKPointI(tabFuncArea.X + 2 * ps, tabFuncArea.Y + 2 * ps);
			tabContentStart.X += tcw * ps;
			rightTop = tabContentStart;
		}
		private void DrawTabFunctions(in SKPointI start, SKCanvas cvs)
		{
			SKPointI p = start;
			SKRectI bound = SKRectI.Create(start, new(11 * ps, 6 * ps));
			SKPointI icon = new(start.X + (bound.Width - 5 * ps) / 2, start.Y + (bound.Height - 4 * ps) / 2);
			foreach (var iconKey in tabFunctionIcons)
			{
				cvs.DrawSlice("content_1", bound, tabColors[CurrentTab].close, ps);
				cvs.DrawSlice(iconKey, icon, ps);
				bound.Offset(0, bound.Height + 2 * ps);
				icon.Offset(0, bound.Height + 2 * ps);
			}
		}
		private void DrawEvents(in SKPointI start, SKCanvas cvs)
		{
			SKPointI p = start;
			EventType[] types = [.. EventsOf(CurrentTab), .. (CurrentTab is Tabs.Unknown ? [] : unSupportedType.Where(showingTypes.Contains))];
			foreach (EventType type in types)
			{
				SKRectI willdraw = type.GetEventIconBounds(p, isEnabled: eventTypesThatHasData.Contains(type), isActive: Hovering == type || showingTypes.Contains(type), scale: ps);
				if (willdraw.Right > Width)
				{
					p.X = start.X;
					p.Y += willdraw.Height;
				}
				willdraw.Location = p;
				cvs.DrawEventIcon(CurrentTab, type, p, isEnabled: eventTypesThatHasData.Contains(type), isActive: Hovering == type || showingTypes.Contains(type), scale: ps);
				p.X = willdraw.Right;
			}
		}
		private void DrawBackground(SKPointI start, int height, SKCanvas cvs, out SKPointI leftTop)
		{
			start.X += 2 * ps;
			SKRectI bound = SKRectI.Create(start, new(11 * ps, 6 * ps));
			SKPointI icon = new(start.X + (bound.Width - 5 * ps) / 2, start.Y + (bound.Height - 4 * ps) / 2);
			foreach (var iconKey in globalFunctionIcons)
			{
				cvs.DrawSlice("content_1", bound, iconBackColor, ps);
				cvs.DrawSlice(iconKey, icon, ps);
				bound.Offset(13 * ps, 0);
				icon.Offset(13 * ps, 0);
			}
			bound.Right = Width;
			cvs.DrawSlice("content_1", bound, barBackColor, ps);

			start.Offset(0, 8 * ps);
			cvs.DrawSlice("content_1", SKRectI.Create(start.X, start.Y, Width - start.X, 7 * ps), iconBackColor, ps);
			for (int i = start.X; i < Width; i += 14 * ps)
				cvs.DrawRect(SKRectI.Create(i, start.Y + 5 * ps, 1 * ps, 2 * ps), new() { Color = rulerBarColor });
			start.Offset(0, 7 * ps);
			cvs.DrawRect(SKRectI.Create(start.X, start.Y, Width - start.X, height - start.Y), new() { Color = meshBackColor });
			for (int i = start.X; i < Width; i += 14 * ps)
				cvs.DrawRect(SKRectI.Create(i, start.Y, 1 * ps, height - start.Y), new() { Color = meshForeColor });
			for (int i = start.Y; i < height; i += 14 * ps)
				cvs.DrawRect(SKRectI.Create(start.X, i, Width - start.X, 1 * ps), new() { Color = i == start.Y ? rulerBarColor : meshForeColor });
			leftTop = start;
		}
		private void DrawStatus(in SKPointI start, SKCanvas cvs, out SKPointI end)
		{
			//string textToDraw = Hovering is EventType type ? string.Format(Localization.Get("status_bar", "selected_events"), (counterItems.TryGetValue(type, out var item) ? item.Count : 0), type.ToString()) : Localization.Get("status_bar", "no_event_selected");

			//float width = font.MeasureText(statusText, out SKRect bounds, fontPaint);
			cvs.DrawText(statusText, start.X, start.Y + 12 * ps, font, fontPaint);
			end = start with { Y = start.Y + 15 * ps };
		}
		private void DrawShownEvent(in SKPointI start, SKCanvas cvs, in bool drawAll = false)
		{
			const int scrollBarWidth = 2;
			SKPoint current = start;
			int save = cvs.Save();
			if (!drawAll)
			{
				cvs.ClipRect(SKRect.Create(start.X, start.Y, Width - start.X, Height - start.Y));
				current.Y -= viewStartTop;
			}
			if (showingInfoBoxes.Count == 0)
				goto ret;
			if (!infoCountMaxUpdated)
			{
				infoCountMax = showingInfoBoxes.Max(i => i.ResultItem.Count);
				infoCountMaxUpdated = true;
			}
			float barWidth = Width - current.X - 2 * ps;
			const float fontScaleT = 2f;
			const float countIndentLength = 14;
			IEnumerable<EventInfoBox> list = muted ? showingInfoBoxes.OrderByDescending(i => i.ResultItem.Count) : [.. showingInfoBoxes];
			foreach (var box in list)
			{
				if (box.Height == 0)
					continue;
				float bh = box.Height.Value * ps;
				if (current.Y + bh < start.Y)
				{
					current.Y += bh;
					continue;
				}
				int count = box.ResultItem.Count;
				float h2 = float.Min(box.Height, EventInfoBox.hoverHeight);
				float bw = barWidth * ((float)box.ResultItem.Count / infoCountMax);
				Debug.Print($"{box.Height.Target}, {box.Height.Value}");
				float barLeft = barWidth - bw;
				float fullInfoPercentage = box.FullInfoPercentage;
				(SKColor back, SKColor front) = muted ? tabColors[Tabs.Unknown] : tabColors[TabOf(box.ResultItem.Type)];
				back.ToHsl(out float h, out float s, out float l);
				back = SKColor.FromHsv(h, s, l / 2);
				cvs.DrawRect(SKRect.Create(current.X, current.Y, barWidth, bh),
					new() { Color = back });
				using SKFont tempFont = new()
				{
					Typeface = typeface,
					Size = (h2 / fontScaleT * ps),
				};
				using SKPaint fontPaint2 = fontPaint.Clone();
				if (fullInfoPercentage < 1)
					cvs.DrawRect(
						current.X + (barLeft * (1 - fullInfoPercentage)),
						current.Y + (bh * (0.5f + (0.5f * fullInfoPercentage))),
						bw + (barLeft * fullInfoPercentage),
						bh * 0.5f * (1 - fullInfoPercentage),
						new() { Color = front.WithAlpha((byte)(255 * (1 - fullInfoPercentage / 4))) });
				fontPaint2.IsStroke = true;
				fontPaint2.StrokeWidth = 2 * ps;
				fontPaint2.Color = back;
				cvs.DrawRect(current.X, current.Y, barWidth, float.Min(2 * ps, bh),
					new() { Color = front.WithAlpha(127) });
				cvs.DrawText(count.ToString(), current.X + 2 * ps, current.Y + h2 / fontScaleT * ps,
					tempFont, fontPaint2);
				cvs.DrawText(muted ? "???" : Localization.Get("events", box.ResultItem.Type.ToString()), current.X + barWidth, current.Y + h2 / fontScaleT * ps,
					SKTextAlign.Right, tempFont, fontPaint2);
				cvs.DrawText(count.ToString(), current.X + 2 * ps, current.Y + h2 / fontScaleT * ps,
					tempFont, fontPaint);
				cvs.DrawText(muted ? "???" : Localization.Get("events", box.ResultItem.Type.ToString()), current.X + barWidth, current.Y + h2 / fontScaleT * ps,
					SKTextAlign.Right, tempFont, fontPaint);
				if (fullInfoPercentage > 0)
				{
					fontPaint2.Color = fontPaint2.Color.WithAlpha(192);
					fontPaint2.Color = fontPaint2.Color.WithAlpha((byte)(255 * fullInfoPercentage));
					using SKPaint fontPaint3 = fontPaint.Clone();
					fontPaint3.Color = fontPaint3.Color.WithAlpha((byte)(255 * fullInfoPercentage));
					if (box.ResultItem is CounterResultItemDetailed crid)
					{
						int[] cts = crid.CountsPerBar;
						int ctsl = cts.Length;
						int maxcts = cts.Max();
						for (int i = 0; i < ctsl; i++)
						{
							int c = cts[i];
							SKPoint cp = new(
								current.X + barLeft * (1 - fullInfoPercentage) + (i * (barWidth - barLeft * (1 - fullInfoPercentage))) / ctsl,
								current.Y + bh * (fullInfoPercentage + 1) / 2
								);
							cvs.DrawRect(
								cp.X, cp.Y,
								(float)(barWidth - barLeft * (1 - fullInfoPercentage)) / ctsl,
								-bh * c * fullInfoPercentage / maxcts,
								new() { Color = front.WithAlpha((byte)(255 * (1 - fullInfoPercentage * (c == maxcts ? 0.1 : ((i % 2) * 0.1 + 0.4))))) });
						}
						cvs.DrawText(maxcts.ToString(), current.X + countIndentLength * ps + h2 / fontScaleT * ps, current.Y + h2 / fontScaleT * ps, tempFont, fontPaint2);
						cvs.DrawText(maxcts.ToString(), current.X + countIndentLength * ps + h2 / fontScaleT * ps, current.Y + h2 / fontScaleT * ps, tempFont, fontPaint3);
					}
				}
				current.Y += (int)box.Height * ps;
				if (current.Y > Height && !drawAll)
					break;
			}
			if (!drawAll)
			{
				(float min, float max, float total) = GetViewStartTopLimit(start);
				if (max - min < total)
				{
					int sbh = Height - start.Y;
					cvs.DrawRect(start.X + barWidth, start.Y + (min / total * sbh), scrollBarWidth * ps, (max - min) / total * sbh, new() { Color = meshForeColor });
				}
			}
		ret:
			cvs.Restore();
		}
		private void DrawWaterMark(SKPointI start, SKCanvas cvs)
		{
			Assembly b = Assembly.GetExecutingAssembly();
			var v = b.GetName().Version;
			string name = Localization.Get("title", "name");
			string unstable = Localization.Get("watermark", "unstable");
			string textToDraw = $"EvtCtr3 - {name}\nV{v}-beta\n{unstable}";
			cvs.Save();
			cvs.Translate(start);
			cvs.RotateDegrees(-30);
			SKPointI p = new(0, 0);
			using SKPaint waterMarkPaint = new()
			{
				Color = textColor.WithAlpha(32),
				IsAntialias = true,
				Style = SKPaintStyle.Fill,
			};
			foreach (string str in textToDraw.Split('\n'))
			{
				float width = font.MeasureText(str, out SKRect bounds, waterMarkPaint);
				cvs.DrawText(str, p.X - width / 2, p.Y + 12 * ps, font, waterMarkPaint);
				p.Y += 30;
			}
			cvs.Restore();
		}
		private SKColor backColor;
		private SKColor meshBackColor;
		private SKColor meshForeColor;
		private SKColor iconBackColor;
		private SKColor rulerBarColor;
		private SKColor barBackColor;
		private SKColor textColor;
		private SKTypeface typeface;
		private SKFont font;
		private SKPaint fontPaint;
		private readonly Dictionary<Tabs, (SKColor close, SKColor open)> tabColors = [];
		private static string TabColorKey(Tabs tab) => tab switch
		{
			Tabs.Sounds => "tab_color_sounds",
			Tabs.Rows => "tab_color_rows",
			Tabs.Actions => "tab_color_actions",
			Tabs.Rooms => "tab_color_rooms",
			Tabs.Decorations => "tab_color_decorations",
			Tabs.Windows => "tab_color_windows",
			Tabs.Unknown => "tab_color_unknown",
			_ => ""
		};
		private static string TabIconKey(Tabs tab) => tab switch
		{
			Tabs.Sounds => "tab_icon_sounds",
			Tabs.Rows => "tab_icon_rows",
			Tabs.Actions => "tab_icon_actions",
			Tabs.Rooms => "tab_icon_rooms",
			Tabs.Decorations => "tab_icon_decorations",
			Tabs.Windows => "tab_icon_windows",
			Tabs.Unknown => "tab_icon_unknown",
			_ => ""
		};
		protected override void OnMouseClick(MouseEventArgs e)
		{
			var tab = GetTabArea(e.Location.ToSKPoint());
			if (tab is Tabs tabnotnull)
			{
				if (CurrentTab == tabnotnull)
				{
					if (e.Button == MouseButtons.Left)
					{
						tabOpen = !tabOpen;
						Invalidate();
					}
				}
				else
				{
					CurrentTab = tabnotnull;
					UpdateClearButtonIcon();
					Invalidate();
				}
			}
			if (tabOpen)
			{
				var tabFuncIndex = GetTabFunctionArea(e.Location.ToSKPoint());
				switch (tabFuncIndex)
				{
					case 0:
						bool hasReversed = false;
						foreach (var type1 in EventsOf(CurrentTab))
						{
							if (showingTypes.Contains(type1))
							{
								RemoveEventInfo(type1);
								hasReversed = true;
							}
							else if (eventTypesThatHasData.Contains(type1))
							{
								AddEventInfo(type1);
								hasReversed = true;
							}
						}
						if (hasReversed)
						{
							reverseButtonReversed = !reverseButtonReversed;
							tabFunctionIcons[0] = reverseButtonReversed ? "icon_reverse_2" : "icon_reverse_1";
						}
						break;
					case 1:
						foreach (var type1 in EventsOf(CurrentTab))
						{
							if (showingTypes.Contains(type1))
								RemoveEventInfo(type1);
						}
						break;

				}
			}
			var func = GetGlobalFunctionArea(e.Location.ToSKPoint());
			switch (func)
			{
				case 0:
					{
						using OpenFileDialog ofd = new()
						{
							Filter = Localization.Get("dialog", "open_file_filter"),
							Title = Localization.Get("dialog", "open_file_title")
						};
						if (ofd.ShowDialog() != DialogResult.OK)
							return;
						string file = ofd.FileName;
						//string file = @"E:\Download\Mike Geno - Cybernetic Program.rdzip";
						counterItems.Clear();
						switch (countingMethod)
						{
							case CountingMethod.Detailed:
								var result1 = Counter.CountDetailed(file);
								foreach (EventType t1 in Enum.GetValues<EventType>())
								{
									var r = result1[t1];
									if (r is CounterResultItemDetailed crid)
										counterItems[t1] = crid;
								}
								break;
							case CountingMethod.Simply:
								var result2 = Counter.CountSimply(file);
								foreach (EventType t2 in Enum.GetValues<EventType>())
								{
									var r = result2[t2];
									if (r is CounterResultItemSimply cris)
										counterItems[t2] = cris;
								}
								break;
						}
						UpdateEventTypesThatHasData();
						return;
					}
				case 1:
					// MessageBox.Show(Localization.Get("menu", "achievement") + " button clicked!");
					return;
				case 2:
					expandedAll = !expandedAll;
					foreach (var box in showingInfoBoxes)
					{
						if (box.ResultItem is not CounterResultItemSimply)
						{
							box.IsExpanded = expandedAll;
							box.UpdateState();
						}
					}
					globalFunctionIcons[2] = expandedAll ? "icon_expandall" : "icon_foldall";
					UpdateStatusText();
					Invalidate();
					UpdateViewLimit();
					return;
				case 3:
					muted = !muted;
					globalFunctionIcons[3] = muted ? "icon_visible_inactive" : "icon_visible_active";
					UpdateStatusText();
					Invalidate();
					return;
				case 4:
					{
						using SaveFileDialog sfd = new()
						{
							Filter = Localization.Get("dialog", "save_counter_filter"),
							Title = Localization.Get("dialog", "save_counter_title")
						};
						if (sfd.ShowDialog() == DialogResult.OK)
						{
							string filename = Path.GetFileNameWithoutExtension(sfd.FileName).Trim('.');
							using SKBitmap bmp = new(Width, (int)showingInfoBoxes.Sum(i => i.Height * ps));
							using SKCanvas cvs = new(bmp);
							DrawShownEvent(new SKPointI(0, 0), cvs, drawAll: true);
							using FileStream fs = new(sfd.FileName + ".png", FileMode.Create, FileAccess.Write);
							bmp.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);
						}
					}
					return;
				case 5:
					themeVariant = (themeVariant + 1) % 4;
					config.Theme = themeVariant;
					AssetManager.SaveConfigToYaml(config);
					ReloadTheme();
					return;
				case 6:
					{
						AssetManager.SaveConfigToYaml(config);
						Form? f = FindForm();
						f?.Close();
						return;
					}
			}
			var type = GetEventTypeArea(e.Location.ToSKPoint());
			if (type is EventType typenotnull)
			{
				if (showingTypes.Contains(typenotnull))
					RemoveEventInfo(typenotnull);
				else if (eventTypesThatHasData.Contains(typenotnull))
					AddEventInfo(typenotnull);
				return;
			}
			var infoBoxIndex = GetEventInfoBoxArea(e.Location.ToSKPoint());
			if (infoBoxIndex != -1)
			{
				var box = showingInfoBoxes[infoBoxIndex];
				if (box.ResultItem is not CounterResultItemSimply)
				{
					box.IsExpanded = !box.IsExpanded;
					box.UpdateState();
				}
				UpdateViewLimit();
				return;
			}
		}
		private int hoveringGlobalFunctionIndex = -1;
		private int hoveringTabFunctionIndex = -1;
		private Tabs? hoveringTab = null;
		protected override void OnMouseMove(MouseEventArgs e)
		{
			var tab = GetTabArea(e.Location.ToSKPoint());
			if (tab != hoveringTab)
			{
				hoveringTab = tab;
				UpdateStatusText();
				Invalidate();
				return;
			}
			var funcIndex1 = GetGlobalFunctionArea(e.Location.ToSKPoint());
			if (funcIndex1 != hoveringGlobalFunctionIndex)
			{
				hoveringGlobalFunctionIndex = funcIndex1;
				UpdateStatusText();
				Invalidate();
				return;
			}
			var funcIndex2 = GetTabFunctionArea(e.Location.ToSKPoint());
			if (funcIndex2 != hoveringTabFunctionIndex)
			{
				hoveringTabFunctionIndex = funcIndex2;
				UpdateStatusText();
				Invalidate();
				return;
			}
			var type = GetEventTypeArea(e.Location.ToSKPoint());
			if (Hovering != type)
			{
				Hovering = type;
				UpdateStatusText();
				Invalidate();
				return;
			}
			var infoBoxIndex = GetEventInfoBoxArea(e.Location.ToSKPoint());
			for (int i = 0; i < showingInfoBoxes.Count; i++)
			{
				var box = showingInfoBoxes[i];
				if (box.IsHovering != (i == infoBoxIndex))
				{
					box.IsHovering = i == infoBoxIndex;
					box.UpdateState();
				}
			}
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			var index = GetEventInfoBoxArea(e.Location.ToSKPoint());
			if (index < 0)
				return;
			float scroll = e.Delta / 120;
			float target = viewStartTop.Target - scroll * scrollSensitivity * ps;
			if (target < 0)
			{
				viewStartTop.SetImmediately(target);
				viewStartTop.Value = 0;
				return;
			}
			(float min, float max, float total) = GetViewStartTopLimit(pivot4);
			if (max > total)
			{
				viewStartTop.SetImmediately(target);
				viewStartTop.Value = float.Max(0, total - (max - min));
				return;
			}
			viewStartTop.Value = target;
		}
		private bool UpdateViewLimit()
		{
			(float min, float max, float total) = GetViewStartTopLimit(pivot4);
			if (min < 0)
			{
				viewStartTop.Value = 0;
				return true;
			}
			if (max > total)
			{
				viewStartTop.Value = float.Max(0, total - (max - min));
				return true;
			}
			return false;
		}
		private (float min, float max, float total) GetViewStartTopLimit(SKPointI start)
		{
			float totalHeight = showingInfoBoxes.Sum(i => i.Height.Target * ps);
			return (viewStartTop, viewStartTop + (Height - start.Y), totalHeight);
		}
		private static readonly string[] globalFunctionIcons = [
			"icon_openfile",
			"icon_achievement",
			"icon_foldall",
			"icon_visible_active",
			"icon_export",
			"icon_theme",
			"icon_close",
		];
		private static readonly string[] tabFunctionIcons = [
			"icon_reverse_1",
			"icon_empty"
		];
		private void UpdateEventTypesThatHasData()
		{
			eventTypesThatHasData = new ReadOnlyEnumCollection<EventType>(2, counterItems.Keys.ToArray());
			EventType[] typesToRemove = [.. showingTypes];
			foreach (var type in typesToRemove)
				RemoveEventInfo(type);
			Invalidate();
		}
		private void AddEventInfo(EventType type, bool force = false)
		{
			if (!force && (showingTypes.Contains(type) || showingInfoBoxes.Any(i => i.ResultItem.Type == type)))
				return;
			showingTypes.Add(type);
			if (!counterItems.TryGetValue(type, out var item))
				return;
			EventInfoBox box = new(timer)
			{
				ResultItem = item,
			};
			box.StateChanged += UpdateInterface;
			showingInfoBoxes.Add(box);
			UpdateClearButtonIcon();
			infoCountMaxUpdated = false;
		}
		private void RemoveEventInfo(EventType type, bool force = false)
		{
			if (!force && !showingTypes.Remove(type))
				return;
			List<EventInfoBox> boxesToRemove = [];
			foreach (var box in showingInfoBoxes)
				if (box.ResultItem.Type == type)
					boxesToRemove.Add(box);
			foreach (var box in boxesToRemove)
			{
				box.Height.ValueChangingEnd += (s, e) =>
				{
					showingInfoBoxes.Remove(box);
					box.StateChanged -= UpdateInterface;
					infoCountMaxUpdated = false;
				};
				box.Height.Value = 0;
			}
			infoCountMaxUpdated = false;
			UpdateClearButtonIcon();
			Invalidate();
		}
		private void UpdateClearButtonIcon()
		{
			if (showingTypes.Any(EventsOf(CurrentTab).Contains))
				tabFunctionIcons[1] = "icon_clear";
			else
				tabFunctionIcons[1] = "icon_empty";
		}
		private void UpdateStatusText()
		{
			if(hoveringTab is Tabs t)
			{
				statusText = Localization.Get("tabs", t.ToString());
				return;
			}
			if(hoveringGlobalFunctionIndex >= 0)
			{
				statusText = Localization.Get("menu", globalFunctionIcons[hoveringGlobalFunctionIndex][5..]);
				return;
			}
			if(hoveringTabFunctionIndex >= 0)
			{
				statusText = Localization.Get("menu", tabFunctionIcons[hoveringTabFunctionIndex][5..]);
				return;
			}
			if (Hovering is EventType t1)
			{
				int count = (counterItems.TryGetValue(t1, out var item) ? item.Count : 0);
				if (count > 0)
					statusText = string.Format(Localization.Get("status_bar", "selected_events"),
						count,
						Localization.Get("events", t1.ToString()));
				else
					statusText = Localization.Get("events", t1.ToString());

			}
			else
			{
				int count = showingInfoBoxes.Sum(i => i.ResultItem.Count);
				if (count > 0)
					statusText = string.Format(Localization.Get("status_bar", "total_events"), count);
				else
					statusText = Localization.Get("status_bar", "no_event_selected");
			}
			return;
		}
		private void UpdateInterface(object? s, EventArgs e)
		{
			Invalidate();
		}
		private Tabs? GetTabArea(SKPointI p)
		{
			SKPointI sp = tabStartPoint;
			foreach (var tab in tabs)
			{
				var bound = new SKRectI
				{
					Location = sp,
					Size = new SKSizeI(tabWidth * ps, tabHeight * ps)
				};
				if (bound.Contains(p))
					return tab;
				sp.Y += bound.Height + gap * ps;
			}
			return null;
		}
		private int GetTabFunctionArea(SKPointI p)
		{
			SKPointI tabContentStart = pivotf with { X = pivotf.X + 2 * ps };
			for (int i = 0; i < tabFunctionIcons.Length; i++)
			{
				var bound = new SKRectI
				{
					Location = tabContentStart,
					Size = new SKSizeI(11 * ps, 6 * ps)
				};
				if (bound.Contains(p))
					return i;
				tabContentStart.Offset(0, 8 * ps);
			}
			return -1;
		}
		private int GetGlobalFunctionArea(SKPointI p)
		{
			SKPointI tabContentStart = pivot1 with { X = pivot1.X + 2 * ps };
			for (int i = 0; i < globalFunctionIcons.Length; i++)
			{
				var bound = new SKRectI
				{
					Location = tabContentStart,
					Size = new SKSizeI(11 * ps, 6 * ps)
				};
				if (bound.Contains(p))
					return i;
				tabContentStart.Offset(13 * ps, 0);
			}
			return -1;
		}
		private EventType? GetEventTypeArea(SKPointI p)
		{
			SKPointI start = pivot2;
			SKPointI current = start;
			SKRectI drawn = new();
			foreach (EventType type in EventsOf(CurrentTab))
			{
				SKRectI willdraw = type.GetEventIconBounds(current, isEnabled: eventTypesThatHasData.Contains(type), isActive: Hovering == type || showingTypes.Contains(type), scale: ps);
				if (willdraw.Right > Width)
				{
					current.X = start.X;
					current.Y += drawn.Height;
				}
				drawn = type.GetEventIconBounds(current, isEnabled: eventTypesThatHasData.Contains(type), isActive: Hovering == type || showingTypes.Contains(type), scale: ps);
				if (drawn.Contains(p))
					return type;
				current.X = drawn.Right;
			}
			return null;
		}
		private int GetEventInfoBoxArea(SKPointI p)
		{
			SKPoint current = pivot4;
			current.Y -= viewStartTop;
			if (p.Y < pivot4.Y)
				return -1;
			IEnumerable<EventInfoBox> list = muted ? showingInfoBoxes.OrderByDescending(i => i.ResultItem.Count) : [.. showingInfoBoxes];
			foreach (var box in list)
			{
				var bound = SKRect.Create(current.X, current.Y, Width - current.X - 2 * ps, (int)box.Height.Value * ps);
				if (bound.Contains(p))
					return showingInfoBoxes.IndexOf(box);
				current.Y += (int)box.Height.Value * ps;
			}
			return -1;
		}
	}
}
