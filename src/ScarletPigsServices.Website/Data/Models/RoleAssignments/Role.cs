namespace ScarletPigsServices.Website.Data.Models.RoleAssignments
{
    public class Role
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? AssignedPlayer { get; set; }
        public string[]? DLCsRequired { get; set; }
        public string[]? TrainingRequired { get; set; }

        public Role()
        {
            Name = "";
            Description = "";
            Icon = "";
            AssignedPlayer = "";
            DLCsRequired = new string[] { };
            TrainingRequired = new string[] { };
        }
    }
}
