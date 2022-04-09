using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace atlascore;


public enum Orientation
{
    Normal,
    FlipHorizontal,
    FlipVertical,
    Rotate180,
    Rotate90,
}

public class RectData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public RectData(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }
}

public class SpriteData
{
    public int PathID { get; set; }
    public string Name { get; set; }
    public Orientation Orientation { get; set; }
    public RectData Rect { get; set; }
    public int SourceTexturePathID { get; set; }

    public Image<Bgra32>? Texture;

    public SpriteData(int pathID, string name, Orientation orientation, RectData rect, int sourceTexturePathID)
    {
        PathID = pathID;
        Name = name;
        Orientation = orientation;
        Rect = rect;
        SourceTexturePathID = sourceTexturePathID;
    }
}