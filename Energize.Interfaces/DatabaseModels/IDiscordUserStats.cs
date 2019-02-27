namespace Energize.Interfaces.DatabaseModels
{
    public interface IDiscordUserStats
    {
        int Identity { get; set; }
        ulong ID { get; set; }
        ulong SnuggledCount { get; set; }
        ulong HuggedCount { get; set; }
        ulong BoopedCount { get; set; }
        ulong SlappedCount { get; set; }
        ulong KissedCount { get; set; }
        ulong ShotCount { get; set; }
        ulong PetCount { get; set; }
        ulong SpankedCount { get; set; }
        ulong YiffedCount { get; set; }
        ulong NomedCount { get; set; }
        ulong LickedCount { get; set; }
        ulong BittenCount { get; set; }
        ulong FlexCount { get; set; }
    }
}
