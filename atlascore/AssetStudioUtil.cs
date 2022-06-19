using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetStudio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace atlascore;

public static class AssetStudioUtil
{
    public static AssetsManager LoadAssetManager(string path)
    {
        AssetsManager assetManager = new();

        if (File.Exists(path))
        {
            assetManager.LoadFiles(path);
        }
        else if (Directory.Exists(path))
        {
            assetManager.LoadFolder(path);
        }
        else
        {
            throw new ArgumentException($"Failed to load asset data at specified path: {path}");
        }
        return assetManager;
    }

    public static IEnumerable<T> EnumerateAssets<T>(this List<SerializedFile> assetList) where T : AssetStudio.Object
    {
        foreach (var file in assetList)
        {
            foreach (var obj in file.Objects)
            {
                if (obj is T type)
                {
                    yield return type;
                }
            }
        }

    }

    public static string GetGameVersion(this List<SerializedFile> assetList)
    {
        foreach (var text in assetList.EnumerateAssets<TextAsset>())
        {
            if (text.m_Name == "version")
            {
                return System.Text.Encoding.UTF8.GetString(text.m_Script);
            }
        }
        return String.Empty;
    }

    public static IEnumerable<Sprite> EnumerateAtlasSprites(this SpriteAtlas atlas)
    {
        foreach (var sprite in atlas.m_PackedSprites)
        {
            if (sprite.TryGet(out Sprite spriteObj))
            {
                yield return spriteObj;
            }
        }
    }

    public static Vector2? GetSpriteTextureRectOffset(this Sprite spr)
    {
        if (spr.m_SpriteAtlas != null && spr.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
        {
            if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var spriteAtlasData))
            {
                return spriteAtlasData.textureRectOffset;
            }
        }
        else if (spr.m_RD != null)
        {
            return spr.m_RD.textureRectOffset;
        }

        return null;
    }

    public static bool TryGetSpriteTexture(this Sprite spr, out Texture2D? texOut, out int fileID)
    {
        if (spr.m_SpriteAtlas != null && spr.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
        {
            if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var texture2D))
            {
                texOut = texture2D;
                fileID = spriteAtlasData.texture.m_FileID;
                return true;
            }
        }
        else if (spr.m_RD != null && spr.m_RD.texture.TryGet(out var texture2D))
        {
            texOut = texture2D;
            fileID = spr.m_RD.texture.m_FileID;
            return true;
        }

        texOut = null;
        fileID = -1;
        return false;
    }

    public static bool TryGetSpriteTexture(this Sprite spr, out Texture2D? texOut)
    {
        if (spr.m_SpriteAtlas != null && spr.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
        {
            if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var texture2D))
            {
                texOut = texture2D;
                return true;
            }
        }
        else if (spr.m_RD != null && spr.m_RD.texture.TryGet(out var texture2D))
        {
            texOut = texture2D;
            return true;
        }

        texOut = null;
        return false;
    }

    public static bool TryGetSpriteAtlasData(this SpriteAtlas atlas, Sprite sprite, out SpriteAtlasData? data)
    {
        if (atlas.m_RenderDataMap.TryGetValue(sprite.m_RenderDataKey, out var spriteAtlasData))
        {
            data = spriteAtlasData;
            return true;
        }
        data = null;
        return false;
    }

    public static RectData GetRectData(this SpriteAtlas atlas, Sprite sprite)
    {
        if (atlas.TryGetSpriteAtlasData(sprite, out var spriteAtlasData))
        {
            var texRect = spriteAtlasData!.textureRect;
            return new RectData(texRect.x, texRect.y, texRect.width, texRect.height);
        }
        return new RectData(0, 0, 1, 1);
    }

    public static Orientation GetOrientation(this SpriteAtlas atlas, Sprite sprite)
    {
        if (atlas.TryGetSpriteAtlasData(sprite, out var spriteAtlasData))
        {
            var settingsRaw = spriteAtlasData!.settingsRaw;

            if (settingsRaw.packed == 1)
            {
                //RotateAndFlip
                switch (settingsRaw.packingRotation)
                {
                    case SpritePackingRotation.FlipHorizontal:
                        return Orientation.FlipHorizontal;
                    case SpritePackingRotation.FlipVertical:
                        return Orientation.FlipVertical;
                    case SpritePackingRotation.Rotate180:
                        return Orientation.Rotate180;
                    case SpritePackingRotation.Rotate90:
                        return Orientation.Rotate90;
                }
            }
        }
        return Orientation.Normal;
    }

    public static IPathCollection ConvertSpriteTrianglesToPath(this Sprite spr)
    {
        Vector2 textureRectOffset = GetSpriteTextureRectOffset(spr) ?? Vector2.Zero;

        var triangles = GetTriangles(spr.m_RD);
        var polygons = triangles.Select(x => new Polygon(new LinearLineSegment(x.Select(y => new PointF(y.X, y.Y)).ToArray()))).ToArray();
        IPathCollection path = new PathCollection(polygons);
        var matrix = System.Numerics.Matrix3x2.CreateScale(spr.m_PixelsToUnits);
        matrix *= System.Numerics.Matrix3x2.CreateTranslation(spr.m_Rect.width * spr.m_Pivot.X - textureRectOffset.X, spr.m_Rect.height * spr.m_Pivot.Y - textureRectOffset.Y);
        return path.Transform(matrix);
    }

    private static Vector2[][] GetTriangles(SpriteRenderData m_RD)
    {
        if (m_RD.vertices != null) //5.6 down
        {
            var vertices = m_RD.vertices.Select(x => (Vector2)x.pos).ToArray();
            var triangleCount = m_RD.indices.Length / 3;
            var triangles = new Vector2[triangleCount][];
            for (int i = 0; i < triangleCount; i++)
            {
                var first = m_RD.indices[i * 3];
                var second = m_RD.indices[i * 3 + 1];
                var third = m_RD.indices[i * 3 + 2];
                var triangle = new[] { vertices[first], vertices[second], vertices[third] };
                triangles[i] = triangle;
            }
            return triangles;
        }
        else //5.6 and up
        {
            var triangles = new List<Vector2[]>();
            var m_VertexData = m_RD.m_VertexData;
            var m_Channel = m_VertexData.m_Channels[0]; //kShaderChannelVertex
            var m_Stream = m_VertexData.m_Streams[m_Channel.stream];
            using (var vertexReader = new BinaryReader(new MemoryStream(m_VertexData.m_DataSize)))
            {
                using (var indexReader = new BinaryReader(new MemoryStream(m_RD.m_IndexBuffer)))
                {
                    foreach (var subMesh in m_RD.m_SubMeshes)
                    {
                        vertexReader.BaseStream.Position = m_Stream.offset + subMesh.firstVertex * m_Stream.stride + m_Channel.offset;

                        var vertices = new Vector2[subMesh.vertexCount];
                        for (int v = 0; v < subMesh.vertexCount; v++)
                        {
                            vertices[v] = vertexReader.ReadVector3();
                            vertexReader.BaseStream.Position += m_Stream.stride - 12;
                        }

                        indexReader.BaseStream.Position = subMesh.firstByte;

                        var triangleCount = subMesh.indexCount / 3u;
                        for (int i = 0; i < triangleCount; i++)
                        {
                            var first = indexReader.ReadUInt16() - subMesh.firstVertex;
                            var second = indexReader.ReadUInt16() - subMesh.firstVertex;
                            var third = indexReader.ReadUInt16() - subMesh.firstVertex;
                            var triangle = new[] { vertices[first], vertices[second], vertices[third] };
                            triangles.Add(triangle);
                        }
                    }
                }
            }
            return triangles.ToArray();
        }
    }

    public static Rectangle ConvertRectf(RectData rectfIn, Rectangle bounds)
    {
        var rectX = (int)Math.Floor(rectfIn.X);
        var rectY = (int)Math.Floor(rectfIn.Y);
        var rectRight = (int)Math.Ceiling(rectfIn.X + rectfIn.Width);
        var rectBottom = (int)Math.Ceiling(rectfIn.Y + rectfIn.Height);
        rectRight = Math.Min(rectRight, bounds.Width);
        rectBottom = Math.Min(rectBottom, bounds.Height);
        return new Rectangle(rectX, rectY, rectRight - rectX, rectBottom - rectY);
    }

    public static Rectangle ConvertRectf(Rectf rectfIn, Rectangle bounds)
    {
        var rectX = (int)Math.Floor(rectfIn.x);
        var rectY = (int)Math.Floor(rectfIn.y);
        var rectRight = (int)Math.Ceiling(rectfIn.x + rectfIn.width);
        var rectBottom = (int)Math.Ceiling(rectfIn.y + rectfIn.height);
        rectRight = Math.Min(rectRight, bounds.Width);
        rectBottom = Math.Min(rectBottom, bounds.Height);
        return new Rectangle(rectX, rectY, rectRight - rectX, rectBottom - rectY);
    }

}