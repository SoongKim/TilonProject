using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
using MySql.Data;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Hosting.Server;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class dbWork : ControllerBase
    {
        #region SELECT QUERY
        /* swagger 포인트 잡기 위한 Post타입 요청 설계 */
        [HttpPost("SelectQuery")]
        public async Task<IActionResult> SelectQuery(string fullQuery)
        {
            try
            {
                string connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_INFO");
                using (MySqlConnection connection = new MySqlConnection(connStr))
                {
                    await connection.OpenAsync();
                    
                    MySqlCommand cmd = new MySqlCommand(fullQuery, connection);
                    List<Dictionary<string, object?>> dbResult = new List<Dictionary<string, object?>>();
                    using (MySqlDataReader msr = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await msr.ReadAsync())
                        {
                            Dictionary<string, object?> row = new Dictionary<string, object?>();
                            for (int i = 0; i < msr.FieldCount; i++)
                            {
                                string columnName = msr.GetName(i);
                                object value = msr[i];
                                row[columnName] = value;
                            }
                            dbResult.Add(row);
                        }
                    }
                    return Ok(dbResult);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region UPDATE, INSERT, DELETE QUERY
        [HttpPost("UpdateInsertDelete")]
        public async Task<IActionResult> UpdateInsertDeleteQuery(string fullQuery)
        {
            try
            {
                string connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_INFO");
                using (MySqlConnection connection = new MySqlConnection(connStr))
                {
                    await connection.OpenAsync();

                    using(MySqlCommand cmd = new MySqlCommand(fullQuery, connection))
                    {
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Ok("처리되었습니다.");
                        }
                        else
                        {
                            return BadRequest();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region 로그 생성
        [HttpPost("LogSession")]
        public async Task<IActionResult> LogSession(string? logUser, string queryType, string userType)
        {
            string connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_INFO");
            
            using (MySqlConnection connection = new MySqlConnection(connStr))
            {
                await connection.OpenAsync();
                string? fullQuery = "";
                if (queryType.ToUpper().Equals("SELECT"))
                {
                    if (userType.Equals("0") && logUser == null) // 관리자 전체 조회
                    {
                        fullQuery = string.Format("INSERT INTO log (admin_id, action) values ('{0}', '{1}');", "AdminUser", queryType + " total User Infoes");
                    }
                    else if (userType.Equals("0") && logUser != null) // 관리자 특정 유저 조회
                    {
                        fullQuery = string.Format("INSERT INTO log (admin_id, action) values ('{0}', '{1}');", "AdminUser", queryType + " specific User " + "[" + logUser + "]" + " Infoes");
                    }
                    else if (userType.Equals("1") && logUser != null) // 일반 유저 로그인 조회(본인 ID, PW 조회)
                    {
                        fullQuery = string.Format("INSERT INTO log (user_id, action) values ('{0}', '{1}');", logUser, queryType + " Trying Login Session");
                    }
                    else // 일반 유저 전체 조회 시. 액션 구현 없으나 에러 문구 삽입.
                    {
                        return BadRequest("Normal User Doesn't Authorized to Search Total User Infoes");
                    }
                }
                else if (queryType.ToUpper().Equals("UPDATE"))
                {
                    if (userType.Equals("0")) // 관리자의 유저 정보 수정. 권한 조정 간에만 사용
                    {
                        fullQuery = string.Format("INSERT INTO log (user_id, action) values ('{0}', '{1}');", logUser, queryType + " User ["+logUser+"]'s Authority");
                    }
                    else // 일반 유저의 본인 정보 수정
                    {
                        fullQuery = string.Format("INSERT INTO log (user_id, action) values ('{0}', '{1}');", logUser, queryType + " User ["+logUser+"] Infoes");
                    }
                }
                else if (queryType.ToUpper().Equals("INSERT"))
                {
                    Console.WriteLine("OK!");
                    fullQuery = string.Format("INSERT INTO log(user_id, action) values ('{0}', '{1}');", logUser, queryType + " New User [" + logUser + "] Infoes");
                }

                using (MySqlCommand cmd = new MySqlCommand(fullQuery, connection))
                {
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("Affected Row Count is 0");
                    }
                }
            }
        }
        #endregion
    }
}
