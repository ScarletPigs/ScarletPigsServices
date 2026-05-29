namespace ScarletPigsServices.Data.Auth
{
    public sealed class CurrentUserResponse
    {
        public required string Id { get; init; }
        public required string Email { get; init; }
        public required string GlobalName { get; init; }
        public required string UserName { get; init; }
        public required string AvatarHash { get; init; }
        public required string AvatarUrl { get; init; }
        public required IReadOnlyList<string> Roles { get; init; }
        public required bool IsAdmin { get; init; }
        public required bool IsAllowedMissionUpload { get; init; }
    }
}