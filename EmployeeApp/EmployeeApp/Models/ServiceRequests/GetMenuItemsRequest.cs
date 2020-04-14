﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeApp.Models.ServiceRequests
{
    class GetMenuItemsRequest : ServiceRequest
    {
        //the endpoint we are trying to hit
        public override string Url => "https://dijkstras-steakhouse-restapi.herokuapp.com/menuItems";
        //the type of request
        public override HttpMethod Method => HttpMethod.Get;
        //headers if we ever need them

        public static async Task<bool> SendGetMenuItemsRequest()
        {
            //make a new request object
            var serviceRequest = new GetMenuItemsRequest();
            //get a response
            var response = await ServiceRequestHandler.MakeServiceCall<MenuItemsList>(serviceRequest);

            if(response == null)
            {
                //call failed
                return false;
            }
            else
            {
                //add the response into the local database
                RealmManager.AddOrUpdate<MenuItemsList>(response);
                //call succeeded
                return true;
            }
        }
    }
}
