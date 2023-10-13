using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text;

namespace Project.Controllers
{
    public class idinfoController : ControllerBase
    {
        #region >> 중복ID 확인 구문
        [HttpPost("check")]
        public async Task<IActionResult> idCheck()
        {
            using(StreamReader streamreader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string input = await streamreader.ReadToEndAsync();
                string targetQuery = string.Format("SELECT * FROM member where user_id = '{0}'", input);
                string connoStr = Environment.GetEnvironmentVariable("DB_CONNECTION_INFO");
                using (MySqlConnection msC = new MySqlConnection(connoStr))
                {
                    await msC.OpenAsync();
                    MySqlCommand msc = new MySqlCommand(targetQuery, msC);

                    using (var reader = await msc.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            msC.Close();
                            return BadRequest();
                        }
                        else
                        {
                            msC.Close();
                            return Ok();
                        }
                    }
                }
            }
        }
        #endregion
    }
}