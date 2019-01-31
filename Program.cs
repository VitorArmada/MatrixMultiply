using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Echovoice.JSON;
using Newtonsoft.Json.Linq;

namespace ConsoleApp4
{
    class Program
    {
        static double[][] MatrixCreate(int size)
        {
            double[][] result = new double[size][];
            for (int i = 0; i < size; ++i)
                result[i] = new double[size];
            return result;
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static async Task<string> InitColumns(int size)
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                client.BaseAddress = new Uri("https://recruitment-test.investcloud.com/api/numbers/init/");
                HttpResponseMessage response = client.GetAsync(size.ToString()).Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                return await Task.FromResult(result);
            }
        }

        public static async Task<JToken[]> GetRowOrCol(string colrow, string type, int value)
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                client.BaseAddress = new Uri("https://recruitment-test.investcloud.com/api/numbers/" + type + "/"+ colrow +"/");
                HttpResponseMessage response = client.GetAsync(value.ToString()).Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                JObject jObject = JObject.Parse(result);
                var jToken = jObject.GetValue("Value").ToString();
                var res = JArray.Parse(jToken).ToArray();
                return await Task.FromResult(res);
            }
        }

        public static async Task<string> PostHash(string md5hash)
        {
            using (var client = new HttpClient(new HttpClientHandler
                {AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate}))
            {
                var response = await client.PostAsync(
                    "https://recruitment-test.investcloud.com/api/numbers/validate", new StringContent(md5hash));

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                return await Task.FromResult(content);
            }
        }

        static void Main(string[] args)
        {
            var size = 3;

            double[][] resultMatrix = MatrixCreate(size);

            // Initiate Matrix A and B
            var initresponse = InitColumns(size);

            Parallel.For(0, size, i =>
            {
                var colA = GetRowOrCol("row", "A", i).Result; //each row of A

                for (int j = 0; j < size - 1; ++j) // each col of B
                {
                    var colB = GetRowOrCol("col", "B", j).Result;
                    
                    for (int k = 0; k < size - 1; ++k) 
                    {
                        resultMatrix[i][j] +=
                            colA[k].ToObject<int>() *
                            colB[k].ToObject<int>(); 
                    }
                }
            });

            string res = "";
            foreach (var row in resultMatrix)
            {
                foreach (var value in row)
                {
                    res = res + value.ToString();
                }
                
            }

            var md5hash = CreateMD5(res);

            var finalresult = PostHash(md5hash);
            Console.WriteLine(finalresult);
        }
    }
}
