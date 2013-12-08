using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;


namespace UnitTestProject_ruPost
{
    [TestClass]    
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //Вывод сообщения
            Action<string> f_assert_message = 
            (m)=>
            {
                try
                {
                    Assert.Inconclusive(m);
                }
                catch (AssertInconclusiveException)
                {
                }
            };

            Task<dynamic> tStateTracks = RussianPostAgent.getStates(
                "[login]", "[password]",
                new string[] { //Список трэкеров
                               string.Empty.PadRight(14,"1"[0]), //"11111111111111"
                               string.Empty.PadRight(14,"2"[0]),
                               string.Empty.PadRight(14,"3"[0]),
                               string.Empty.PadRight(14,"4"[0]),
                             });
            //Ожидаем завершения задачи
            tStateTracks.Wait();
            
            //Пполучим результат выполнения 
            dynamic data = tStateTracks.Result;
           

            //Проверим его на ошибку
            string error = data.Exception as string;                   
            Assert.IsFalse(!string.IsNullOrEmpty(error), error);
            
            //Вывод результатов
            object[] operations = data.Operations;            
            object[] errors = data.Errors;
            foreach (dynamic oper in operations)                
                      f_assert_message(
                           string.Format("TrackId:{0}\tDate:{1}\tID Oper:{2}\tOper Name:{3}\tCategory:{4}",
                           oper.TrackID,
                           oper.Date,
                           oper.ID,
                           oper.Name,
                           oper.Category
                           ));
                   foreach (dynamic err in errors)
                       f_assert_message(
                           string.Format("TrackId:{0}\tErrorID:{1}\tError:{2}",
                           err.TrackID,
                           err.ID,
                           err.Name
                           ));            
        }
    }
}
