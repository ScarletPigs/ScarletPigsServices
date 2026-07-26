namespace ScarletPigsServices.Data.Files
{
    public sealed class MissionUploadResponse
    {
        public required string TargetName { get; init; }
        public required string Folder { get; init; }
        public required string FileName { get; init; }
        public required string RemotePath { get; init; }
    }
}