namespace Energize.Interfaces.DatabaseModels
{
    public interface IDiscordUser
    {
        int Identity { get; set; }
        ulong ID { get; set; }
        ulong Level { get; set; }
        string Style { get; set; }
        string Description { get; set; }
    }
}
