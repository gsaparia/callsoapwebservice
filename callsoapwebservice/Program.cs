using System;

namespace callsoapwebservice
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new MyWebService
            {
                Url = "https://www.dataaccess.com/webservicesserver/NumberConversion.wso",
                MethodName = "NumberToWords",
                SOAPAction = "http://www.dataaccess.com/webservicesserver/",
                UserName = "",
                Password = "",
                WSSPasswordType = "None",
                Params=new System.Collections.Generic.Dictionary<string, string> 
                { 
                    { "ubiNum", "465233534" },                     
                }

            };
            service.Invoke();
        }
    }
    //https://ehikioya.com/web-services-without-adding-reference/
    //https://documenter.getpostman.com/view/8854915/Szf26WHn
    //https://www.dataaccess.com/webservicesserver/NumberConversion.wso

}
