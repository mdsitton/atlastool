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
    public static void OverwriteSprite(Bitmap atlas, Bitmap replaceImage, Sprite sprite)
    {
        if (sprite.m_SpriteAtlas != null && sprite.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
        {
            if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(sprite.m_RenderDataKey, out var spriteAtlasData))
            {
                SwapImage(atlas, replaceImage, sprite, spriteAtlasData.textureRect, spriteAtlasData.textureRectOffset, spriteAtlasData.settingsRaw);
            }
        }
    }
    private static void SwapImage(Bitmap atlas, Bitmap replaceImage, Sprite sprite, Rectf textureRect, Vector2 textureRectOffset, SpriteSettings settingsRaw)
    {
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

        using (var graphic = Graphics.FromImage(atlas))
        {
            graphic.FillRectangle(Brushes.Transparent, rect);
            graphic.DrawImage(replaceImage, rect, destRect, GraphicsUnit.Pixel);
        }

        //Tight
        //if (settingsRaw.packingMode == SpritePackingMode.kSPMTight)
        //{
        //    try
        //    {
        //        var triangles = SpriteHelper.GetTriangles(sprite.m_RD);
        //        var points = triangles.Select(x => x.Select(y => new PointF(y.X, y.Y)).ToArray());
        //        using (var path = new GraphicsPath())
        //        {
        //            foreach (var p in points)
        //            {
        //                path.AddPolygon(p);
        //            }
        //            using (var matr = new Matrix())
        //            {
        //                var version = sprite.version;
        //                if (version[0] < 5
        //                    || (version[0] == 5 && version[1] < 4)
        //                    || (version[0] == 5 && version[1] == 4 && version[2] <= 1)) //5.4.1p3 down
        //                {
        //                    matr.Translate(sprite.m_Rect.width * 0.5f - textureRectOffset.X, sprite.m_Rect.height * 0.5f - textureRectOffset.Y);
        //                }
        //                else
        //                {
        //                    matr.Translate(sprite.m_Rect.width * sprite.m_Pivot.X - textureRectOffset.X, sprite.m_Rect.height * sprite.m_Pivot.Y - textureRectOffset.Y);
        //                }
        //                matr.Scale(sprite.m_PixelsToUnits, sprite.m_PixelsToUnits);
        //                path.Transform(matr);
        //                using (var graphic = Graphics.FromImage(atlas))
        //                {
        //                    replaceImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
        //                    using (var brush = new TextureBrush(replaceImage))
        //                    {
        //                        graphic.FillPath(brush, path);
        //                        return;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch
        //    {
        //    }
        //}
    }
}
