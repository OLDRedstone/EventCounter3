using EvtCtr3.Assets;
using EvtCtr3.Core;
using EvtCtr3.Animation;
using RhythmBase.RhythmDoctor.Events;
using SkiaSharp;
using RhythmBase.Global.Components;

namespace EvtCtr3
{
	internal static class Extensions
	{
		internal static AnimationTimer GlobalAnimationTimer = new();
		public static void DrawSlice(this SKCanvas canvas, string src, SKRectI dest, int scale = 1)
		{
			if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
				return;

			if (info.IsNinePatch)
			{
				DrawNinePatch(canvas, AssetManager._assetFile, info, dest, null, scale);
			}
			else
			{
				canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, dest);
			}
		}
		public static void DrawSlice(this SKCanvas canvas, string src, SKRectI dest, SKColor replace, int scale = 1)
		{
			if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
				return;

			if (replace.Alpha == 0)
				return;

			float tr = replace.Red / 255f;
			float tg = replace.Green / 255f;
			float tb = replace.Blue / 255f;
			float ta = replace.Alpha / 255f;

			float[] colorMatrix =
			[
					0.2126f * tr, 0.7152f * tr, 0.0722f * tr, 0, 0,
								0.2126f * tg, 0.7152f * tg, 0.0722f * tg, 0, 0,
								0.2126f * tb, 0.7152f * tb, 0.0722f * tb, 0, 0,
								0,            0,            0,            ta, 0
			];

			using SKPaint paint = new()
			{
				ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix)
			};

			if (info.IsNinePatch)
			{
				DrawNinePatch(canvas, AssetManager._assetFile, info, dest, paint, scale);
			}
			else
			{
				canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, dest, paint);
			}
		}
		public static SKRectI DrawSlice(this SKCanvas canvas, string src, SKPointI dest, int scale = 1)
		{
			if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
				return default;

			SKRectI destRect = SKRectI.Create(dest, new(info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale));
			canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, destRect);

			return SKRectI.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);
		}
		public static SKRectI DrawSlice(this SKCanvas canvas, string src, SKPointI dest, SKColor replace, int scale = 1)
		{
			if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
				return default;

			SKRectI destRect = SKRectI.Create(dest, new(info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale));

			// 不绘制完全透明的目标色，仍返回期望的 bounds
			if (replace.Alpha == 0)
				return SKRectI.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);

			float tr = replace.Red / 255f;
			float tg = replace.Green / 255f;
			float tb = replace.Blue / 255f;
			float ta = replace.Alpha / 255f;

			float[] colorMatrix =
			[
					0.2126f * tr, 0.7152f * tr, 0.0722f * tr, 0, 0,
								0.2126f * tg, 0.7152f * tg, 0.0722f * tg, 0, 0,
								0.2126f * tb, 0.7152f * tb, 0.0722f * tb, 0, 0,
								0,            0,            0,            ta, 0
			];

			using SKPaint paint = new()
			{
				ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix)
			};
			canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, destRect, paint);

			return SKRectI.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);
		}
		private static readonly ReadOnlyEnumCollection<EventType> _beatTypes =new(
			2, 
			EventType.AddFreeTimeBeat,
			EventType.PulseFreeTimeBeat
		);
		public static SKRectI DrawEventIcon(this SKCanvas canvas, Tabs tab, EventType type, SKPointI dest, bool isActive = false, bool isEnabled = true, int scale = 1)
		{
			string postfix2 = (isActive ? "a" : "i");
			string key1 = $"event_{type}_i";
			if (isActive || !AssetManager._slices.TryGetValue(key1, out SliceInfo info))
			{
				string key2 = $"event_{type}";
				if (!AssetManager._slices.TryGetValue(key2, out info))
				{
					if (!AssetManager._slices.TryGetValue($"event_Unknown", out info))
					{
						throw new NotImplementedException();
					}
				}
			}
			float effectiveScale = float.Max(1, (scale / float.Max(1, info.Scale)));
			SKPoint pivot = new(info.Pivot.X * effectiveScale, info.Pivot.Y * effectiveScale);
			SKRect destRect = SKRect.Create(
				dest - new SKPoint(pivot.X, pivot.Y),
				new(info.Bounds.Size.Width * effectiveScale,
				info.Bounds.Size.Height * effectiveScale));
			SKRect destRect2 = destRect;
			if (info.HasSpace)
			{
				destRect.Left += pivot.X;
				destRect.Top += pivot.Y;
				destRect.Right -= destRect.Height / 2;
			}
			string tabName = _beatTypes.Contains(type) ? "Beats" : isEnabled ? tab.ToString() : "disabled";
			if (isEnabled)
			{
				canvas.DrawSlice($"event_{tab}_{postfix2}", SKRectI.Round(destRect), scale);
			}
			else
			{
				canvas.DrawSlice($"event_disabled_{postfix2}", SKRectI.Round(destRect), scale);
			}
			canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, destRect2);
			return SKRectI.Round(destRect);
		}
		public static SKRectI GetEventIconBounds(this EventType type, SKPointI dest, bool isActive = false, bool isEnabled = true, int scale = 1)
		{
			string key1 = $"event_{type}_i";
			if (isActive || !AssetManager._slices.TryGetValue(key1, out SliceInfo info))
			{
				string key2 = $"event_{type}";
				if (!AssetManager._slices.TryGetValue(key2, out info))
				{
					if (!AssetManager._slices.TryGetValue($"event_Unknown", out info))
					{
						throw new NotImplementedException();
					}
				}
			}
			float effectiveScale = float.Max(1, scale / float.Max(1, info.Scale));
			SKPoint pivot = new(info.Pivot.X * effectiveScale, info.Pivot.Y * effectiveScale);
			SKRect destRect = SKRect.Create(
				dest - new SKPoint(pivot.X,
				pivot.Y),
				new(info.Bounds.Size.Width * effectiveScale,
				info.Bounds.Size.Height * effectiveScale));
			if (info.HasSpace)
			{
				destRect.Left += pivot.X;
				destRect.Top += pivot.Y;
				destRect.Right -= destRect.Height / 2;
			}
			return SKRectI.Round(destRect);
		}
		private static void DrawNinePatch(SKCanvas canvas, SKBitmap srcBitmap, SliceInfo info, SKRectI destRect, SKPaint? paint, int scale)
		{
			int sx0 = info.Bounds.Left;
			int sx3 = info.Bounds.Right;
			int sy0 = info.Bounds.Top;
			int sy3 = info.Bounds.Bottom;

			int sx1 = sx0 + info.Center.Left;
			int sx2 = sx0 + info.Center.Right;
			int sy1 = sy0 + info.Center.Top;
			int sy2 = sy0 + info.Center.Bottom;

			int swLeft = sx1 - sx0;
			int swCenter = sx2 - sx1;
			int swRight = sx3 - sx2;

			int shTop = sy1 - sy0;
			int shCenter = sy2 - sy1;
			int shBottom = sy3 - sy2;

			int dwLeft = swLeft * Math.Max(1, scale);
			int dwRight = swRight * Math.Max(1, scale);
			int dwCenter = destRect.Width - dwLeft - dwRight;
			if (dwCenter < 0)
			{
				float scaleX = (float)destRect.Width / Math.Max(1, swLeft + swRight);
				dwLeft = Math.Max(0, (int)Math.Round(swLeft * scaleX));
				dwRight = Math.Max(0, destRect.Width - dwLeft);
				dwCenter = 0;
			}

			int dhTop = shTop * Math.Max(1, scale);
			int dhBottom = shBottom * Math.Max(1, scale);
			int dhCenter = destRect.Height - dhTop - dhBottom;
			if (dhCenter < 0)
			{
				float scaleY = (float)destRect.Height / Math.Max(1, shTop + shBottom);
				dhTop = Math.Max(0, (int)Math.Round(shTop * scaleY));
				dhBottom = Math.Max(0, destRect.Height - dhTop);
				dhCenter = 0;
			}

			int[] srcXs = [sx0, sx1, sx2, sx3];
			int[] srcYs = [sy0, sy1, sy2, sy3];

			int dx0 = destRect.Left;
			int dx1 = dx0 + dwLeft;
			int dx2 = dx1 + dwCenter;
			int dx3 = destRect.Right;

			int dy0 = destRect.Top;
			int dy1 = dy0 + dhTop;
			int dy2 = dy1 + dhCenter;
			int dy3 = destRect.Bottom;

			int[] dstXs = [dx0, dx1, dx2, dx3];
			int[] dstYs = [dy0, dy1, dy2, dy3];

			for (int row = 0; row < 3; row++)
			{
				for (int col = 0; col < 3; col++)
				{
					int sLeft = srcXs[col];
					int sTop = srcYs[row];
					int sRight = srcXs[col + 1];
					int sBottom = srcYs[row + 1];
					int sW = sRight - sLeft;
					int sH = sBottom - sTop;
					if (sW <= 0 || sH <= 0)
						continue;

					int dLeft = dstXs[col];
					int dTop = dstYs[row];
					int dRight = dstXs[col + 1];
					int dBottom = dstYs[row + 1];
					int dW = dRight - dLeft;
					int dH = dBottom - dTop;
					if (dW <= 0 || dH <= 0)
						continue;

					var srcRect = SKRectI.Create(sLeft, sTop, sW, sH);
					var dstRect = SKRectI.Create(dLeft, dTop, dW, dH);

					canvas.DrawBitmap(srcBitmap, srcRect, dstRect, paint);
				}
			}
		}

		internal static readonly ReadOnlyEnumCollection<EventType> soundsTypes = new(2,
				EventType.NarrateRowInfo,
				EventType.PlaySong,
				EventType.PlaySound,
				EventType.ReadNarration,
				EventType.SayReadyGetSetGo,
				EventType.SetBeatSound,
				EventType.SetBeatsPerMinute,
				EventType.SetClapSounds,
				EventType.SetCountingSound,
				EventType.SetCrotchetsPerBar,
				EventType.SetGameSound,
				EventType.SetHeartExplodeInterval,
				EventType.SetHeartExplodeVolume
				);
		internal static readonly ReadOnlyEnumCollection<EventType> rowsTypes = new(2,
				EventType.AddClassicBeat,
				EventType.AddFreeTimeBeat,
				EventType.AddOneshotBeat,
				EventType.PulseFreeTimeBeat,
				EventType.SetOneshotWave,
				EventType.SetRowXs
				);
		internal static readonly ReadOnlyEnumCollection<EventType> actionsTypes = new(2,
				EventType.AdvanceText,
				EventType.BassDrop,
				EventType.CallCustomMethod,
				// EventType.ChangeCharacter,
				EventType.ChangePlayersRows,
				EventType.CustomFlash,
				EventType.FinishLevel,
				EventType.Flash,
				EventType.FlipScreen,
				EventType.FloatingText,
				EventType.HideRow,
				EventType.InvertColors,
				EventType.MoveCamera,
				EventType.MoveRow,
				EventType.PaintHands,
				EventType.PlayExpression,
				EventType.PulseCamera,
				EventType.ReorderRow,
				EventType.SetBackgroundColor,
				EventType.SetForeground,
				EventType.SetHandOwner,
				EventType.SetPlayStyle,
				EventType.SetSpeed,
				EventType.SetTheme,
				EventType.SetVFXPreset,
				EventType.ShakeScreen,
				EventType.ShakeScreenCustom,
				EventType.ShowDialogue,
				EventType.ShowHands,
				EventType.ShowStatusSign,
				// EventType.ShowSubdivisionsRows,
				EventType.Stutter,
				EventType.TagAction,
				EventType.TextExplosion,
				EventType.TintRows
				);
		internal static readonly ReadOnlyEnumCollection<EventType> roomsTypes = new(2,
				EventType.FadeRoom,
				EventType.MaskRoom,
				EventType.MoveRoom,
				EventType.ReorderRooms,
				EventType.SetRoomContentMode,
				EventType.SetRoomPerspective,
				EventType.ShowRooms
				);
		internal static readonly ReadOnlyEnumCollection<EventType> decorationTypes = new(2,
				EventType.Blend,
				EventType.Move,
				EventType.PlayAnimation,
				EventType.ReorderSprite,
				EventType.SetVisible,
				EventType.Tile,
				EventType.Tint
				);
		internal static readonly ReadOnlyEnumCollection<EventType> windowsTypes = new(2,
				EventType.NewWindowDance,
				EventType.SetWindowContent,
				EventType.WindowResize
				);
		internal static readonly ReadOnlyEnumCollection<EventType> unknownTypes = new(2,
				EventType.Comment
				);
		internal static readonly ReadOnlyEnumCollection<EventType> unSupportedType = new(2,
				EventType.ChangeCharacter,
				EventType.ShowSubdivisionsRows,
				EventType.MacroEvent,
				EventType.ForwardEvent,
				EventType.ForwardRowEvent,
				EventType.ForwardDecorationEvent
				);
		internal static Tabs TabOf(EventType type)
		{
			if (soundsTypes.Contains(type))
				return Tabs.Sounds;
			if (rowsTypes.Contains(type))
				return Tabs.Rows;
			if (actionsTypes.Contains(type))
				return Tabs.Actions;
			if (roomsTypes.Contains(type))
				return Tabs.Rooms;
			if (decorationTypes.Contains(type))
				return Tabs.Decorations;
			if (windowsTypes.Contains(type))
				return Tabs.Windows;
			return Tabs.Unknown;
		}
		internal static EventType[] EventsOf(Tabs tab)
		{
			return tab switch
			{
				Tabs.Sounds => [.. soundsTypes],
				Tabs.Rows => [.. rowsTypes],
				Tabs.Actions => [.. actionsTypes],
				Tabs.Rooms => [.. roomsTypes],
				Tabs.Decorations => [.. decorationTypes],
				Tabs.Windows => [.. windowsTypes],
				Tabs.Unknown => [.. unknownTypes],
				_ => []
			};
		}
	}
}