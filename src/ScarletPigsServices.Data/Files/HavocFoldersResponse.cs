namespace ScarletPigsServices.Data.Files
{
    public sealed class HavocFoldersResponse
    {
        public required string TargetName { get; init; }
        public required bool IsConfigured { get; init; }
        public required IReadOnlyList<string> Folders { get; init; }
    }
}