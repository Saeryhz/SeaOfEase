using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace SeaOfEase.SeaOfThieves.Util
{
    public class GeoIP
    {
        public static string[] GetIPLocation(string ipAdress)
        {
            StringBuilder output = new StringBuilder();
            XmlReader xmlReader = XmlReader.Create((TextReader)new StringReader(new WebClient { Proxy = null }.DownloadString(("http://ip-api.com/xml/" + ipAdress))));
            using (XmlWriter.Create(output, new XmlWriterSettings() { Indent = true }))
            {
                try
                {
                    xmlReader.ReadToFollowing("country");
                    string country = xmlReader.ReadElementContentAsString();
                    xmlReader.ReadToFollowing("regionName");
                    string regionName = xmlReader.ReadElementContentAsString();
                    xmlReader.ReadToFollowing("city");
                    string city = xmlReader.ReadElementContentAsString();
                    return new string[] { country, regionName, city };
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
    }
}
