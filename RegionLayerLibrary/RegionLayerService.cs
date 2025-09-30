using RegionLayerLibrary.Models;
using System;

namespace RegionLayerLibrary
{
    public class RegionLayerService
    {
        private readonly RegionTile[,] _regionMap;
        private readonly int _mapWidth;
        private readonly int _mapHeight;

        public RegionLayerService(int mapWidth, int mapHeight, int regionSize)
        {
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _regionMap = new RegionTile[mapWidth, mapHeight];
        }

        public void GenerateRegions(IReadOnlyList<string> regionNames = null)
        {
            int regionsX = (int)Math.Ceiling((double)_mapWidth / _regionSize);
            int regionsY = (int)Math.Ceiling((double)_mapHeight / _regionSize);
            uint regionId = 1;

            for (int regionY = 0; regionY < regionsY; regionY++)
            {
                for (int regionX = 0; regionX < regionsX; regionX++)
                {
                    int startX = regionX * _regionSize;
                    int startY = regionY * _regionSize;
                    int width = Math.Min(_regionSize, _mapWidth - startX);
                    int height = Math.Min(_regionSize, _mapHeight - startY);

                    string name = regionNames?[random.Next(regionNames.Count)]
                        ?? $"Region_{regionId}";

                    var region = new RegionTile(regionId, name, startX, startY, width, height);

                    // Заполняем карту - структуры копируются, но это дешево
                    for (int y = startY; y < startY + height; y++)
                    {
                        for (int x = startX; x < startX + width; x++)
                        {
                            _regionMap[x, y] = region;
                        }
                    }

                    regionId++;
                }
            }
        }

        // O(1) доступ - возвращаем структуру (копия, но небольшая)
        public RegionTile GetRegionAt(int x, int y) => _regionMap[x, y];

        // O(1) доступ к ID без копирования всей структуры
        public uint GetRegionId(int x, int y) => _regionMap[x, y].Id;

        public bool IsTileInRegion(int x, int y, uint regionId)
            => _regionMap[x, y].Id == regionId;
    }
}
