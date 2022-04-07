=== Clone Hero Texture Atlas Tool ===
A tool for modifying game textures in Clone Hero


Usage:
This program requires you to invoke it and pass commands to it via a command line.

Arguments:
- -h / --help
  Displays information about available commands.
  
- -x -i <path1> -o <path2> / --export --input <path1> --output <path2>
  Gets the sprite atlas from the input path and exports it and individual sprites to the output path.
  Input directory can be either the data.unity3d file, or the `Clone Hero_Data` folder.
  A new folder named after the game version will be created within the output path. If no path is specified, it defaults to atlastool's folder.
  
- -c -i <path> / --combine --input <path>
  Combines the sprites from the `sprites` folder in the input folder into a new atlas image, which gets placed into the input folder.

Examples:
- Extracting:
    -x -i C:\Games\Clone Hero\Clone Hero_Data\unity.data3d -o .\extracted
    -x -i %APPDATA%\Clone Hero Launcher\gameFiles\Clone Hero_Data
  
- Combining:
    -c -i .\v.23.2.2

Note:
For now, the new atlas will need to be added in using Unity Asset Bundle Extractor Avalonia: https://github.com/nesrak1/UABEA/releases

1. Go to File > Open and select the correct data file:
   - For v.23.2.2 or earlier, load the `data.unity3d` file. For the PTB, load the `resources.assets` file.
2. Find the texture beginning with `sactx` and select it, then click Plugins and select `Edit texture`.
3. In the menu, click the Load button to select the new atlas image, and set the texture format setting to RGBA32 to prevent image quality loss.
4. Click Save.

License:
This program is licensed under the MIT license. See license.txt for details.
See thirdparty.txt for licenses of third-party assets.
