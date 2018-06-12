namespace NosCore.Shared.Enumerations.Group
{
	public enum GroupRequestType : byte
	{
		Requested = 0,
		Invited = 1,
		Accepted = 3,
		Declined = 4,
		Sharing = 5,
		AcceptedShare = 6,
		DeclinedShare = 7
	}
}