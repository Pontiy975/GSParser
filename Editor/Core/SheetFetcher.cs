using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace GSParser.Editor.Core
{
    public static class SheetFetcher
    {
        public static async Task<GoogleSheetResponse> FetchAsync(SheetConnection connection)
        {
            using var request = UnityWebRequest.Get(connection.BuildURL());

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"Request failed: {request.error}");

            if (request.responseCode != 200)
                throw new Exception($"Google Sheets API error ({request.responseCode}):\n{request.downloadHandler.text}");

            var response = JsonConvert.DeserializeObject<GoogleSheetResponse>(request.downloadHandler.text);

            if (response?.values == null || response.values.Count == 0)
                throw new Exception("Google Sheet is empty or returned no values.");

            return response;
        }
    }
}