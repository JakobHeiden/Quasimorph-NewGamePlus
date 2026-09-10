using System.IO;
using System.Reflection;
using UnityEngine;

namespace NewGamePlus
{
    /// <summary>
    ///     Turns PNGs embedded in the mod assembly into sprites. The csproj embeds everything under assets/ by
    ///     bare file name, so the images ship inside the DLL and cannot go missing from the mod folder. Scale and
    ///     pivot are copied from a vanilla sprite doing the same job, because the game's pixels-per-unit lives in
    ///     its asset data rather than its code, and a mismatch draws the sprite at the wrong size on the map.
    /// </summary>
    public static class EmbeddedSprites
    {
        public static Sprite Load(string fileName, Sprite scaleTemplate)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(ReadResource(fileName)))
                throw new InvalidDataException($"Embedded resource '{fileName}' is not a readable image.");

            texture.name = Path.GetFileNameWithoutExtension(fileName);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var templateSize = scaleTemplate.rect.size;
            var normalizedPivot = new Vector2(scaleTemplate.pivot.x / templateSize.x, scaleTemplate.pivot.y / templateSize.y);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), normalizedPivot,
                scaleTemplate.pixelsPerUnit);
            sprite.name = texture.name;
            return sprite;
        }

        private static byte[] ReadResource(string fileName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"No embedded resource '{fileName}'. Is it in the assets folder?");

                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }
    }
}
