#region >> 사용모듈
using Microsoft.AspNetCore.Mvc;
using System.DirectoryServices;
using Project.Models;
using System.DirectoryServices.AccountManagement;
#endregion

namespace Project.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class ADUserController : ControllerBase
    {
        public string domainInfo = "kopoproject";
        public string domainInfoSecond = "dev";

        #region >> 전체 정보 조회
        [HttpPost("totalinfo")]
        public IActionResult totalUserInfo()
        {
            List<Dictionary<string, string>> _userList = new List<Dictionary<string, string>>();

            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, string.Format("{0}.{1}", domainInfo, domainInfoSecond), string.Format("OU=Group,DC={0},DC={1}", domainInfo, domainInfoSecond)))
            {
                using (DirectorySearcher searcher = new DirectorySearcher(new DirectoryEntry(string.Format("LDAP://OU=Group,DC={0},DC={1}", domainInfo, domainInfoSecond))))
                {
                    searcher.Filter = "(&(objectClass=organizationalUnit)(!(name=Group)))";
                    foreach (SearchResult result in searcher.FindAll())
                    {
                        string ouName = result.Properties["name"][0].ToString();

                        PrincipalContext ouContext = new PrincipalContext(ContextType.Domain, string.Format("{0}.{1}", domainInfo, domainInfoSecond)
                            , string.Format("OU={0},OU=Group,DC={1},DC={2}", ouName, domainInfo, domainInfoSecond));

                        UserPrincipal user = new UserPrincipal(ouContext);

                        PrincipalSearcher principalsearcher = new PrincipalSearcher(user);

                        foreach (var userResult in principalsearcher.FindAll())
                        {
                            if (userResult is UserPrincipal userPrincipal)
                            {
                                Dictionary<string, string> userInfo = new Dictionary<string, string>();

                                userInfo.Add("OU", ouName);
                                userInfo.Add("ID", userPrincipal.UserPrincipalName);
                                userInfo.Add("FullName", userPrincipal.SamAccountName);
                                userInfo.Add("GivenName", userPrincipal.GivenName);
                                userInfo.Add("SurName", userPrincipal.Surname);
                                userInfo.Add("Email", userPrincipal.EmailAddress);

                                // 유저 생성 간 필수로 입력되는 정보이므로 하나라도 값을 지니지 않는다면 올바르지 않은 데이터로 인식합니다.
                                bool isUserExist = false;
                                foreach (var value in userInfo.Values)
                                {
                                    if (value != null)
                                    {
                                        isUserExist = true;
                                        break;
                                    }
                                }
                                if (isUserExist)
                                {
                                    _userList.Add(userInfo);
                                }
                            }
                        }
                    }
                }
            }
            return Ok(_userList);
        }
        #endregion

        #region >> 유저 생성
        [HttpPost("usercreate")]
        public IActionResult UserCreate([FromBody] AddsUserInsertModel addsM)
        {
            try
            {
                string userGroupOU = addsM.userOu;
                string userId = addsM.userId;
                string samAccountName = addsM.userName;
                string userEmail = addsM.userEmailAddress;
                string password = addsM.userPassword;

                //Console.WriteLine(userGroupOU);
                //Console.WriteLine(string.Format("LDAP://OU={0},OU=Group,DC=kopoproject,DC=dev", userGroupOU));
                //Console.WriteLine(string.Format("LDAP://CN={0},OU={1},OU=Group,DC=kopoproject,DC=dev", addsM.userName, userGroupOU));

                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, string.Format("{0}.{1}", domainInfo, domainInfoSecond)
                    , string.Format("OU={0},OU=Group,DC={1},DC={2}", userGroupOU, domainInfo, domainInfoSecond)))
                {
                    UserPrincipal user = new UserPrincipal(context);
                    user.SamAccountName = userId;
                    user.UserPrincipalName = string.Format("{0}@{1}.{2}", userId, domainInfo, domainInfoSecond);
                    user.DisplayName = samAccountName;
                    user.EmailAddress = userEmail;

                    string _givenName = samAccountName.Split(' ')[0];
                    string _surName = samAccountName.Split(' ')[1];

                    user.GivenName = _givenName;
                    user.Surname = _surName;
                    user.SetPassword(password);
                    user.Enabled = true;
                    user.Save();
                    return Ok("AD 신규 유저 생성이 완료되었습니다.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        #endregion

        #region >> 그룹 이동
        [HttpPost("usergroupmove")]
        public async Task<IActionResult> UserGroupMove([FromBody] AddsMainModel addsmainmodel)
        {
            string changeUserName = addsmainmodel.dataFirst;
            string presentGroupOU = addsmainmodel.dataSecond;
            string userGroupOU = addsmainmodel.dataThird;
            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, string.Format("{0}.{1}", domainInfo, domainInfoSecond)
                    , string.Format("OU={0},OU=Group,DC={1},DC={2}", presentGroupOU, domainInfo, domainInfoSecond)))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, changeUserName);

                    DirectoryEntry userEntry = (DirectoryEntry)user.GetUnderlyingObject();
                    DirectoryEntry targetOuEntry = new DirectoryEntry(string.Format("LDAP://OU={0},OU=Group,DC=kopoproject,DC=dev", userGroupOU));
                    userEntry.MoveTo(targetOuEntry);
                    userEntry.CommitChanges();
                    userEntry.Close();
                    targetOuEntry.Close();
                }
                return Ok("그룹 변경이 완료되었습니다.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region >> 유저 삭제
        [HttpPost("userdelete")]
        public async Task<IActionResult> UserDelete([FromBody] AddsMainModel addsmainmodel)
        {
            string userName = addsmainmodel.dataFirst;
            string userOu = addsmainmodel.dataSecond;
            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, string.Format("{0}.{1}", domainInfo, domainInfoSecond)
                    , string.Format("OU={0},OU=Group,DC={1},DC={2}", userOu, domainInfo, domainInfoSecond)))
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName);
                    user.Delete();
                }
                return Ok("AD 유저 삭제가 완료되었습니다.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region >> 조직 구성 단위(OU) 신설
        [HttpPost("createou")]
        public IActionResult CreateOU([FromBody] AddsMainModel addsmainmodel) // Blank 단위로 구분해서 최상부 부터 하부까지 신규 OU를 생성. 단, 상위 계층이 존재하지 않는데 생성하면 오류 발생
        {
            try
            {
                using (DirectoryEntry _directoryEntry = new DirectoryEntry(string.Format("LDAP://DC={0},DC={1}", domainInfo, domainInfoSecond)))
                {
                    string newOUname = addsmainmodel.dataFirst;
                    string isTopOU = addsmainmodel.dataSecond;
                    string targetOuPath = "";
                    int countBlank = newOUname.Split(' ').Length-1;

                    if (countBlank > 0)
                    {
                        string[] ouParts = newOUname.Split(' ');

                        for (int i = countBlank; i >= 0; i--)
                        {
                            targetOuPath += string.Format("OU=" + ouParts[i]);

                            if (i != 0)
                            {
                                targetOuPath += ",";
                            }
                        }
                        DirectoryEntries _directoryEntries = _directoryEntry.Children;
                        DirectoryEntry _newDirectoryEntry = _directoryEntries.Add(targetOuPath, "OrganizationalUnit");
                        _newDirectoryEntry.CommitChanges();
                    }
                    else if(isTopOU.Equals("Yes"))
                    {
                        DirectoryEntries _directoryEntries = _directoryEntry.Children;
                        DirectoryEntry _newDirectoryEntry = _directoryEntries.Add("OU=" + newOUname, "OrganizationalUnit");
                        _newDirectoryEntry.CommitChanges();
                    }
                    else
                    {
                        DirectoryEntries _directoryEntries = _directoryEntry.Children;
                        DirectoryEntry _newDirectoryEntry = _directoryEntries.Add("OU=" + newOUname + ",OU=Group", "OrganizationalUnit");
                        _newDirectoryEntry.CommitChanges();
                    }
                }
                return Ok("새로운 OU가 생성되었습니다.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        /*
        #region >> 최상위 조직 구성 단위(OU) 신설
        [HttpPost("topoucreate")]
        public IActionResult TopOUcreate([FromBody] AddsMainModel addsmainmodel)
        {
            string newTopOuName = addsmainmodel.targetOU;
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(string.Format("LDAP://DC={0},DC={1}", domainInfo, domainInfoSecond)))
                {
                    DirectoryEntries newEntries = entry.Children;
                    DirectoryEntry newTopOU = newEntries.Add("OU=" + newTopOuName, "OrganizationalUnit");
                    newTopOU.CommitChanges();
                    return Ok(string.Format("신규 최상위 조직 그룹 단위가 신설되었습니다. [OU명:{0}]", newTopOuName));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
        */

        //#region >> 그룹명 확인
        //[HttpPost("findGroupName")]
        //public async Task<string> findGroupName(string groupId)
        //{
        //    string fullQuery = string.Format("SELECT group_name FROM groupinfo WHERE group_id = '{0}';", groupId);
        //    string connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_INFO");
        //    try
        //    {
        //        using (MySqlConnection connection = new MySqlConnection(connStr))
        //        {
        //            await connection.OpenAsync();
        //            MySqlCommand cmd = new MySqlCommand(fullQuery, connection);

        //            List<Dictionary<string, object>> myList = new List<Dictionary<string, object>>();

        //            using (MySqlDataReader _mysqldatareader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
        //            {
        //                while (await _mysqldatareader.ReadAsync())
        //                {
        //                    Dictionary<string, object> row = new Dictionary<string, object>();

        //                    for (int i = 0; i < _mysqldatareader.FieldCount; i++)
        //                    {
        //                        string columnName = _mysqldatareader.GetName(i);
        //                        object value = _mysqldatareader[i];
        //                        row[columnName] = value;
        //                    }

        //                    myList.Add(row);
        //                }
        //            }
        //            if (myList.Count > 0 && myList[0].ContainsKey("group_name"))
        //            {
        //                return myList[0]["group_name"].ToString();
        //            }
        //            else
        //            {
        //                return "CODE005";
        //                /* CODE005 : 대상 그룹이 존재하지 않을 때. 신설을 위해 필요함. */
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.Message;
        //    }
        //}
        //#endregion
    }
}