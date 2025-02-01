using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

public static class ValidaCPF
{
    [Function("ValidaCPF")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string cpf = query["cpf"] ?? string.Empty;

        if (string.IsNullOrEmpty(cpf))
        {
            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            dynamic? data = null;
            if (!string.IsNullOrEmpty(requestBody))
            {
                object? v = JsonConvert.DeserializeObject(requestBody);
                if (v != null)
                {
                    data = v;
                }
                if (data != null)
                {
                    cpf = data.cpf;
                }
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);

        if (string.IsNullOrEmpty(cpf))
        {
            response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new { erro = "CPF nao fornecido" });
            return response;
        }

        bool valido = Validar(cpf);
        if (!valido)
        {
            response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new { erro = "CPF invalido" });
            return response;
        }

        await response.WriteAsJsonAsync(new { valido = "CPF valido" });
        return response;
    }

    private static bool Validar(string cpf)
    {
        // Remove non-numeric characters
        cpf = Regex.Replace(cpf, @"[^\d]", "");

        if (cpf.Length != 11)
            return false;

        // Check for invalid CPF numbers
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Validate CPF
        for (int t = 9; t < 11; t++)
        {
            int sum = 0;
            for (int i = 0; i < t; i++)
                sum += int.Parse(cpf[i].ToString()) * (t + 1 - i);

            int remainder = (sum * 10) % 11;
            if (remainder == 10 || remainder == 11)
                remainder = 0;

            if (remainder != int.Parse(cpf[t].ToString()))
                return false;
        }

        return true;
    }
}
