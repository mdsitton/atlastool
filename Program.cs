using System;
using System.Drawing.Imaging;
using System.IO;
using AssetStudio;

namespace atlastool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Loading asset file {args[0]}");
            AssetsManager assetManager = new AssetsManager();
            Console.WriteLine(args[0]);

            if (File.Exists(args[0]))
            {
                assetManager.LoadFiles(new string[] { args[0] });
            }
            else if (Directory.Exists(args[0]))
            {
                assetManager.LoadFolder(args[0]);
            }
            else
            {
                Console.WriteLine("Error loading file");
                return;
            }

            foreach (var file in assetManager.assetsFileList)
            {
                foreach (var obj in file.Objects)
                {
                    switch(obj)
                    {
                        case Sprite spr:
                            foreach(var atlas in spr.m_AtlasTags)
                            {
                                if (atlas == "fiveFretAtlas")
                                {
                                    var image = SpriteHelper.GetImage(spr);
                                    image.Save($"{spr.m_Name}.png", ImageFormat.Png);
                                    Console.WriteLine($"fiveFretAtlas Sprite {spr.m_Name} saved");
                                }
                            }
                            break;
                        case SpriteAtlas spriteAtlas:
                            Console.WriteLine($"Atlas {spriteAtlas.m_Name}");
                            //spriteAtlas.
                            break;
                    }
                }
            }
        }
    }
}
