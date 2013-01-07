using System;

namespace TerrainExplorer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (TerrainExplorer game = new TerrainExplorer())
            {
                game.Run();
            }
        }
    }
}

