namespace ScarletPigsServices.Website.Data.Enums
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class CommandLineNameAttribute : Attribute
    {
        public string Name { get; }

        public CommandLineNameAttribute(string name)
        {
            Name = name;
        }
    }
}
