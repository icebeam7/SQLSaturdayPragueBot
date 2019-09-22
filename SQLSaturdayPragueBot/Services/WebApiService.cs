using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using SQLSaturdayPragueBot.Helpers;
using SQLSaturdayPragueBot.Models;
using System.Collections.Generic;

namespace SQLSaturdayPragueBot.Services
{
    public static class WebApiService
    {
        public static async Task<List<Product_Model>> GetProducts(string productName)
        {
            var query = $"{Constants.WebApiUrl}/Products/{productName}";

            using (var client = new HttpClient())
            {
                var getProducts = await client.GetAsync(query);

                if (getProducts.IsSuccessStatusCode)
                {
                    var json = await getProducts.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<Product_Model>>(json);
                    return products;
                }
            }

            return default(List<Product_Model>);
        }

        public static async Task<Product_Model> GetProduct(int productID)
        {
            var query = $"{Constants.WebApiUrl}/Products/ID/{productID}";

            using (var client = new HttpClient())
            {
                var getProducts = await client.GetAsync(query);

                if (getProducts.IsSuccessStatusCode)
                {
                    var json = await getProducts.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<Product_Model>(json);
                    return product;
                }
            }

            return default(Product_Model);
        }

        public static async Task<CustomerShort> GetCustomer(string email)
        {
            var query = $"{Constants.WebApiUrl}/Customers/{email}";

            using (var client = new HttpClient())
            {
                var getCustomer = await client.GetAsync(query);

                if (getCustomer.IsSuccessStatusCode)
                {
                    var json = await getCustomer.Content.ReadAsStringAsync();
                    var customer = JsonConvert.DeserializeObject<CustomerShort>(json);
                    return customer;
                }
            }

            return default(CustomerShort);
        }
    }
}
