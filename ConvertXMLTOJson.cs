using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FunctionApp
{
    public static class ConvertXMLToJson
    {
        private static ILogger logger; 
        [FunctionName("ConvertXMLToJson")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            logger = log;
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            try
            {
                dynamic json = data.json;
                string xml = data.xml;
                XmlDocument xmldocument = new XmlDocument();
                xmldocument.LoadXml(xml);
                string convertedjson = JsonConvert.SerializeXmlNode(xmldocument);
                dynamic convertedjson1 = JsonConvert.DeserializeObject(convertedjson);
                ConvertJson(convertedjson1, "",json, convertedjson1);
                return new OkObjectResult(convertedjson1);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e);
            }
        }

        public static bool isPropertyArray(JToken toConvert, string jsonPath, bool retunsome=false)
        {
            var pattern = @"\[\d+\]";
            jsonPath = Regex.Replace(jsonPath,pattern, "");
            JToken relative = toConvert;
            bool returnvalue = retunsome;
            string[] jsonPathNodes = jsonPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < jsonPathNodes.Length; i++)
            {
                if (relative is JArray)
                {
                    foreach (JToken child in relative.Children())
                    {
                         returnvalue = isPropertyArray(child, string.Join(".", jsonPathNodes.Skip(i)), returnvalue);
                        if(returnvalue)
                        {
                            return returnvalue;
                        }                                             
                    }
                   
                }
                else if(relative is JObject)
                {
                    JToken type = (relative as JObject).Property(jsonPathNodes[i]).Value;
                    relative = type;
                    if (type is JArray)
                    {
                        returnvalue = true;
                    }
                }
            }
            return returnvalue;
        }
        
        public static void ConvertJson(JToken json, string path ,JToken refjson, JToken convertedjson1)
        {

             if (json is JArray)
            {
                foreach (JToken child in json.Children())
                {
                    ConvertJson(child, child.Path, refjson, convertedjson1);
                }
            }
            else if (json is JObject)
            {
                foreach (JProperty key in (json as JObject).Properties())
                {
                    ConvertJson(key.Value,key.Path, refjson, convertedjson1);
                }
               bool some= isPropertyArray(refjson,path);
                var pattern = @"\[\d+\]$";
                if (some && !Regex.IsMatch(path,pattern))
                {
                    ConvertjsonObjecttoArray(convertedjson1, path);
                }
            }
        }

        public static bool ConvertjsonObjecttoArray(JToken toConvert, string jsonPath, bool retunsome = false)
        {
            var pattern = @"\[\d+\]";
            jsonPath = Regex.Replace(jsonPath, pattern, "");
            JToken relative = toConvert;
            bool returnvalue = retunsome;
            string[] jsonPathNodes = jsonPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < jsonPathNodes.Length; i++)
            {
                if (relative is JArray)
                {
                    foreach (JToken child in relative.Children())
                    {
                       ConvertjsonObjecttoArray(child, string.Join(".", jsonPathNodes.Skip(i)), returnvalue);                     
                    }
                }
                else if (relative is JObject)
                {
                    JToken type = (relative as JObject).Property(jsonPathNodes[i]).Value;
                   
                    if(i == jsonPathNodes.Length - 1)
                    {
                        JArray array = new JArray();
                        array.Add((relative as JObject).Property(jsonPathNodes[i]).Value);
                        (relative as JObject).Property(jsonPathNodes[i]).Value = array;
                        relative = type;
                    }
                    else
                    {
                        relative = type;
                    } 
                }
            }
            return returnvalue;
        }
    }

}
