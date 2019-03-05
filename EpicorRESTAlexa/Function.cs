using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET;
using Amazon.Lambda.Core;
using System.Text;
using System.Data;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace EpicorRESTAlexa
{
    public class Function
    {

        /// <summary>
        /// A simple Amazon Skill Example which uses the Epicor REST API
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            //Get Access to the AWS Lambda Logger to see the status of our request
            var log = context.Logger;

            //Get the Request Type
            var requestType = input.GetRequestType();

            SkillResponse resp = null;

            //This will be said to the user after every response as a way to "reprompt" (continue)
            var reprompt = new Alexa.NET.Response.Reprompt
            {
                OutputSpeech = new PlainTextOutputSpeech() { Text = "For a list of available commands say, Help!. What woudl you like to do?" }
            };

            //When the skill initially launches, give them some helpful info
            if (requestType == typeof(LaunchRequest))
            {
                log.LogLine("INFO: Initial Launch Request");

                var speech = new Alexa.NET.Response.SsmlOutputSpeech()
                {
                    Ssml = string.Format(@"<speak>Welcome to this sample Alexa Skill using the Epicor REST API. For a list of available commands say help. What would you like to do? </speak>")
                };

                resp = ResponseBuilder.Ask(speech, reprompt);
            }
            //If they askef for something specific, figure out what and respond accordingly.
            else if( requestType == typeof(IntentRequest))
            {
                var intent = input.Request as IntentRequest;
                log.LogLine($"INFO: Launched Intent:{intent.Intent.Name}");

                switch(intent.Intent.Name)
                {
                    //If they involved the RunSampleBAQ Intent
                    case "RunSampleBAQ":
                        {
                            var speech = new Alexa.NET.Response.SsmlOutputSpeech();
                            log.LogLine($"INFO: Running BAQ");
                            speech.Ssml = RunSampleBAQ();
                            resp = ResponseBuilder.Ask(speech, reprompt);
                        }
                        break;
                    //If they involved the GetCompanyInfo intent
                    case "GetCompanyInfo":
                        {
                            var speech = new Alexa.NET.Response.SsmlOutputSpeech();
                            log.LogLine($"INFO: Getting Company Info");
                            speech.Ssml = GetCompanyInfo();
                            resp = ResponseBuilder.Ask(speech, reprompt);
                        }
                        break;
                    case "AMAZON.HelpIntent":
                        {
                            try
                            {
                                log.LogLine("Launched Help intent");
                                StringBuilder sbCommandList = new StringBuilder();
                                sbCommandList.AppendLine($"<speak>Available commands are: <break strength='medium' />");
                                sbCommandList.AppendLine($"Run Sample BAQ <break strength='medium' />");
                                sbCommandList.AppendLine($"Get Company Info,<break strength='medium' />");
                                sbCommandList.AppendLine($"What would you like to do?</speak>");
                                log.LogLine("Finished building command List");
                                var speech = new SsmlOutputSpeech()
                                {
                                    Ssml = sbCommandList.ToString()
                                };
                                log.LogLine("Assigned Response");
                                resp = ResponseBuilder.Ask(speech, reprompt);
                            }
                            catch (Exception e)
                            {
                                log.LogLine(e.ToString());
                            }

                        }
                        break;
                    case "AMAZON.StopIntent":
                        {
                            var speech = new SsmlOutputSpeech()
                            {
                                Ssml = "<speak>Thank you for Trying out this Sample Skill<break strength='medium' />. Good bye.</speak>"
                            };
                            resp = ResponseBuilder.Tell(speech);
                        }
                        break;
                    case "AMAZON.CancelIntent":
                        {
                            var speech = new SsmlOutputSpeech()
                            {
                                Ssml = "<speak>Good bye.</speak>"
                            };
                            resp = ResponseBuilder.Tell(speech);
                        }
                        break;
                    case "Unhandled":
                    default:
                        {
                            var speech = new PlainTextOutputSpeech()
                            {
                                Text = "Ut oh, you've somehow ended in an unhandled intent... You shouldn't be here, GET OUT!"
                            };
                            resp = ResponseBuilder.Ask(speech, reprompt);
                        }
                        break;
                }
            }

            return resp;
        }

        public void SetupEpicor()
        {
            EpicorRestAPI.EpicorRest.AppPoolHost = "Your.Server.Tld";
            EpicorRestAPI.EpicorRest.AppPoolInstance = "Epicor10Production";
            EpicorRestAPI.EpicorRest.IgnoreCertErrors = true;
            EpicorRestAPI.EpicorRest.UserName = "user";
            EpicorRestAPI.EpicorRest.UserName = "password";

        }
        /// <summary>
        /// Runs an Epicor Built in BAQ getting the Top Most Result and responding with that information
        /// </summary>
        /// <returns></returns>
        public string RunSampleBAQ()
        {
            StringBuilder respose = new StringBuilder();
            respose.Append("<speak>");
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("$top", "1");
            DataTable dt = EpicorRestAPI.EpicorRest.GetBAQResults("zCustomer01", dic);
            respose.Append($"Customer Name: {dt.Rows[0]["Customer_Name"]}, ");
            respose.Append($"Customer ID: {dt.Rows[0]["Customer_CustID"]} ");
            respose.Append("</speak>");

            return respose.ToString();

        }

        /// <summary>
        /// Gets a list of Companies in Epicor and returns the top most result
        /// </summary>
        /// <returns></returns>
        public string GetCompanyInfo()
        {
            StringBuilder respose = new StringBuilder();
            respose.Append("<speak>");
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("$top", "1");
            dynamic companyData = EpicorRestAPI.EpicorRest.DynamicGet("Erp.BO.CompanySvc", "Companies", dic);
            respose.Append($"Company ID: {companyData.value[0].Company1},");
            respose.Append($"Company Name: {companyData.value[0].Name}.");
            respose.Append("</speak>");

            return respose.ToString();
        }
    }
}
