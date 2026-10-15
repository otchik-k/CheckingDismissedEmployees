using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class Functions
{
    public static string logName = "log\\log - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".txt";


    public static void UsingStreamWriter(string anyString)
    {
        using (StreamWriter logWriter = new StreamWriter(logName, true))
        {
            logWriter.WriteLine(anyString);
        }
    }


    public static string[] GetParametrValue(string configFileLine, string separator)
    {
        return configFileLine.Split(separator);
    }


    public static List<string> CutNullData(string[] anyArray)
    {
        List<string> cat = new List<string>();
        for (int i = 0; i < anyArray.Length; i++)
        {
            if (anyArray[i] != null)
            {
                cat.Add(anyArray[i].ToString());
            }
        }
        return cat;
    }


    public static List<string> GetAllLoginAd(string ldapPath)
    {

        using (DirectoryEntry entry = new DirectoryEntry("LDAP://" + ldapPath))
        {
            try
            {
                DirectorySearcher searcher = new DirectorySearcher(entry);
                searcher.Filter = "(objectClass=user)";
                searcher.PropertiesToLoad.Add("sAMAccountName");
                SearchResultCollection results = searcher.FindAll();

                List<string> samAccountNames = new List<string>();
                foreach (SearchResult result in results)
                {
                    if (result.Properties.Contains("sAMAccountName"))
                    {
                        string samAccountName = (string)result.Properties["sAMAccountName"][0];
                        samAccountNames.Add(samAccountName);
                    }

                }

                return samAccountNames;
            }
            catch (Exception ex)
            {
                UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + ex.Message);
                return null;
            }

        }
    }


    public static Dictionary<string, string> GetUserAtributs(string ldapPath, string login)
    {
        Dictionary<string, string> resultDict = new Dictionary<string, string>
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

        using (DirectorySearcher searcher = new DirectorySearcher())
        {
            searcher.SearchRoot = new DirectoryEntry("LDAP://" + ldapPath);
            searcher.Filter = $"sAMAccountName={login}";

            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("accountExpires");
            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("description");
            searcher.PropertiesToLoad.Add("telephoneNumber");
            searcher.PropertiesToLoad.Add("wWWHomePage");
            searcher.PropertiesToLoad.Add("mail");
            searcher.PropertiesToLoad.Add("homePhone");

            SearchResult result = searcher.FindOne();

            if (result != null)
            {
                foreach (var key in resultDict.Keys)
                {
                    if (result.Properties.Contains(key) && result.Properties[key].Count > 0)
                    {
                        resultDict[key] = result.Properties[key][0].ToString();
                    }
                    else
                    {
                        resultDict[key] = null;
                    }
                }
            }
            else
            {
                UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + "Пользователь" + login + " не найден.");
            }
        }
        return resultDict;
    }


    public static int GetUserAccountControl(string ldapPath, string login)
    {
        using (DirectoryEntry root = new DirectoryEntry("LDAP://" + ldapPath))
        using (DirectorySearcher searcher = new DirectorySearcher(root))
        {
            searcher.Filter = $"(sAMAccountName={login})";

            searcher.PropertiesToLoad.Add("userAccountControl");

            SearchResult result = searcher.FindOne();

            if (result != null)
            {
                if (result.Properties.Contains("userAccountControl") &&
                    result.Properties["userAccountControl"].Count > 0)
                {
                    return Convert.ToInt32(result.Properties["userAccountControl"][0]);
                }
                else
                {
                    return 512;
                }
            }
            else
            {
                // Пользователь с таким логином не найден
                UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + "Пользователь " + login + " не найден.");
                return -1;
            }
        }
    }


    public static void MoveUserToOU(string userSamAccountName, string targetOUPath, string ldapPath, string distinguishedName)
    {
        try
        {
            using (DirectoryEntry root = new DirectoryEntry($"LDAP://{ldapPath}"))
            {
                using (DirectoryEntry user = new DirectoryEntry($"LDAP://{distinguishedName}"))
                {

                    user.MoveTo(new DirectoryEntry($"LDAP://{targetOUPath}"));
                    user.CommitChanges();
                    UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + " " + userSamAccountName + " успешно перемещён в OU: " + targetOUPath);
                }

            }
        }
        catch (Exception ex)
        {
            UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $"Ошибка: {ex.Message}");
        }
    }


    public static void UpdateUserAttribute(string userSamAccountName, string attributeName, object newValue, string ldapPath, string distinguishedName)
    {
        try
        {
            using (DirectoryEntry root = new DirectoryEntry($"LDAP://{ldapPath}"))
            {
                using (DirectoryEntry user = new DirectoryEntry($"LDAP://{distinguishedName}"))
                {
                    user.Properties[attributeName].Value = newValue;
                    user.CommitChanges();
                    UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + " Пользователь " + userSamAccountName + " изменен атрибут: " + attributeName + ", новое значение: " + newValue);
                }

            }

        }
        catch (Exception ex)
        {
            UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $"Ошибка при обновлении атрибута: {ex.Message}");
        }
    }


    public static void UpdateUserAccountControl(string userSamAccountName, int currentUserAccountControl, int newUserAccountControl, string ldapPath, string distinguishedName)
    {
        if ((currentUserAccountControl & 2) == 2)
        {
            UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $"Пользователь {userSamAccountName} уже заблокирован");
        }
        else
        {
            try
            {
                using (DirectoryEntry root = new DirectoryEntry($"LDAP://{ldapPath}"))
                {
                    using (DirectoryEntry user = new DirectoryEntry($"LDAP://{distinguishedName}"))
                    {
                        user.Properties["userAccountControl"].Value = currentUserAccountControl | newUserAccountControl;
                        user.CommitChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $"ОШИБКА при блокировке пользователя {userSamAccountName}: {ex.Message}");
            }
        }
        
    }


    public static void AddUserToGroup(string userSamAccountName, string userDistinguishedName, string groupDistinguishedName, string ldapPath)
    {
        try
        {
            using (DirectoryEntry group = new DirectoryEntry($"LDAP://{groupDistinguishedName}"))
            {
                if (group.Properties["member"].Contains(userDistinguishedName))
                {
                    UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") +
                        $" Пользователь {userSamAccountName} уже состоит в группе {groupDistinguishedName}");
                    return;
                }

                group.Properties["member"].Add(userDistinguishedName);
                group.CommitChanges();

                UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") +
                    $" Пользователь {userSamAccountName} успешно добавлен в группу {groupDistinguishedName}");
            }
        }
        catch (System.Runtime.InteropServices.COMException comEx) when (comEx.ErrorCode == -2147016653)
        {
            UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $" Предупреждение: Пользователь {userSamAccountName} уже состоит в группе");
        }
        catch (Exception ex)
        {
            UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $" Ошибка при добавлении пользователя {userSamAccountName} в группу: {ex.Message}");
            throw;
        }
    }


    public static void RemoveUserFromAllGroupsExceptPrimary(string userSamAccountName, string primaryGroupDistinguishedName, int primaryGroupId, string userDistinguishedName)
    {
        try
        {
            using (DirectoryEntry user = new DirectoryEntry($"LDAP://{userDistinguishedName}"))
            {
                // Получаем список групп из memberOf (основная группа туда НЕ входит по дизайну AD)
                var groupsToRemove = new List<string>();
                foreach (var group in user.Properties["memberOf"])
                {
                    groupsToRemove.Add(group.ToString());
                }

                // Удаляем пользователя из каждой группы
                int removedCount = 0;
                foreach (string groupDn in groupsToRemove)
                {
                    // Дополнительная защита: пропускаем основную группу, если вдруг она там оказалась
                    if (!string.IsNullOrEmpty(primaryGroupDistinguishedName) &&
                        groupDn.Equals(primaryGroupDistinguishedName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        using (DirectoryEntry group = new DirectoryEntry($"LDAP://{groupDn}"))
                        {
                            if (group.Properties["member"].Contains(userDistinguishedName))
                            {
                                group.Properties["member"].Remove(userDistinguishedName);
                                group.CommitChanges();
                                removedCount++;
                            }
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException comEx) when (comEx.ErrorCode == -2147016672) // ADS_ERROR_NO_SUCH_MEMBER
                    {
                        // Пользователь уже не в группе — игнорируем
                        continue;
                    }
                    catch (Exception ex)
                    {
                        UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $" Не удалось удалить {userSamAccountName} из группы {groupDn}: {ex.Message}");
                        // Продолжаем обработку остальных групп
                    }
                }

                UsingStreamWriter($"{DateTime.Now:HH.mm.ss} {userSamAccountName} (primaryGroupId: {primaryGroupId}) " +
                    $"удалён из {removedCount} групп. Основная группа: {primaryGroupDistinguishedName ?? "не указана"}");
            }
        }
        catch (Exception ex)
        {
            UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $" Ошибка при очистке групп для {userSamAccountName}: {ex.Message}");
            throw;
        }
    }


    public static bool ValidateConfig(IConfigurationRoot configuration, string[] requiredKeys)
    {
        int count = requiredKeys.Length;

        foreach (var key in requiredKeys)
        {
            if (string.IsNullOrEmpty(configuration[key]))
            {
                UsingStreamWriter(DateTime.Now.ToString("HH.mm.ss") + $" ОШИБКА! Отсутствует параметр {key}");
                count--;
            }
        }
        if (count < requiredKeys.Length)
        {
            return false;
        }
        else
        {
            return true;
        }
    }


    public static void CreateConfigFile(string filePath, string fileName)
    {
        var config = new
        {
            ActiveDirectory = new
            {
                ldapPath = "DC=youdomain, DC=local",
                ouForDesmissed = "OU=youOU, DC=youdomain, DC=local",
                primaryGroupID = "6154 example",
                distinguishedName = "CN=youCN, OU=youOU, DC=youdomain, DC=local"
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string jsonString = JsonSerializer.Serialize(config, options);
        File.WriteAllText($"{filePath}\\{fileName}", jsonString, System.Text.Encoding.UTF8);
    }
}

