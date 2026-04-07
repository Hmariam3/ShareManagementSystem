using System;
using System.Collections.Generic;
using System.DirectoryServices; // Added for DirectoryEntry, DirectorySearcher, etc.
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shareholder_Management_System.Controllers
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class DisplayNameSearchRequest
    {
        public string DisplayName { get; set; }
    }

    public class LoginController : Controller
    {
        // Move these to web.config for better security
        private readonly string LdapServiceUsername = "noreply";
        private readonly string LdapServicePassword = "N0rep1y7ujm<KI*";
        private readonly string LdapUrl = "LDAP://10.1.72.10";

        [HttpGet]
        public JsonResult SuggestDisplayNames(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return Json(new { success = false, message = "Search term cannot be empty." }, JsonRequestBehavior.AllowGet);
                }

                var displayNames = new List<string>();

                using (var entry = new DirectoryEntry(LdapUrl, LdapServiceUsername, LdapServicePassword))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(displayName=*{HttpUtility.HtmlEncode(term)}*)";
                    searcher.PropertiesToLoad.Add("displayName");
                    searcher.SizeLimit = 10;

                    foreach (SearchResult result in searcher.FindAll())
                    {
                        if (result.Properties.Contains("displayName") && result.Properties["displayName"].Count > 0)
                        {
                            string name = result.Properties["displayName"][0].ToString();
                            displayNames.Add(name);
                        }
                    }
                }

                return Json(new { success = true, suggestions = displayNames }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "LDAP search failed.", error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetUserByDisplayName(string displayName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    return Json(new { success = false, message = "Display name cannot be empty." }, JsonRequestBehavior.AllowGet);
                }

                using (var entry = new DirectoryEntry(LdapUrl, LdapServiceUsername, LdapServicePassword))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(displayName={HttpUtility.HtmlEncode(displayName)})";

                    searcher.PropertiesToLoad.Add("sAMAccountName");
                    searcher.PropertiesToLoad.Add("cn");
                    searcher.PropertiesToLoad.Add("givenName");
                    searcher.PropertiesToLoad.Add("sn");
                    searcher.PropertiesToLoad.Add("displayName");
                    searcher.PropertiesToLoad.Add("mail");
                    searcher.PropertiesToLoad.Add("title");
                    searcher.PropertiesToLoad.Add("department");
                    searcher.PropertiesToLoad.Add("telephoneNumber");
                    searcher.PropertiesToLoad.Add("memberOf");

                    SearchResult result = searcher.FindOne();

                    if (result == null)
                        return Json(new { success = false, message = "User not found." }, JsonRequestBehavior.AllowGet);

                    string username = result.Properties.Contains("sAMAccountName") && result.Properties["sAMAccountName"].Count > 0
                        ? result.Properties["sAMAccountName"][0].ToString()
                        : string.Empty;

                    string fullName = result.Properties.Contains("cn") && result.Properties["cn"].Count > 0
                        ? result.Properties["cn"][0].ToString()
                        : string.Empty;

                    string givenName = result.Properties.Contains("givenName") && result.Properties["givenName"].Count > 0
                        ? result.Properties["givenName"][0].ToString()
                        : string.Empty;

                    string surname = result.Properties.Contains("sn") && result.Properties["sn"].Count > 0
                        ? result.Properties["sn"][0].ToString()
                        : string.Empty;

                    string email = result.Properties.Contains("mail") && result.Properties["mail"].Count > 0
                        ? result.Properties["mail"][0].ToString()
                        : string.Empty;

                    string title = result.Properties.Contains("title") && result.Properties["title"].Count > 0
                        ? result.Properties["title"][0].ToString()
                        : string.Empty;

                    string department = result.Properties.Contains("department") && result.Properties["department"].Count > 0
                        ? result.Properties["department"][0].ToString()
                        : string.Empty;

                    string telephone = result.Properties.Contains("telephoneNumber") && result.Properties["telephoneNumber"].Count > 0
                        ? result.Properties["telephoneNumber"][0].ToString()
                        : string.Empty;

                    var groups = new List<string>();
                    if (result.Properties.Contains("memberOf"))
                    {
                        foreach (var group in result.Properties["memberOf"])
                        {
                            string groupDn = group.ToString();
                            string groupName = groupDn.StartsWith("CN=")
                                ? groupDn.Substring(3, groupDn.IndexOf(',') - 3)
                                : groupDn;
                            groups.Add(groupName);
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        user = new
                        {
                            UserName = username,
                            FullName = fullName,
                            GivenName = givenName,
                            Surname = surname,
                            DisplayName = displayName,
                            Email = email,
                            Title = title,
                            Department = department,
                            Telephone = telephone,
                            Groups = groups
                        }
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "LDAP lookup failed.", error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Authenticate(LoginRequest login)
        {
            try
            {
                if (login == null || string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
                {
                    return Json(new { success = false, message = "Username and password are required." });
                }

                using (var entry = new DirectoryEntry(LdapUrl, login.Username, login.Password))
                {
                    // Trigger LDAP bind
                    object nativeObject = entry.NativeObject;

                    using (var searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = $"(sAMAccountName={HttpUtility.HtmlEncode(login.Username)})";
                        SearchResult result = searcher.FindOne();

                        if (result == null)
                        {
                            return Json(new { success = false, message = "User not found in directory." });
                        }
                    }

                    return Json(new { success = true, message = "LDAP authentication successful." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "LDAP authentication failed.", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SearchByDisplayName(DisplayNameSearchRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.DisplayName))
                {
                    return Json(new { success = false, message = "Display name cannot be empty." });
                }

                using (var entry = new DirectoryEntry(LdapUrl, LdapServiceUsername, LdapServicePassword))
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(displayName=*{HttpUtility.HtmlEncode(request.DisplayName)}*)";

                    searcher.PropertiesToLoad.Add("sAMAccountName");
                    searcher.PropertiesToLoad.Add("cn");
                    searcher.PropertiesToLoad.Add("givenName");
                    searcher.PropertiesToLoad.Add("sn");
                    searcher.PropertiesToLoad.Add("displayName");
                    searcher.PropertiesToLoad.Add("mail");
                    searcher.PropertiesToLoad.Add("title");
                    searcher.PropertiesToLoad.Add("department");
                    searcher.PropertiesToLoad.Add("telephoneNumber");
                    searcher.PropertiesToLoad.Add("memberOf");

                    SearchResultCollection results = searcher.FindAll();
                    var users = new List<object>();

                    foreach (SearchResult result in results)
                    {
                        string username = result.Properties.Contains("sAMAccountName") && result.Properties["sAMAccountName"].Count > 0
                            ? result.Properties["sAMAccountName"][0].ToString()
                            : string.Empty;
                        string fullName = result.Properties.Contains("cn") && result.Properties["cn"].Count > 0
                            ? result.Properties["cn"][0].ToString()
                            : string.Empty;
                        string givenName = result.Properties.Contains("givenName") && result.Properties["givenName"].Count > 0
                            ? result.Properties["givenName"][0].ToString()
                            : string.Empty;
                        string surname = result.Properties.Contains("sn") && result.Properties["sn"].Count > 0
                            ? result.Properties["sn"][0].ToString()
                            : string.Empty;
                        string displayName = result.Properties.Contains("displayName") && result.Properties["displayName"].Count > 0
                            ? result.Properties["displayName"][0].ToString()
                            : string.Empty;
                        string email = result.Properties.Contains("mail") && result.Properties["mail"].Count > 0
                            ? result.Properties["mail"][0].ToString()
                            : string.Empty;
                        string title = result.Properties.Contains("title") && result.Properties["title"].Count > 0
                            ? result.Properties["title"][0].ToString()
                            : string.Empty;
                        string department = result.Properties.Contains("department") && result.Properties["department"].Count > 0
                            ? result.Properties["department"][0].ToString()
                            : string.Empty;
                        string telephone = result.Properties.Contains("telephoneNumber") && result.Properties["telephoneNumber"].Count > 0
                            ? result.Properties["telephoneNumber"][0].ToString()
                            : string.Empty;

                        var groups = new List<string>();
                        if (result.Properties.Contains("memberOf"))
                        {
                            foreach (var group in result.Properties["memberOf"])
                            {
                                string groupDn = group.ToString();
                                string groupName = groupDn.StartsWith("CN=")
                                    ? groupDn.Substring(3, groupDn.IndexOf(',') - 3)
                                    : groupDn;
                                groups.Add(groupName);
                            }
                        }

                        users.Add(new
                        {
                            UserName = username,
                            FullName = fullName,
                            GivenName = givenName,
                            Surname = surname,
                            DisplayName = displayName,
                            Email = email,
                            Title = title,
                            Department = department,
                            Telephone = telephone,
                            Groups = groups
                        });
                    }

                    return Json(new { success = true, users = users });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "LDAP search failed.", error = ex.Message });
            }
        }
    }
}