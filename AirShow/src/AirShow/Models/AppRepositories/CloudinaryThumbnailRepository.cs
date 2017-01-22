using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Common;
using AirShow.Models.EF;
using AirShow.Models.Interfaces;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net;
using Microsoft.Net.Http.Headers;

namespace AirShow.Models.AppRepositories
{
    internal class CloudinaryClient
    {
        private static HttpClient client = new HttpClient();
        private ApiParams _apiParams;
        private static SHA1 sha1 = SHA1.Create();

        internal static ApiParams DefaultParams = new ApiParams
        {
            cloudName = "personalairshow",
            ApiKey = "348885415398134",
            ApiSecret = "HpnhRMdAyOnJZdGei5gX8xmGxLI"
        };

        internal class ApiParams
        {
            public string ApiKey { get; set; }
            public string ApiSecret { get; set; }
            public string cloudName { get; set; }
        }

        internal CloudinaryClient(ApiParams apiParams)
        {
            _apiParams = apiParams;
        }


        internal async Task<OperationStatus> UploadStream(Stream stream, string publicId)
        {

            byte[] buffer = ReadFully(stream);
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string signature = GenerateSignature(publicId, timestamp, _apiParams.ApiSecret);

            MultipartFormDataContent form = new MultipartFormDataContent();

            form.Add(new StringContent(_apiParams.ApiKey), "api_key", "api_key");
            //form.Add(new StringContent(timestamp + ""), "timestamp", "timestamp");
            form.Add(new StringContent(signature), "signature", "signature");

            form.Add(new ByteArrayContent(buffer), "file", "file");
            //form.Add(new StringContent("nyqyjw2b"), "upload_preset");
            form.Add(new StringContent(publicId + ".pdf"), "public_id");
            

            var uri = "https://api.cloudinary.com/v1_1/" + _apiParams.cloudName + "/raw/upload";
            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
                requestMessage.Headers.Add("X-Requested-With", "XMLHttpRequest");
                requestMessage.Headers.Add("User-Agent", "CloudinaryiOS/10.1");
                requestMessage.Content = form;

                var response = await client.SendAsync(requestMessage);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    return new OperationStatus
                    {
                        ErrorMessageIfAny = "Failed with server response " + stringResponse
                    };
                }
            } catch(Exception e)
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = "An error ocurred while generating the thumbnail"
                };
            }
            

            return new OperationStatus();
        }

        internal string URLFor(string publicId)
        {
            return "http://res.cloudinary.com/" + _apiParams.cloudName + "/image/upload/w_200,h_250,c_fill/" + publicId + ".png";
        }

        private static string GenerateSignature(string publicId, long timestamp, string apiSecret)
        {
            var stringToBeEncoded = "public_id=" + publicId + "&timestamp=" + timestamp + "" + apiSecret;
           
            var bytes = Encoding.ASCII.GetBytes(stringToBeEncoded);
            var hash = sha1.ComputeHash(bytes);

            return Encoding.ASCII.GetString(hash);
        }

        private static byte[] ReadFully(Stream input)
        {
            input.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }


    }

    public class CloudinaryThumbnailRepository : IPresentationThumbnailRepository
    {

        private static CloudinaryClient _client = new CloudinaryClient(CloudinaryClient.DefaultParams); 

        public async  Task<OperationStatus> AddThumbnailFor(Presentation p, Stream fileStream)
        {
            var publicId = CreatePublicIdFrom(p);
            return await _client.UploadStream(fileStream, publicId);
        }

        public async Task<OperationResult<string>> GetThumbnailURLFor(Presentation p)
        {
            var publicId = CreatePublicIdFrom(p);
            var url = _client.URLFor(publicId);
            return new OperationResult<string>
            {
                Value = url
            };
        }

        private static string CreatePublicIdFrom(Presentation p)
        {
            return p.Name + p.UploadedDate.Ticks;
        }
    }
}
