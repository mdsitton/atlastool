using AssetStudio;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static SwapData PreparSwapData(Bitmap atlas, Bitmap replaceImage, Sprite sprite)
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
                replaceImage.RotateFlip(RotateFlipType.RotateNoneFlipY);

                if (settingsRaw.packed == 1)
                {
                    //RotateAndFlip
                    switch (settingsRaw.packingRotation)
                    {
                        case SpritePackingRotation.kSPRFlipHorizontal:
                            replaceImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            break;
                        case SpritePackingRotation.kSPRFlipVertical:
                            replaceImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                            break;
                        case SpritePackingRotation.kSPRRotate180:
                            replaceImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        case SpritePackingRotation.kSPRRotate90:
                            replaceImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
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
        public Bitmap replaceImage;
        public Sprite sprite;
        public Rectf textureRect;
        public Rectangle textureRect2;
        public Rectangle destRect;
        public Vector2 textureRectOffset;
        public SpriteSettings settingsRaw;
    }

    public static Bitmap ClearRect(Bitmap atlas, Rectangle rect)
    {
        Rectangle fullRect = new Rectangle(0, 0, atlas.Width, atlas.Height);
        Region region = new Region(fullRect);
        GraphicsPath path = new GraphicsPath();
        path.AddRectangle(rect);
        region.Exclude(path);
        Bitmap bm = new Bitmap(atlas);

        using (Graphics gr = Graphics.FromImage(bm))
        {
            gr.Clear(System.Drawing.Color.Transparent);

            // Fill the region.
            gr.SetClip(region, CombineMode.Replace);
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            using (TextureBrush brush = new TextureBrush(atlas, fullRect))
            {
                gr.FillRectangle(brush, fullRect);
            }
        }
        return bm;
    }

    public static Bitmap ProcessSwaps(Bitmap atlas, SwapData[] swaps)
    {
        foreach (var swap in swaps)
        {
            atlas = ClearRect(atlas, swap.textureRect2);
            using (var graphic = Graphics.FromImage(atlas))
            {
                graphic.DrawImage(swap.replaceImage, swap.textureRect2, swap.destRect, GraphicsUnit.Pixel);
            }
        }
        return atlas;
    }
}
