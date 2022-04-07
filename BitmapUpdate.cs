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

    public static void ProcessChanges(Image<Bgra32> atlas, Image<Bgra32> replaceImage, Sprite sprite)
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
                    return;
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
                var destRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
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

                var point = new Point(destRect.Left, destRect.Top);
                atlas.Mutate(x => x.DrawImage(replaceImage, point, PixelColorBlendingMode.Add, PixelAlphaCompositionMode.Src, 1.0f));
            }
        }
    }
}
