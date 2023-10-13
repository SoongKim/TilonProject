using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using Project.Controllers;
using Project.Models;

namespace Project.Services
{
    public class QueryServices
    {
        #region SELECT QUERY PART
        public async Task<IActionResult> Select(QueryModel queryModel)
        {
            string fullQuery;
            if (queryModel.dataThird == null)
            {
                fullQuery = string.Format("{0} {1} FROM {2};", queryModel.queryType, queryModel.dataFirst, queryModel.dataSecond);
            }
            else
            {
                fullQuery = string.Format("{0} {1} FROM {2} WHERE {3};", queryModel.queryType, queryModel.dataFirst, queryModel.dataSecond, queryModel.dataThird);
            }
            dbWork dbw = new dbWork();
            return await dbw.SelectQuery(fullQuery);
        }
        #endregion

        #region INSERT QUERY(Normal) PART
        public async Task<IActionResult> Insert(QueryModel querymodel)
        {
            string fullQuery = "";
            
            if (querymodel.dataSecond.Contains(","))
            {
                string[] colArr = querymodel.dataSecond.Split(", ");
                string[] valArr = querymodel.dataThird.Split(", ");

                //if (colArr.Length != valArr.Length)
                //{
                //    return BadRequest("입력하는 컬럼과 값의 갯수가 일치하지 않습니다.");
                //}
/*   */
                /* INSERT 간 다중 컬럼을 대상으로 입력을 요할 시. 형식에 맞춰 데이터를 정돈한다. */
                int valArrLength = valArr.Length;
                for (int i = 0; i < valArrLength-1; i++)
                {
                    if (i == valArrLength - 2)
                    {
                        valArr[i] = "'" + valArr[i] + "'";
                    }
                    else
                    {
                        valArr[i] = "'" + valArr[i] + "',";
                    }
                }

                string newTargetValue = "";
                for (int i = 0; i < colArr.Length; i++)
                {
                    newTargetValue += valArr[i];
                }

                if (querymodel.dataFourth == null)
                {
                    fullQuery = string.Format("{0} INTO {1} ({2}) VALUES ({3});", querymodel.queryType, querymodel.dataFirst, querymodel.dataSecond, newTargetValue);

                }
                else
                {
                    fullQuery = string.Format("{0} INTO {1} ({2}) VALUES ({3}) WHERE {4};", querymodel.queryType, querymodel.dataFirst, querymodel.dataSecond, newTargetValue, querymodel.dataFourth);
                }

            }
            else
            {
                if (querymodel.dataFourth == null)
                {
                    fullQuery = string.Format("{0} INTO {1} ({2}) VALUES ('{3}');", querymodel.queryType, querymodel.dataFirst, querymodel.dataSecond, querymodel.dataThird);
                }
                else
                {
                    fullQuery = string.Format("{0} INTO {1} ({2}) VALUES ('{3}') WHERE {4};", querymodel.queryType, querymodel.dataFirst, querymodel.dataSecond, querymodel.dataThird, querymodel.dataFourth);
                }
            }
            //return Ok(fullQuery);
            MainController maincontroller = new MainController();
            var result = await maincontroller.toDBcontroller(fullQuery);
            return result;
        }
        #endregion

        #region UPDATEQUERY PART
        public async Task<IActionResult> Update(QueryModel querymodel)
        {
            string fullQuery = string.Format("{0} {1} SET {2} = '{3}' WHERE {4};",
                querymodel.queryType, querymodel.dataFirst, querymodel.dataSecond, querymodel.dataThird, querymodel.dataFourth);

            MainController maincontroller = new MainController();
            return await maincontroller.toDBcontroller(fullQuery);
            
        }
        #endregion

        #region DELETE QUERY PART
        public async Task<IActionResult> Delete(QueryModel querymodel)
        {
            MainController maincontroller = new MainController();
                string fullQuery = string.Format("{0} FROM {1} WHERE {2};", querymodel.queryType, querymodel.dataFirst, querymodel.dataSecond);
                return await maincontroller.toDBcontroller(fullQuery);
        }
        #endregion

        #region >> SQL INJECTION CHECKING
        public string SqlInjectionChecking(string condition) // 1=1과 같은 동어 반복 SQL Injection 공격을 탐지합니다.
        {
            string checkingString = condition.ToUpper().Replace(" ", "").Replace("(", "").Replace(")", "");
            if (checkingString.Contains("OR"))
            {
                string[] sqlArray = checkingString.Split("OR");
                for (int i = 1; i < sqlArray.Length; i++)
                {
                    string[] checkArray = sqlArray[i].Split("=");
                    if (checkArray[0].Equals(checkArray[1]))
                    {
                        return "CODE001";
                    }
                }
            }
            else
            {
                string[] sqlArray = checkingString.Split("=");
                for(int i = 0; i < sqlArray.Length-1; i+=2)
                {
                    if (sqlArray[i].Equals(sqlArray[(i + 1)]))
                    {
                        return "CODE001";
                    }
                }
            }
            return "CODE002";
        }
        /* 모든 특문 제거 후 등호를 기준으로 좌우를 대조합니다. 동일한 값임이 탐지된 경우 CODE001을, 정상인 경우 CODE002를 return합니다. */
        #endregion
    }
}
