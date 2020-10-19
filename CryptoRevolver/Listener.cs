using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRevolver
{

    public static class ResponseStatus
    {
        public static string Success = "Success";
        public static string Error = "Error";
    }
    public class Listener
    {
        string url = "http://+";
        string port = "9646";
        string prefix;
        HttpListener listener;
        Dictionary<string, BaseController> controllers = new Dictionary<string, BaseController>();
        Service Service;

        public Listener()
        {
            prefix = String.Format("{0}:{1}/", url, port);
            Service = new Service();
        }

        public void Start()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Error Start Listener");
                return;
            }

            Type controllersTypes = typeof(BaseController);
            IEnumerable<Type> list = Assembly.GetAssembly(controllersTypes).GetTypes().Where(type => type.IsSubclassOf(controllersTypes));
            object[] seviceObject = new object[1];
            seviceObject[0] = Service;
            foreach (Type itm in list)
            {
                //получаем конструктор
                ConstructorInfo ci = itm.GetConstructor(new Type[] { typeof(Service) });

                //вызываем конструтор
                object Obj = ci.Invoke(seviceObject);
                
                controllers.Add(itm.Name, Obj as BaseController);
            }

            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            listener.Start();

            Console.WriteLine("Listening on {0}...", prefix);

            while (true)
            {
                // Ожидание входящего запроса
                HttpListenerContext context = listener.GetContext();

                Task.Factory.StartNew(() => {
                    // Объект запроса
                    HttpListenerRequest request = context.Request;

                    // Объект ответа
                    HttpListenerResponse response = context.Response;

                    // Проверяем тип запроса и достаем параметры в JSON
                    string jsonParams = "";
                    if (request.Headers.Get("Content-Type").Contains("multipart/form-data"))
                    {
                        jsonParams = JsonConvert.SerializeObject(NvcToDictionary(request.QueryString, false));
                        var fileInfo = JsonConvert.DeserializeObject<FileUpload>(jsonParams);
                        SaveFile(context.Request.ContentEncoding, GetBoundary(context.Request.ContentType), context.Request.InputStream, @"eas/" + fileInfo.FileName);
                    }
                    else
                    {
                        if (request.HttpMethod == "POST")
                        {
                            using (var readers = new StreamReader(request.InputStream, request.ContentEncoding))
                            {
                                jsonParams = readers.ReadToEnd();
                            }
                        }
                        else if (request.HttpMethod == "GET")
                        {
                            jsonParams = JsonConvert.SerializeObject(NvcToDictionary(request.QueryString, false));
                        }
                    }
                  

                    Console.WriteLine("{0} request was caught: {1}",
                                       request.HttpMethod, request.Url);

                    // Парсим строку что бы убрать GET параметры
                    var baseUrl = request.Url.ToString().Split('?');
                    var url = baseUrl[0].Split('/');

                    // Если в URL не хватает сегментов, 404
                    if (url.Length < 5)
                    {
                        Send404(response);
                        return;
                    }

                    // Находим нужный контроллер
                    BaseController controller;
                    if (!controllers.TryGetValue(url[3], out controller))
                    {
                        Send404(response);
                        return;
                    }

                    // Забираем методы контроллера
                    Type controllerType = controller.GetType();
                    var method = controllerType.GetMethod(url[4]);

                    if (method == null)
                    {
                        Send404(response);
                        return;
                    }

                    // Проверка на HTTP метод
                    HttpMethodAttribute httpMethodAttribute = method.GetCustomAttribute<HttpMethodAttribute>();

                    // Если у метода нет HttpMethodAttribute атрибута то мы считаем что он обрабатывает GET запросы
                    if (httpMethodAttribute == null && request.HttpMethod != "GET") Send404(response);
                    // Если у метода есть HttpMethodAttribute то проверяем запрос
                    if (httpMethodAttribute != null && httpMethodAttribute.HttpMethod != request.HttpMethod) Send404(response);

                    // Забираем параметры метода
                    var paramsTypeInfo = method.GetParameters();
                    BaseResponse resp = new BaseResponse();

                    // Если у метода есть параметры, а их не передали, то ошибка
                    if (paramsTypeInfo != null && paramsTypeInfo.Length > 0 && (jsonParams == "" || jsonParams == null))
                    {
                        resp.ErrorMessage = "Error params";
                        Send400(response, resp);
                        return;
                    }

                    // Забираем параметры, проверяем, отправляем
                    object[] objectsParam = new object[1];
                    if (paramsTypeInfo != null && paramsTypeInfo.Length > 0)
                    {
                        try
                        {
                            var paramsType = paramsTypeInfo[0].ParameterType;
                            object paramObject = JsonConvert.DeserializeObject(jsonParams, paramsType);

                            // Проверка на доступность метода по сессии
                            HttpSecurityAttribute httpSecurityAttribute = method.GetCustomAttribute<HttpSecurityAttribute>();

                            // Если у метода есть HttpSecurityAttribute то проверяем запрос
                            if (httpSecurityAttribute != null)
                            {
                                UserSession userSession;
                                if ((paramObject as BaseRequest).Token == null || (paramObject as BaseRequest).Token == "")
                                {
                                    resp.ErrorMessage = "Invalid token";
                                    Send400(response, resp);
                                    return;
                                }

                                userSession = Service.CheckSession((paramObject as BaseRequest).Token);

                                if (userSession == null)
                                {
                                    resp.ErrorMessage = "Invalid token";
                                    Send400(response, resp);
                                    return;
                                }

                                (paramObject as BaseRequest).UserSession = userSession;
                            }

                            objectsParam[0] = paramObject;

                        }
                        catch (Exception ex)
                        {
                            resp.ErrorMessage = ex.Message;
                            Send400(response, resp);
                            return;
                        }
                    }

                    // Отправляем запрос в контроллер
                    if (paramsTypeInfo != null && paramsTypeInfo.Length > 0)
                        resp = (BaseResponse)method.Invoke(controller, objectsParam);
                    else
                        resp = (BaseResponse)method.Invoke(controller, null);

                    Send200(response, resp);
                });
            }
        }
        private static String GetBoundary(String ctype)
        {
            return "--" + ctype.Split(';')[1].Split('=')[1];
        }

        private static void SaveFile(Encoding enc, String boundary, Stream input, string path)
        {
            Byte[] boundaryBytes = enc.GetBytes(boundary);
            Int32 boundaryLen = boundaryBytes.Length;

            using (FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                Byte[] buffer = new Byte[1024];
                Int32 len = input.Read(buffer, 0, 1024);
                Int32 startPos = -1;

                // Find start boundary
                while (true)
                {
                    if (len == 0)
                    {
                        throw new Exception("Start Boundaray Not Found");
                    }

                    startPos = IndexOf(buffer, len, boundaryBytes);
                    if (startPos >= 0)
                    {
                        break;
                    }
                    else
                    {
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
                    }
                }

                // Skip four lines (Boundary, Content-Disposition, Content-Type, and a blank)
                for (Int32 i = 0; i < 4; i++)
                {
                    while (true)
                    {
                        if (len == 0)
                        {
                            throw new Exception("Preamble not Found.");
                        }

                        startPos = Array.IndexOf(buffer, enc.GetBytes("\n")[0], startPos);
                        if (startPos >= 0)
                        {
                            startPos++;
                            break;
                        }
                        else
                        {
                            len = input.Read(buffer, 0, 1024);
                        }
                    }
                }

                Array.Copy(buffer, startPos, buffer, 0, len - startPos);
                len = len - startPos;

                while (true)
                {
                    Int32 endPos = IndexOf(buffer, len, boundaryBytes);
                    if (endPos >= 0)
                    {
                        if (endPos > 0) output.Write(buffer, 0, endPos - 2);
                        break;
                    }
                    else if (len <= boundaryLen)
                    {
                        throw new Exception("End Boundaray Not Found");
                    }
                    else
                    {
                        output.Write(buffer, 0, len - boundaryLen);
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen;
                    }
                }
            }
        }

        private static Int32 IndexOf(Byte[] buffer, Int32 len, Byte[] boundaryBytes)
        {
            for (Int32 i = 0; i <= len - boundaryBytes.Length; i++)
            {
                Boolean match = true;
                for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
                {
                    match = buffer[i + j] == boundaryBytes[j];
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Send404(HttpListenerResponse response) 
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            using (Stream stream = response.OutputStream) { }
        }

        public void Send400(HttpListenerResponse response, BaseResponse data)
        {
            data.Status = "Error";
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            using (Stream stream = response.OutputStream)
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        public void Send200(HttpListenerResponse response, BaseResponse data)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json; charset=UTF-8";
            response.ContentEncoding = Encoding.UTF8;
            response.AddHeader("Access-Control-Allow-Origin", "*");

            using (Stream stream = response.OutputStream)
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                stream.Write(buffer, 0, buffer.Length);
            }
            response.Close();
        }

        static Dictionary<string, object> NvcToDictionary(NameValueCollection nvc, bool handleMultipleValuesPerKey)
        {
            var result = new Dictionary<string, object>();
            foreach (string key in nvc.Keys)
            {
                if (handleMultipleValuesPerKey)
                {
                    string[] values = nvc.GetValues(key);
                    if (values.Length == 1)
                    {
                        result.Add(key, values[0]);
                    }
                    else
                    {
                        result.Add(key, values);
                    }
                }
                else
                {
                    result.Add(key, nvc[key]);
                }
            }

            return result;
        }
    }
}
