namespace ObjectLayerLibrary.Models
{
    public struct GameObject(
        string id,
        int x,
        int y,
        int width,
        int height
    )
    {
        public string Id { get; set; } = id;
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int Width { get; set; } = width;
        public int Height { get; set; } = height;

        public bool ContainsPoint(int pointX, int pointY)
        {
            return pointX >= X && pointX < X + Width &&
                   pointY >= Y && pointY < Y + Height;
        }

        public bool IntersectsWith(int areaX, int areaY, int areaWidth, int areaHeight)
        {
            return X < areaX + areaWidth && X + Width > areaX &&
                   Y < areaY + areaHeight && Y + Height > areaY;
        }
    }
}
