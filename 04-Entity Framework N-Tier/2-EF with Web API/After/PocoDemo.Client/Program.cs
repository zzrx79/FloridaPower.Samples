﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using AspnetWebApi2Helpers.Serialization;
using AspnetWebApi2Helpers.Serialization.Protobuf;
using PocoDemo.Data;
using WebApiContrib.Formatting;
using System.Net.Http.Headers;

namespace PocoDemo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // Prompt user for media type
            Console.WriteLine("Select media type: {1} Xml, {2} Json, {3} Protobuf");
            int selection = int.Parse(Console.ReadLine());

            // Configure accept header and media type formatter
            MediaTypeFormatter formatter;
            string acceptHeader;
            switch (selection)
            {
                case 1:
                    formatter = new XmlMediaTypeFormatter();
                    ((XmlMediaTypeFormatter)formatter).XmlPreserveReferences
                        (typeof(Category), typeof(List<Product>));
                    acceptHeader = "application/xml";
                    break;
                case 2:
                    formatter = new JsonMediaTypeFormatter();
                    ((JsonMediaTypeFormatter)formatter).JsonPreserveReferences();
                    acceptHeader = "application/json";
                    break;
                case 3:
                    formatter = new ProtoBufFormatter();
                    ((ProtoBufFormatter)formatter).ProtobufPreserveReferences
                        (typeof(Category).Assembly.GetTypes());
                    acceptHeader = "application/x-protobuf";
                    break;
                default:
                    Console.WriteLine("Invalid selection: {0}", selection);
                    return;
            }

            // Create an http client with service base address
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:51245/api/"),
            };

            // Set request accept header
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));

            // Get response
            HttpResponseMessage response = client.GetAsync("customers").Result;
            response.EnsureSuccessStatusCode();

            // Read response content 
            var customers = response.Content.ReadAsAsync<List<Customer>>
                (new[] { formatter }).Result;
            foreach (var c in customers)
            {
                Console.WriteLine("{0} {1} {2}",
                    c.CustomerId,
                    c.CompanyName,
                    c.City);
            }

            // Select a customer
            Console.WriteLine("\nCustomer Id:");
            string customerId = Console.ReadLine();

            // Get customer orders
            response = client.GetAsync("orders?customerId=" + customerId).Result;
            response.EnsureSuccessStatusCode();

            // Read response content
            var orders = response.Content.ReadAsAsync<List<Order>>
                (new[] { formatter }).Result;
            foreach (var o in orders)
            {
                Console.WriteLine("{0} {1}",
                    o.OrderId,
                    o.OrderDate.GetValueOrDefault().ToShortDateString());
                foreach (var od in o.OrderDetails)
                {
                    Console.WriteLine("\t{0} {1} {2} {3}",
                        od.OrderDetailId,
                        od.Product.ProductName,
                        od.Quantity,
                        od.UnitPrice.ToString("C"));
                }
            }

            // Create a new order
            Console.WriteLine("\nPress Enter to create a new order");
            Console.ReadLine();
            var newOrder = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.Today,
                ShippedDate = DateTime.Today.AddDays(1),
                OrderDetails = new List<OrderDetail>
                    {
                        new OrderDetail { ProductId = 1, Quantity = 5, UnitPrice = 10 },
                        new OrderDetail { ProductId = 2, Quantity = 10, UnitPrice = 20 },
                        new OrderDetail { ProductId = 4, Quantity = 40, UnitPrice = 40 }
                    }
            };

            // Post the new order
            response = client.PostAsync<Order>("orders", newOrder, formatter).Result;
            response.EnsureSuccessStatusCode();
            var order = response.Content.ReadAsAsync<Order>(new[] { formatter }).Result;
            PrintOrderWithDetails(order);

            // Update the order
            Console.WriteLine("\nPress Enter to update order details");
            Console.ReadLine();
            order.OrderDate = order.OrderDate.GetValueOrDefault().AddDays(1);

            // Put the updated order
            response = client.PutAsync<Order>("orders", order, formatter).Result;
            response.EnsureSuccessStatusCode();
            order = response.Content.ReadAsAsync<Order>(new[] { formatter }).Result;
            PrintOrderWithDetails(order);

            // Delete the order
            Console.WriteLine("\nPress Enter to delete the order");
            Console.ReadLine();

            // Send delete
            response = client.DeleteAsync("Orders/" + order.OrderId).Result;
            response.EnsureSuccessStatusCode();

            // Verify delete
            response = client.GetAsync("Orders/" + order.OrderId).Result;
            if (!response.IsSuccessStatusCode)
                Console.WriteLine("Order deleted");
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private static void PrintOrderWithDetails(Order o)
        {
            Console.WriteLine("{0} {1}",
                o.OrderId,
                o.OrderDate.GetValueOrDefault().ToShortDateString());
            foreach (var od in o.OrderDetails)
            {
                Console.WriteLine("\t{0} {1} {2} {3}",
                    od.OrderDetailId,
                    od.ProductId,
                    od.Quantity,
                    od.UnitPrice.ToString("c"));
            }
        }
    }
}
