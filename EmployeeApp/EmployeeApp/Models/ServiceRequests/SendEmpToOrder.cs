﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeApp.Models.ServiceRequests
{
    public class SendEmpToOrder : ServiceRequest
    {
        public override string Url { get; }
        public override HttpMethod Method => HttpMethod.Put;
        public IList<UpdateOrderWithUser> Body;

        public SendEmpToOrder(string orderId, string empId)
        {
            Url = "https://dijkstras-steakhouse-restapi.herokuapp.com/orders/" + orderId;

            UpdateOrderWithUser UpdateUserRequestObject = new UpdateOrderWithUser
            {
                propName = "employee_id",
                value = empId,
            };
            Body = new List<UpdateOrderWithUser>();
            Body.Add(UpdateUserRequestObject);
        }

        public static async Task<bool> SendEmpRequest(string orderId, string empId)
        {
            var senduser = new SendEmpToOrder(orderId, empId);
            var response = await ServiceRequestHandler.MakeServiceCall<DeleteResponse>(senduser, senduser.Body);

            if (response == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class UpdateOrderWithUser
    {
        public string propName { get; set; }
        public string value { get; set; }
    }
}