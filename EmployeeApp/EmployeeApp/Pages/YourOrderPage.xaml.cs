﻿using EmployeeApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using EmployeeApp.Models.ServiceRequests;

namespace EmployeeApp.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class YourOrderPage : ContentPage
    {
        public YourOrderPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();

            // Necessary for the refreshview to work
            System.Windows.Input.ICommand cmd = new Command(onRefresh);
            orderRefreshView.Command = cmd;

            
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await DisplayOrder();
        }

        async void OnSendOrderClicked(object sender, EventArgs e)
        {
            //Navigate to order confirmation page
            if (await DisplayAlert("WARNING: Sending Order", "Are you sure you want to send the order? Current items cannot be changed by anyone at the table.", "Yes", "No"))
            {
                // Set order status to 'sent'
                await SendOrderRequest.SendSendOrderRequest(RealmManager.All<Order>().FirstOrDefault()._id);

                await Navigation.PushAsync(new YourOrderPage());
            }
        }

        // Replaces send order button after order is sent
        async void OnPayClicked(object sender, EventArgs e)
        {
            await DisplayAlert("All Items Sent", "All items have already been sent to the kitchen", "Ok");
        }

        async void OnAddItemClicked(object sender, EventArgs e)
        {
            //Navigate to menu

            await Navigation.PushAsync(new menuPage());
        }

        async void OnTableButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TablePage());


        }

        async void OnAlertButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new alertPage());

        }

        /// <summary>
        /// Only called by the swipeView Edit button. Allows user to change the special instructions for the selected item if the order is not yet sent
        /// This function will first pull the most recent order status, then, if the instructions have been changed, update the remote database with the new instructions
        /// If the item has been removed from the order before the instructions were input, an error message will be displayed and the remote order will not be updated
        /// Lastly, pulls the most recent order from the server again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void OnEditItemInvoked(object sender, EventArgs e)
        {
            OrderItem item = new OrderItem((OrderItem)(((SwipeItem)sender).BindingContext));

            await DisplayOrder();

            // Don't allow edits for sent items. Might change this to check if the item is prepared later
            if (RealmManager.All<Order>().FirstOrDefault().send_to_kitchen)
            {
                await DisplayAlert("Option Unavailable", "Sorry, but this option is not available since the order has already been sent", "OK");
                return;
            }
            
            string instructions = await DisplayPromptAsync("Special Instructions", "Enter special instructions, such as allergen information", "OK", "Cancel", null, -1, keyboard: Keyboard.Plain, item.special_instruct);
            
            // Don't send request if nothing changed
            if (instructions == item.special_instruct)
                return;

            OrderItem preUpdateItem = RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => o.special_instruct == item.special_instruct && o._id == item._id).FirstOrDefault();

            if (preUpdateItem != null)
            {
                RealmManager.Write(() => preUpdateItem.special_instruct = instructions);

                await UpdateOrderMenuItemsRequest.SendUpdateOrderMenuItemsRequest(RealmManager.All<Order>().FirstOrDefault()._id, RealmManager.All<Order>().FirstOrDefault().menuItems.ToList());
            }
            else
            {
                await DisplayAlert("Something went wrong", "Sorry, but we were unable to edit that item's description. Most likely, it was removed by another user at your table. Please verify if the item is gone, then re-add it to your order", "OK");
            }
            await DisplayOrder();
        }

        /// <summary>
        /// Only called by the Delete swipeView item. Removes the selected item from the order if the order is not yet sent
        /// Pulls the most recent update for the order, removes the item if it still exists, then updates the remote order.
        /// Lastly, pulls the most recent order from the server again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void OnRemoveItemInvoked(object sender, EventArgs e)
        {
            OrderItem item = new OrderItem((OrderItem)(((SwipeItem)sender).BindingContext));

            await DisplayOrder();

            // Don't allow removal of sent items. Might change this to check if the item is prepared later
            if (RealmManager.All<Order>().FirstOrDefault().send_to_kitchen)
            {
                Order myOrder = new Order();
                OrderItem myItem = new OrderItem();
                bool select = await DisplayAlert("Are You Sure?", "The order has already been sent, this will be a void", "Yes", "No");
                if (select)
                {
                    
                   string reason = await DisplayPromptAsync("Reson For Void", "Why are you voiding this item?", "OK", "Cancel", null, -1, keyboard: Keyboard.Plain, item.special_instruct);
                    var resp = AddCompRequest.SendAddCompRequest(reason, item._id, RealmManager.All<Employee>().FirstOrDefault()._id);
                    myOrder = RealmManager.All<Order>().FirstOrDefault();
                    for (int i = 0; i < myOrder.menuItems.Count(); i++)
                    {
                        if (myOrder.menuItems[i]._id == item._id)
                        {
                            RealmManager.Write(() =>
                            {
                                myOrder.menuItems[i].paid = true;
                            });

                            await Task.Delay(50);
                        }
                    }

                    await Task.Delay(50);
                    var respo = UpdateOrderMenuItemsRequest.SendUpdateOrderMenuItemsRequest(myOrder._id, RealmManager.All<Order>().FirstOrDefault().menuItems);
                    
                    return;
                }
                else
                {
                    return;
                }
            }

            OrderItem preUpdateItem = RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => o.special_instruct == item.special_instruct && o._id == item._id).FirstOrDefault();
            
            // Remove item if it was not already removed
            if(preUpdateItem != null)
            {
                RealmManager.Write(() => RealmManager.All<Order>().FirstOrDefault().menuItems.Remove(preUpdateItem));

                await UpdateOrderMenuItemsRequest.SendUpdateOrderMenuItemsRequest(RealmManager.All<Order>().FirstOrDefault()._id, RealmManager.All<Order>().FirstOrDefault().menuItems.ToList());
            }


            await DisplayOrder();
        }


        /// <summary>
        /// Called every time the list of order items (orderRefreshView) is refreshed by pulling down
        /// </summary>
        async void onRefresh()
        {
            // Pull newest order status
            await DisplayOrder();

            orderRefreshView.IsRefreshing = false;
        }

        /// <summary>
        /// Gets the most recent version of the order, sets the menuFoodItemsView's ItemsSource property to that order, and turns the Send Order button into a Payment button if the order has been sent
        /// </summary>
        /// <returns></returns>
        public async Task DisplayOrder()
        {
            
            
           await GetOrderRequest.SendGetOrderRequest(RealmManager.All<Order>().FirstOrDefault()._id);

            // Fetch most recent version of the order

            var ihatethis = new List<OrderItem>();
            ihatethis = RealmManager.All<Order>().FirstOrDefault().menuItems.ToList();

            menuFoodItemsView.ItemsSource = ihatethis;

            if (RealmManager.All<Order>().FirstOrDefault().send_to_kitchen)
            {
                sendOrderButton.Clicked -= OnSendOrderClicked;

                // Add new functionality
                sendOrderButton.Clicked += OnPayClicked;             
            }
        }
    }

}