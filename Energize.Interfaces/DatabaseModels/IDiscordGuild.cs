namespace Energize.Interfaces.DatabaseModels
{
    public interface IDiscordGuild
    {
        int Identity { get; set; }
        ulong ID { get; set; }
        bool ShouldDeleteInvites { get; set; }
        bool HasHallOfShames { get; set; }
        ulong HallOfShameID { get; set; }
    }
}
