namespace glaa_trips.Models
{
    public interface IPaginator
    {
        string Name { get; }

        string Link { get; }

        IPaginator Next { get; }

        IPaginator Previous { get; }
    }
}
