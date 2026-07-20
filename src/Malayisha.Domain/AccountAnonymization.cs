namespace Malayisha.Domain;

public static class AccountAnonymization
{
    public static string CreatePhoneIdentifier(Guid userId) =>
        $"del-{userId:N}"[..20];

    public static string CreateDisplayNameIdentifier(Guid userId) =>
        $"deleted-user-{userId:D}";
}
