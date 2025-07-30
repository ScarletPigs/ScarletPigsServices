namespace ScarletPigsServices.Website.Data.Enums
{
    public enum DlcEnum
    {
        [DisplayName("Western Sahara")]
        [CommandLineName("ws")]
        WesternSahara = 1681170,

        [DisplayName("Spearhead 1944")]
        [CommandLineName("spe")]
        Spearhead = 1175380,

        [DisplayName("Contact")]
        [CommandLineName("")] //never need to add this to the command line
        Contact = 1021790,

        [DisplayName("Global Mobilization")]
        [CommandLineName("gm")]
        GlobalMobilization = 1042220,

        [DisplayName("S.O.G. Prairie Fire")]
        [CommandLineName("vn")]
        SogPrairieFire = 1227700,

        [DisplayName("CSLA Iron Curtain")]
        [CommandLineName("csla")]
        CslaIronCurtain = 1294440,

        [DisplayName("Reaction Forces")]
        [CommandLineName("rf")]
        ReactionForces = 2647760,
    }
}
