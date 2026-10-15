using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Const
{
    public const string configFilelway = "config\\Config.txt";

    public static readonly string[] requiredKeys =
        {
        "ActiveDirectory:ldapPath",
        "ActiveDirectory:ouForDesmissed",
        "ActiveDirectory:primaryGroupID",
        "ActiveDirectory:distinguishedName"
        };

}
