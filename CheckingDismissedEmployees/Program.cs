using System;
using System.Runtime.CompilerServices;

using static Functions;

using static Const;


Directory.CreateDirectory("./log");
Directory.CreateDirectory("./config");


string ldapPath;
string description;
string ouForDesmissed;
string primaryGroupID;
string groupLdapPath;


Dictionary<string, string> userData = new Dictionary<string, string>
{
    { "cn", "" },
    { "accountExpires", "" },
    { "distinguishedName", "" },
    { "description", "" },
    { "telephoneNumber", "" },
    { "wWWHomePage", "" },
    { "mail", "" },
    { "homePhone", "" },

};


UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + " чтение файла конфига");
using (StreamReader ReaderObject = new StreamReader(configFilelway))
{
    string[] fileData = new string[] { };
    fileData = ReaderObject.ReadToEnd().Split('\n');
    ldapPath = GetParametrValue(fileData[0], ": ")[1];
    ouForDesmissed = GetParametrValue(fileData[1], ": ")[1];
    primaryGroupID = GetParametrValue(fileData[2], ": ")[1];
    groupLdapPath = GetParametrValue(fileData[3], ": ")[1];
}


UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + " получение списка пользователей");
List<string> allLoginAdList = new List<string>();
allLoginAdList = CutNullData(GetAllLoginAd(ldapPath).ToArray());
List<Dictionary<string, string>> userDataList = new List<Dictionary<string, string>>();
for (int i = 0; i < allLoginAdList.Count; i++)
{
    Dictionary<string, string> userDataCopy = new Dictionary<string, string>(userData);
    userDataCopy["sAMAccountName"] = allLoginAdList[i];
    userDataList.Add(userDataCopy);
}

for (int i = 0; i < userDataList.Count; i++)
{
    Dictionary<string, string> userDataSearch = GetAdUserAtributs(ldapPath, userDataList[i]["sAMAccountName"]);

    userDataList[i]["cn"] = userDataSearch["cn"];
    userDataList[i]["accountExpires"] = userDataSearch["accountExpires"];
    userDataList[i]["distinguishedName"] = userDataSearch["distinguishedName"];
    userDataList[i]["description"] = userDataSearch["description"];
    userDataList[i]["telephoneNumber"] = userDataSearch["telephoneNumber"];
    userDataList[i]["wWWHomePage"] = userDataSearch["wWWHomePage"];
    userDataList[i]["mail"] = userDataSearch["mail"];
    userDataList[i]["homePhone"] = userDataSearch["homePhone"];

}


UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + " ищем уволенных");

DateTimeOffset nowOffset = DateTimeOffset.UtcNow;
long fileTime = nowOffset.ToFileTime();

long userTime;

for (int i = 0; i < userDataList.Count; i++)
{
    userTime = Convert.ToInt64(userDataList[i]["accountExpires"]);

    if (userTime != 0 && userTime <= fileTime)
    {
        UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + " " + userDataList[i]["cn"]);

        UpdateUserAttribute(userDataList[i]["sAMAccountName"], "wWWHomePage", userDataList[i]["mail"], ldapPath, userDataList[i]["distinguishedName"]);
        UpdateUserAttribute(userDataList[i]["sAMAccountName"], "homePhone", userDataList[i]["telephoneNumber"], ldapPath, userDataList[i]["distinguishedName"]);
        UpdateUserAttribute(userDataList[i]["sAMAccountName"], "mail", null, ldapPath, userDataList[i]["distinguishedName"]);
        UpdateUserAttribute(userDataList[i]["sAMAccountName"], "telephoneNumber", null, ldapPath, userDataList[i]["distinguishedName"]);
        AddUserToGroup(userDataList[i]["sAMAccountName"], userDataList[i]["distinguishedName"], groupLdapPath, ldapPath);
        int intPrimaryGroupID = int.Parse(primaryGroupID);
        UpdateUserAttribute(userDataList[i]["sAMAccountName"], "primaryGroupID", intPrimaryGroupID, ldapPath, userDataList[i]["distinguishedName"]);
        RemoveUserFromAllGroupsExceptPrimary(userDataList[i]["sAMAccountName"], primaryGroupID, intPrimaryGroupID, userDataList[i]["distinguishedName"]);
        MoveUserToOU(userDataList[i]["sAMAccountName"], ouForDesmissed, ldapPath, userDataList[i]["distinguishedName"]);
    }
}

