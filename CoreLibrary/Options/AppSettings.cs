using Core.Models;

namespace Core.Options
{
    public class AppSettings
    {
        public int MapWidth { get; set; } = 1000;
        public int MapHeight { get; set; } = 1000;
        public Region[] Regions { get; set; } = [];
    }
}
