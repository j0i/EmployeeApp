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
    public partial class checkoutPage : ContentPage
    {
        double contribution = 0, tip = 0;

        public checkoutPage()
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void OnPayButtonClicked(object sender, EventArgs e)
        {
            if ((contribution + tip) > 0)
            {
                if (await DisplayAlert("Confirm", "You will not be able to change this contribution after leaving this screen. Continue to payment screen?", "OK", "Cancel"))
                {
                    // Pull most recent order status form database
                    List<string> paidForIDs = new List<string>();

                    foreach (OrderItem o in RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => o.paid))
                        paidForIDs.Add(o._id);

                    await GetOrderRequest.SendGetOrderRequest(RealmManager.All<Order>().FirstOrDefault()._id);

                    List<string> alreadyPaidIDs = new List<string>();
                    foreach (OrderItem o in RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => o.paid))
                        alreadyPaidIDs.Add(o._id);

                    foreach(string ID in alreadyPaidIDs)
                    {
                        var index = paidForIDs.IndexOf(ID);
                        if(index != -1)
                        {
                            paidForIDs.RemoveAt(index);
                        }
                    }

                    // Do something with coupons
                    // 

                    

                    double newContribution = 0;
                    foreach(string ID in paidForIDs)
                    {
                        newContribution += (RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => o._id == ID && !o.paid).FirstOrDefault()).price;
                        RealmManager.Write(() => RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => o._id == ID && !o.paid).FirstOrDefault().paid = true);
                    }

                    

                    // Update items paid for in database
                    await UpdateOrderMenuItemsRequest.SendUpdateOrderMenuItemsRequest(RealmManager.All<Order>().FirstOrDefault()._id, RealmManager.All<Order>().FirstOrDefault().menuItems.ToList());

                    

                    // Lock in user's payment status
                    RealmManager.Write(() =>
                    {
                        RealmManager.All<User>().FirstOrDefault().contribution = newContribution;
                        RealmManager.All<User>().FirstOrDefault().tip = tip;
                        RealmManager.All<User>().FirstOrDefault().paymentInProgress = true;
                    });

                    // Send tip to server

                    if (Math.Abs(newContribution - contribution) >= 0.01)
                        await DisplayAlert("Notice", "Due to coupons or other items already being paid for, your payment has been changed to " + newContribution.ToString("C") + " plus your tip of " + tip.ToString("C"), "OK");

                    await Navigation.PushAsync(new paymentPage(newContribution, tip));
                }
            }
            else // No contribution
                await Navigation.PushAsync(new endPage());
        }


        // Clear the tip's entry every time it is selected
        void clearTip(object sender, EventArgs e)
        {
            ((Entry)sender).Text = "";
        }

        /// <summary>
        /// Called upon the tip entry box being deselected.
        /// Rounds the tip to two decimal places (currency) if it is a valid entry (non-negative numbers)
        /// Otherwise, sets tip to 0
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnTipCompleted(object sender, EventArgs e)
        {
            // Sanity check inputs
            if (!double.TryParse(((Entry)sender).Text, out tip) || tip < 0)
                tip = 0;
            else
                tip = double.Parse(((Entry)sender).Text, System.Globalization.NumberStyles.Currency);

            // Round to two decimal places, then update the textboxes
            tip = Math.Round(tip, 2);

            tipEntry.Text = tip.ToString("C");

            OnContributionCompleted();
        }

        /// <summary>
        /// Updates labels according to contribution and tip sum
        /// Also uses placeholder text to suggest a tip
        /// </summary>
        void OnContributionCompleted()
        {
            // Suggest tip through placeholder text
            tipEntry.Placeholder = (contribution * 0.2).ToString("C");

            // Update buttons
            if ((contribution + tip) > 0)
                payButton.Text = "Pay " + (contribution + tip).ToString("C");
            else
                payButton.Text = "No Contribution";
        }


        async void OnAlertButtonClicked(object sender, EventArgs e)
        {
            // Send refill request

        }
        async void OnServerButtonClicked(object sender, EventArgs e)
        {
            // Send Help Request
           
        }

        /// <summary>
        /// Pulls the most recent order status, then assigns that to the items list's itemsSource
        /// Also resets contribution to 0
        /// </summary>
        /// <returns></returns>
        public async Task DisplayOrder()
        {
            orderRefreshView.IsEnabled = false;

            // Fetch most recent order status
            await GetOrderRequest.SendGetOrderRequest(RealmManager.All<Order>().FirstOrDefault()._id);

            contribution = 0;

            OnContributionCompleted();

            menuFoodItemsView.ItemsSource = RealmManager.All<Order>().FirstOrDefault().menuItems.Where((OrderItem o) => !o.paid).ToList();

            orderRefreshView.IsEnabled = true;
        }

        /// <summary>
        /// Called upon toggling a switch.
        /// Note that the toggle itself is tied to an order item's paid attribute, so we do not need ot manually change it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void OnTogglePaid(object sender, ToggledEventArgs e)
        {
            // Don't do anything if Realm is writing
            if (RealmManager.Realm.IsInTransaction)
                return;

            OrderItem toggledItem = new OrderItem((OrderItem)(((ViewCell)(((Switch)sender).Parent.Parent.Parent)).BindingContext));

            // Error checking
            if (toggledItem._id == null)
            {
                await DisplayOrder();
                return;
            }
                
               
            // Update contribution according to toggled/not toggled
            if (e.Value)
            {
                contribution += toggledItem.price;
                contribution = Math.Round(contribution, 2);
            }
            else
            {
                contribution -= toggledItem.price;
                contribution = Math.Round(contribution, 2);
                // Mainly need this because onAppearing seems to toggle off, even if the switch already was off
                if (contribution < 0)
                    contribution = 0;
            }

            OnContributionCompleted();
        }

        async void onRefresh()
        {
            // Pull newest order status
            await DisplayOrder();

            orderRefreshView.IsRefreshing = false;
        }
    }
}