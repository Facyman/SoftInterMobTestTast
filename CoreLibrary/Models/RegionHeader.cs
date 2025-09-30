namespace Core.Models
{
    public readonly ref struct RegionHeader
    {
        public readonly uint? Id;
        public readonly string? Name;

        public RegionHeader(uint? id, string? name)
        {
            Id = id;
            Name = name;
        }
    }
}
