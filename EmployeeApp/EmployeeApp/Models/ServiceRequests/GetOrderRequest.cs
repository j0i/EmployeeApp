﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeApp.Models.ServiceRequests
{
    class GetOrderRequest : ServiceRequest
    {
        string orderID;
        //the endpoint we are trying to hit
        public override string Url => "https://dijkstras-steakhouse-restapi.herokuapp.com/orders/" + orderID;
        //the type of request
        public override HttpMethod Method => HttpMethod.Get;
        //headers if we ever need them

        GetOrderRequest(string ID)
        {
            orderID = ID;
        }

        public static async Task<bool> SendGetOrderRequest(string ID)
        {
            //make a new request object
            var serviceRequest = new GetOrderRequest(ID);
            //get a response
            var response = await ServiceRequestHandler.MakeServiceCall<Order>(serviceRequest);

            if(response == null)
            {
                //call failed
                return false;
            }
            else
            {
                // Add the response into the local database

                // Remove current contents
                RealmManager.RemoveAll<Order>();
                RealmManager.RemoveAll<OrderItem>();

                // Assign each order item a unique ID
                Random rand = new Random();
                for(int i = 0; i < ((Order)response).menuItems.Count(); ++i)
                {
                    ((Order)response).menuItems[i].newID = rand.Next(0, 1000000000).ToString();
                    while (RealmManager.Find<OrderItem>((((Order)response).menuItems[i].newID)) != null)
                    {
                        ((Order)response).menuItems[i].newID = rand.Next(0, 1000000000).ToString();
                    }
                }
                RealmManager.AddOrUpdate<Order>(response);
                //call succeeded
                return true;
            }
        }
    }
}
