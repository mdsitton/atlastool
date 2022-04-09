using AssetStudio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.IO;

namespace atlascore;

static class ImageOps
{
    private static IImageProcessingContext GetApplyOrientation(this IImageProcessingContext context, Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Normal => context.Flip(FlipMode.Vertical),
            Orientation.FlipHorizontal => context.Flip(FlipMode.Vertical).Flip(FlipMode.Horizontal),
            Orientation.FlipVertical => context, // do nothing
            Orientation.Rotate180 => context.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate180),
            Orientation.Rotate90 => context.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90),
            _ => context.Flip(FlipMode.Vertical),
        };
    }

    private static IImageProcessingContext GetUnapplyOrientation(this IImageProcessingContext context, Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Normal => context.Flip(FlipMode.Vertical),
            Orientation.FlipHorizontal => context.Flip(FlipMode.Vertical).Flip(FlipMode.Horizontal),
            Orientation.FlipVertical => context, // do nothing
            Orientation.Rotate180 => context.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate180),
            Orientation.Rotate90 => context.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate270),
            _ => context.Flip(FlipMode.Vertical),
        };
    }

    public static void ReplaceImage(Image<Bgra32> atlas, SpriteData spriteData)
    {
        if (spriteData.Texture == null)
        {
            return;
        }

        var textureRect = spriteData.Rect;

        // Verify source and updated texture are the same size
        if (spriteData.Texture.Width != textureRect.Width || spriteData.Texture.Height != textureRect.Height)
        {
            return;
        }

        Rectangle destRect = AssetStudioUtil.ConvertRectf(textureRect, atlas.Bounds());
        Image<Bgra32> replaceImage = spriteData.Texture.Clone((x) => x.GetApplyOrientation(spriteData.Orientation));

        var point = new Point(destRect.Left, destRect.Top);
        atlas.Mutate(x => x.DrawImage(replaceImage, point, PixelColorBlendingMode.Add, PixelAlphaCompositionMode.Src, 1.0f));
    }

    public static Image<Bgra32> CutImage(Image<Bgra32> atlas, SpriteData spriteData)
    {
        Rectangle destRect = AssetStudioUtil.ConvertRectf(spriteData.Rect, atlas.Bounds());
        return atlas.Clone(x => x.Crop(destRect).GetUnapplyOrientation(spriteData.Orientation));
    }

}
