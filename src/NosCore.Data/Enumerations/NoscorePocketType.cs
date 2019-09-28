using ChickenAPI.Packets.Enumerations;

namespace NosCore.Data
{
    public enum NoscorePocketType : byte
    {
        Equipment = PocketType.Equipment,
        Main = PocketType.Main,
        Etc = PocketType.Etc,
        Miniland = PocketType.Miniland,
        Specialist = PocketType.Specialist,
        Costume = PocketType.Costume,
        Wear = 8
    }
}