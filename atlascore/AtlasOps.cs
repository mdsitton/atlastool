using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace atlascore;

public class AtlasOps
{
    public static void SliceSprites(AtlasData atlasData)
    {
        foreach (var sprite in atlasData.Sprites)
        {
            var texture = atlasData.Textures[sprite.SourceTexturePathID];
            sprite.Texture = ImageOps.CutImage(texture.Texture, sprite);
        }
    }

    public static void MergeSprites(AtlasData atlasData)
    {
        foreach (var sprite in atlasData.Sprites)
        {
            var texture = atlasData.Textures[sprite.SourceTexturePathID];
            ImageOps.ReplaceImage(texture.Texture, sprite);
        }
    }

    public static void SaveTextures(AtlasData atlasData, string outputDir)
    {
        foreach (var tex in atlasData.Textures.Values)
        {
            var imgPath = Path.Combine(outputDir, $"{tex.Name}-{tex.PathID}.png");
            try
            {
                var texOut = tex.Texture.Clone((x) => x.Flip(FlipMode.Vertical));
                texOut.SaveAsPng(imgPath);
            }
            catch
            {
                Console.WriteLine($"error saving texture: {tex.Name}-{tex.PathID}.png");
                continue;
            }
        }
    }

    public static void LoadTextures(AtlasData atlasData, string outputDir)
    {
        foreach (var tex in atlasData.Textures.Values)
        {
            var imgPath = Path.Combine(outputDir, $"{tex.Name}-{tex.PathID}.png");
            try
            {
                tex.Texture = Image.Load<Bgra32>(imgPath);
            }
            catch
            {
                Console.WriteLine($"error loading texture: {tex.Name}-{tex.PathID}.png");
            }
        }
    }
}