namespace ScarletPigsServices.Website.Data.Enums
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class DisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public DisplayNameAttribute(string name)
        {
            DisplayName = name;
        }
    }
}
