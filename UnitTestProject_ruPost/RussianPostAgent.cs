using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnitTestProject_ruPost.ruPost;


namespace UnitTestProject_ruPost
{
    /// <summary>
    /// Класс обертка для vfc.russianpost.ru
    /// </summary>
    public class RussianPostAgent
    {
        #region Внутренние объекты

        //Экземпляр клиента-сервиса        
        static readonly ItemDataService client = new ItemDataService();
        //Словари с описанием статусов
        static readonly Dictionary<int,string> _oper_types = new Dictionary<int,string>();
        static readonly Dictionary<int,string> _oper_categories = new Dictionary<int,string>();

        //Статический конструктор
        static RussianPostAgent()
        {
           _oper_types.Add(1,"Приём");
           _oper_types.Add(2,"Вручение");
           _oper_types.Add(3,"Возврат");
           _oper_types.Add(4,"Досылка почты");
           _oper_types.Add(5,"Невручение");
           _oper_types.Add(6,"Хранение");
           _oper_types.Add(7,"Временное хранение");
           _oper_types.Add(8,"Обработка");
           _oper_types.Add(9,"Импорт международной почты");
           _oper_types.Add(10,"Экспорт международной почты");
           _oper_types.Add(11,"Передано таможне");
           _oper_types.Add(12,"Неудачная попытка вручения");
           _oper_types.Add(13,"Регистрация отправки");
           _oper_types.Add(14,"Таможенное оформление завершено");
           _oper_types.Add(15,"Передача на временное хранение");
           _oper_types.Add(16,"Уничтожение");
            
           _oper_categories.Add(0,"Сортировка");
           _oper_categories.Add(1,"Вручение адресату");
           _oper_categories.Add(2,"Прибыло в место вручения");
           _oper_categories.Add(3,"Прибыло в сортировочный центр");
           _oper_categories.Add(4,"Покинуло сортировочный центр");
           _oper_categories.Add(5,"Прибыло в место международного обмена");
           _oper_categories.Add(6,"Покинуло место международного обмена");
           _oper_categories.Add(8,"Иное");
           _oper_categories.Add(9,"Адресат заберет отправление сам");
           _oper_categories.Add(10,"Нет адресата");
        }

        /// <summary>
        /// Получить соответствие ID из словаря если оно есть
        /// </summary>
        /// <param name="d">Словарь</param>
        /// <param name="id">ID</param>
        /// <returns></returns>
        static string _get_(IDictionary<int,string> d,string id)
        {
            int _id=-1;
            if (d!=null&&!string.IsNullOrEmpty(id)
                && int.TryParse(id, out _id)
                && d.ContainsKey(_id))
                return d[_id];
            return id;
        }

        /// <summary>
        /// Получить тип операции по ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static string _get_oper_type(string id)
        {
            return _get_(_oper_types,id);
        }
        
        /// <summary>
        /// Получить категорию операции по ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static string _get_oper_category(string id)
        {
            return _get_(_oper_categories, id);
        }

        #endregion

        /// <summary>
        /// Получить детальную информацию о состояние почтовых трэков
        /// </summary>
        /// <param name="login">Логин в системе vfc.russianpost.ru</param>
        /// <param name="password">Пароль</param>
        /// <param name="trackid_list">Список trackid</param>        
        public static Task<dynamic> getStates(string login, string password, string[] trackid_list)
        {            
            //Если параметры пустые, то выходим
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)
                || (trackid_list == null || trackid_list.Length == 0))
                return null;

            return Task.Factory.StartNew<dynamic>((Func<object,dynamic>)(
                (args) =>
                {
                    object result = null;

                    //Обработка ошибки
                    Func<object, bool> f_error =
                        e =>
                        {
                            if (e is error)
                            {
                                error _error = e as error;
                                //Установим результат с исключением
                                if (_error != null)
                                   result = new { Exception = string.Format("{0} (код ошибки:{1})", _error.ErrorName, _error.ErrorTypeID) };
                                return true;
                            }
                            return false;
                        };

                    //Запрос билета, на  доступ к детальной информации списка трэков
                    result = client.getTicket(
                          new file
                          {
                              DatePreparation = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                              FileTypeID = "1",
                              FileName = "0",
                              SenderID = "0",
                              RecipientID = "1",
                              FileNumber = "0",
                              Item = trackid_list.Aggregate(new List<item>(), (l, i) =>
                              {
                                  l.Add(new item { Barcode = i });
                                  return l;
                              }).ToArray()
                          }
                          , login, password, "RUS");

                    //Если нет ошибки в ответе
                    //То по номеру выданного билета получим детальную информацию по списку трэков
                    if (!f_error(result) && result is string)
                    {                        
                        result = client.getResponseByTicket(result as string, (args as string[])[0], (args as string[])[1]);
                        if (!f_error(result)&&result is item)
                        {
                            item item = result as item;
                            //Вернем результат
                            return 
                            new
                            {
                                Exception = string.Empty, //Ошибка не указана
                                //Список операции
                                Operations = item.Operation.Aggregate(new List<object>(),
                                                        (a, o) =>
                                                        {
                                                            a.Add(new { TrackID = item.Barcode, Date = o.DateOper, ID = o.IndexOper, Name = o.OperName, Category = _get_oper_category(o.OperCtgID) });
                                                            return a;
                                                        }
                                                     ).ToArray(),
                                //Список ошибок
                                Errors = item.Error.Aggregate(new List<object>(),
                                                       (a, e) =>
                                                       {
                                                           a.Add(new { TrackID = item.Barcode, Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), ID = e.ErrorTypeID, Name = e.ErrorName });
                                                           return a;
                                                       }).ToArray()
                            };                         
                        }
                    }

                    return result;
                }), new string[] { login, password });
                

        
        }
    }
}
