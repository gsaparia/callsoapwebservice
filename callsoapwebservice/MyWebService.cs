using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml.Linq;

public class MyWebService
{
    public string Url { get; set; }
    public string MethodName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string SOAPAction { get; set; }
    public string WSSPasswordType { get; set; }
    public Dictionary<string, string> Params = new Dictionary<string, string>();
    public XDocument ResultXML;
    public string ResultString;

    public MyWebService()
    {
    }

    public MyWebService(string url, string methodName, string userName, string password, string soapAction, string wssPasswordType)
    {
        Url = url;
        MethodName = methodName;
        UserName = userName;
        Password = password;
        SOAPAction = soapAction;
        WSSPasswordType = wssPasswordType;
    }

    /// <summary>
    /// Invokes service
    /// </summary>
    public void Invoke()
    {
        Invoke(true);
    }

    /// <summary>
    /// Invokes service
    /// </summary>
    /// <param name="encode">Added parameters will encode? (default: true)</param>
    public void Invoke(bool encode)
    {
        string phrase = Guid.NewGuid().ToString();
        string tempPhrase = phrase.Replace("-", "");
        tempPhrase = tempPhrase.ToUpper();
        string userNameToken = "UsernameToken-" + tempPhrase;
        DateTime created = DateTime.Now;
        string createdStr = created.ToString("yyyy-MM-ddThh:mm:ss.fffZ");
        SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();
        byte[] hashedDataBytes = sha1Hasher.ComputeHash(Encoding.UTF8.GetBytes(phrase));
        string nonce = Convert.ToBase64String(hashedDataBytes);
        string soapStr = "";

        if (WSSPasswordType == "PasswordText")
        {
            soapStr =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
                    <soap:Header>
                        <wsse:Security soap:mustUnderstand=""true""
                                xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
                                xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
                            <wsse:UsernameToken wsu:Id=""";
            soapStr += userNameToken;
            soapStr += @""">
                            <wsse:Username>" + UserName + @"</wsse:Username>
                            <wsse:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"">" + Password + @"</wsse:Password>
                            <wsse:Nonce EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"">" + nonce + @"</wsse:Nonce>
                            <wsu:Created>" + createdStr + @"</wsu:Created>
                            </wsse:UsernameToken>
                        </wsse:Security>
                    </soap:Header>
                    <soap:Body>
                    <{0}>
                        {1}
                    </{0}>
                    </soap:Body>
                </soap:Envelope>";
        }
        else if (WSSPasswordType == "None")
        {
            soapStr =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
                    <soap:Body>
                    <{0} xmlns=""{2}"">
                        {1}
                    </{0}>
                    </soap:Body>
                </soap:Envelope>";
        }

        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
        req.Headers.Add("SOAPAction", SOAPAction);
        req.ContentType = "application/soap+xml;charset=\"utf-8\"";
        req.Accept = "application/soap+xml";
        req.Method = "POST";

        if (WSSPasswordType == "None")
        {
            NetworkCredential netCredential = new NetworkCredential(UserName, Password);
            byte[] credentialBuffer = new UTF8Encoding().GetBytes(UserName + ":" + Password);
            string auth = Convert.ToBase64String(credentialBuffer);
            req.Headers.Add("Authorization", "Basic " + auth);
        }

        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(
            delegate (
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
            {
                return true;
            });

        using (Stream stm = req.GetRequestStream())
        {
            string postValues = "";
            foreach (var param in Params)
            {
                if (encode)
                {
                    postValues += string.Format("<{0}>{1}</{0}>", HttpUtility.UrlEncode(param.Key), HttpUtility.UrlEncode(param.Value));
                }
                else
                {
                    postValues += string.Format("<{0}>{1}</{0}>", param.Key, param.Value);
                }
            }

            soapStr = string.Format(soapStr, MethodName, postValues,SOAPAction);
            using (StreamWriter stmw = new StreamWriter(stm))
            {
                stmw.Write(soapStr);
            }
        }

        using (StreamReader responseReader = new StreamReader(req.GetResponse().GetResponseStream()))
        {
            string result = responseReader.ReadToEnd();
            ResultXML = XDocument.Parse(result);
            ResultString = result;
        }
    }
}