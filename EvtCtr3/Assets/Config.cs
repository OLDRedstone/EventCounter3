using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace EvtCtr3.Assets;
internal class Config
{
	public CountingMethod CountingMethod { get; set; } = CountingMethod.Simply;
	public int PixelSize { get; set; } = 2;
	public string FontFamily { get; set; } = "Arial";
	public int Theme { get; set; }
	public string Language { get; set; } = "en-us";
}
