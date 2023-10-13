using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Project.Models;
using System;
using System.Globalization;
using Project.Services;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class MainController : ControllerBase
    {

        #region INSERT(Normal), SELECT, UPDATE
        [HttpPost("query")] // Select, Update, Delete, Insert(Normal) 시 https://Domain:11088/api/main/query URL을 사용합니다.
        public async Task<IActionResult> TargetQuery([FromBody] QueryModel jsonQueryModel)
        {
            string targetQuery = jsonQueryModel.queryType.ToUpper();
            try
            {
                if (targetQuery.Equals("SELECT"))
                {
                    QueryServices queryservices = new QueryServices();
                    if (jsonQueryModel.dataThird != null)
                    {
                        string answer = queryservices.SqlInjectionChecking(jsonQueryModel.dataThird);
                        if (answer.Equals("CODE001"))
                        {
                            return BadRequest("SQL INJECTION 공격이 감지되었습니다.");
                        }
                    }

                    var result = await queryservices.Select(jsonQueryModel);
                    if (result is OkObjectResult okResult) // result 내 Value 값만을 정제하여 return
                    {
                        dbWork dbwork = new dbWork();
                        string? logUser = null;
                        if (jsonQueryModel.dataThird != null)
                        {
                            logUser = jsonQueryModel.dataThird.Replace("'", "").Replace(" ", "").Split("=")[1].Split("AND")[0];
                        }
                        else
                        {
                            logUser = null;
                        }
                        string userType = jsonQueryModel.dataFifth;
                        string queryType = jsonQueryModel.queryType.ToUpper();
                        var logResult = await dbwork.LogSession(logUser, queryType, userType);

                        if (logResult is BadRequestObjectResult badRequestObject)
                        {
                            return Ok(badRequestObject.Value);
                        }
                        else
                        {
                            return Ok(okResult.Value);
                        }
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                else if (targetQuery.Equals("UPDATE"))
                {
                    QueryServices queryservices = new QueryServices();
                    if (jsonQueryModel.dataFourth != null)
                    {
                        string answer = queryservices.SqlInjectionChecking(jsonQueryModel.dataFourth);
                        if (answer.Equals("CODE001"))
                        {
                            return BadRequest("SQL INJECTION 공격이 감지되었습니다.");
                        }
                    }

                    var result = await queryservices.Update(jsonQueryModel);
                    dbWork dbwork = new dbWork();

                    string logUser = jsonQueryModel.dataFourth.Replace("'", "").Replace(" ", "").Split("=")[1];
                    string userType = jsonQueryModel.dataFifth;
                    string queryType = jsonQueryModel.queryType.ToUpper();
                    var logResult = dbwork.LogSession(logUser, queryType, userType);
                    /* Json 내 Value값이 필요한 경우가 아니므로 return result;로 간략화 */
                    return result;
                }
                else if (targetQuery.Equals("INSERT"))
                {
                    QueryServices queryservices = new QueryServices();
                    if (jsonQueryModel.dataFourth != null)
                    {
                        string answer = queryservices.SqlInjectionChecking(jsonQueryModel.dataFourth);
                        if (answer.Equals("CODE001"))
                        {
                            return BadRequest("SQL INJECTION 공격이 감지되었습니다.");
                        }
                    }
                    var result = await queryservices.Insert(jsonQueryModel);
                    return result;
                }
                else if (targetQuery.Equals("DELETE"))
                {
                    QueryServices queryservices = new QueryServices();
                    if (jsonQueryModel.dataSecond != null)
                    {
                        string answer = queryservices.SqlInjectionChecking(jsonQueryModel.dataSecond);
                        if (answer.Equals("CODE001"))
                        {
                            return BadRequest("SQL INJECTION 공격이 감지되었습니다.");
                        }
                    }

                    var result = await queryservices.Delete(jsonQueryModel);
                    return result;
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
                /* 어떤 오류가 발생하였는지 에러 코드 + 어떤 부분이 잘못되었는지 에러 코드 뱉도록 */
            }
            return BadRequest("Error Occured!");
        }
        #endregion

        #region INSERT QUERY(User) PART
        [HttpPost("userInsert")]
        public async Task<IActionResult> userInfoController([FromBody] UserInfoModel uiM)
        {
            #region BadRequest 판정 부문
            if (uiM == null)
            {
                return BadRequest("Json Body에 값이 담기지 않았습니다.");
            }
            #endregion

            //return Ok("Good1");

            /* 데이터 정제 부문 */
            string user_id = "'" + uiM.user_id + "', ";
            string password = "'" + uiM.password + "', ";
            string name = "'" + uiM.name + "',";
            string employee_num = "'" + uiM.employee_num + "',";
            string role_id = "'" + uiM.role_id + "'";

            //return Ok("Good2");

            string fullTarget = user_id + password + name + employee_num + role_id;

            //return Ok("Good3");

            string fullQuery = string.Format("INSERT INTO member (user_id, password, name, employee_num, role_id) VALUES ({0});", fullTarget);

            //return Ok(fullQuery);

            dbWork dbWorks = new dbWork();

            var result = await dbWorks.UpdateInsertDeleteQuery(fullQuery);

            string logUser = uiM.user_id;
            string queryType = "insert";
            string userType = "1";
            var logResult = await dbWorks.LogSession(logUser, queryType, userType);
            Console.WriteLine(logResult);
            return result;
        }
        #endregion

        #region DBCONNECTING
        [HttpPost("toDBconnector")]
        public async Task<IActionResult> toDBcontroller(string targetQuery)
        {
            dbWork dbWorks = new dbWork();
            var result = await dbWorks.UpdateInsertDeleteQuery(targetQuery);
            if (result is OkObjectResult okResult)
            {
                return Ok(okResult.Value);
            }
            else
            {
                return BadRequest();
            }
        }
        #endregion
    }
}
