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

static class BitmapUpdate
{
    public static string GetTextureName(Sprite spr)
    {
        if (spr.m_SpriteAtlas != null && spr.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
        {
            if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var texture))
            {
                return texture.m_Name;
            }
        }
        return String.Empty;

    }

    public static SwapData PrepareSwapData(Image<Bgra32> atlas, Image<Bgra32> replaceImage, Sprite sprite)
    {
        if (sprite.m_SpriteAtlas != null && sprite.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
        {
            if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(sprite.m_RenderDataKey, out var spriteAtlasData))
            {
                var settingsRaw = spriteAtlasData.settingsRaw;
                var textureRect = spriteAtlasData.textureRect;

                // Verify source and updated texture are the same size
                if (replaceImage.Width != textureRect.width || replaceImage.Height != textureRect.height)
                {
                    return null;
                }

                var rectf = new RectangleF(textureRect.x, textureRect.y, textureRect.width, textureRect.height);
                var rect = Rectangle.Round(rectf);
                if (rect.Width == 0)
                {
                    rect.Width = 1;
                }
                if (rect.Height == 0)
                {
                    rect.Height = 1;
                }
                var destRect = new Rectangle(0, 0, rect.Width, rect.Height);
                replaceImage.Mutate((x) => x.Flip(FlipMode.Vertical));

                if (settingsRaw.packed == 1)
                {
                    //RotateAndFlip
                    switch (settingsRaw.packingRotation)
                    {
                        case SpritePackingRotation.kSPRFlipHorizontal:
                            replaceImage.Mutate((x) => x.Flip(FlipMode.Horizontal));
                            break;
                        case SpritePackingRotation.kSPRFlipVertical:
                            replaceImage.Mutate((x) => x.Flip(FlipMode.Vertical));
                            break;
                        case SpritePackingRotation.kSPRRotate180:
                            replaceImage.Mutate((x) => x.Rotate(RotateMode.Rotate180));
                            break;
                        case SpritePackingRotation.kSPRRotate90:
                            replaceImage.Mutate((x) => x.Rotate(RotateMode.Rotate90));
                            break;
                    }
                }

                return new SwapData()
                {
                    replaceImage = replaceImage,
                    sprite = sprite,
                    textureRect = textureRect,
                    textureRect2 = rect,
                    destRect = destRect,
                    textureRectOffset = spriteAtlasData.textureRectOffset,
                    settingsRaw = settingsRaw
                };
            }
        }
        return null;
    }

    public class SwapData
    {
        public Image<Bgra32> replaceImage;
        public Sprite sprite;
        public Rectf textureRect;
        public Rectangle textureRect2;
        public Rectangle destRect;
        public Vector2 textureRectOffset;
        public SpriteSettings settingsRaw;
    }

    public static void ClearRect(Image<Bgra32> atlas, Rectangle rect)
    {
        var graphicsOptions = new GraphicsOptions
        {
            AlphaCompositionMode = PixelAlphaCompositionMode.Clear
        };
        var options = new DrawingOptions
        {
            GraphicsOptions = graphicsOptions
        };
        atlas.Mutate(x => x.Fill(options, SixLabors.ImageSharp.Color.Transparent, rect));
    }

    public static void ReplaceRect(Image<Bgra32> atlas, Image<Bgra32> image, Rectangle rect)
    {
        var point = new Point(rect.Left, rect.Top);
        atlas.Mutate(x => x.DrawImage(image, point, PixelColorBlendingMode.Add, 1.0f));
    }

    public static void ProcessSwaps(Image<Bgra32> atlas, SwapData[] swaps)
    {
        foreach (var swap in swaps)
        {
            ClearRect(atlas, swap.textureRect2);
            ReplaceRect(atlas, swap.replaceImage, swap.destRect);
        }
    }
}
