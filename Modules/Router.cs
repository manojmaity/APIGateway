using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using APIGateway.Model;
using APIGateway.Helper;

namespace APIGateway
{
    public class Router
    {
        public List<RouteData> RoutesInfo { get; set; }
        public Destination AuthenticationService { get; set; }

        public Router(string routeConfigFilePath)
        {
            dynamic router = JsonHelper.LoadFromFile<dynamic>(routeConfigFilePath);

            RoutesInfo = JsonHelper.Deserialize<List<RouteData>>(Convert.ToString(router.routes));
            AuthenticationService = JsonHelper.Deserialize<Destination>(Convert.ToString(router.authenticationService));

        }
        public async Task<HttpResponseMessage> RouteRequest(HttpRequest request)
        {
            string path = request.Path.ToString();
            string basePath = '/' + path.Split('/')[1];

            Destination destination;
            try
            {
                destination = RoutesInfo.First(r => r.Endpoint.Equals(basePath)).Destination;
            }
            catch
            {
                return ConstructErrorMessage("The path could not be found.");
            }

            if (destination.RequiresAuthentication)
            {
                string token = request.Headers["token"];
                request.Query.Append(new KeyValuePair<string, StringValues>("token", new StringValues(token)));
                HttpResponseMessage authResponse = await AuthenticationService.SendRequest(request);
                if (!authResponse.IsSuccessStatusCode) return ConstructErrorMessage("Authentication failed.");
            }

            return await destination.SendRequest(request);
        }

        private HttpResponseMessage ConstructErrorMessage(string error)
        {
            HttpResponseMessage errorMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(error)
            };
            return errorMessage;
        }

    }

}
