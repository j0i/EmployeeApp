﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using EmployeeApp.Models;
using EmployeeApp.Models.ServiceRequests;

namespace EmployeeApp.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class paymentPage : ContentPage
    {
        static double total;

        public paymentPage(double contribution, double tip)
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();

            confirmPayButton.IsVisible = false;

            total = contribution + tip;
        }

        /// <summary>
        /// Activates the Card.IO scanner, then swaps the visibility of the Scan Card button with that of the Confirm Payment button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void scanCard(object sender, EventArgs e)
        {
            DependencyService.Get<ICard>().StartRead();

            // Give the card reader time to come up
            await System.Threading.Tasks.Task.Delay(1000);

            scanCardButton.IsVisible = false;

            // Add button to confirm order if it was not already added
            confirmPayButton.Text = "Confirm Payment of " + total.ToString("C");
            confirmPayButton.IsVisible = true;

        }

        /// <summary>
        /// Becomes visible after the Card.IO scanner appears.
        /// If a valid card has been provided, allows the user to confirm their payment, then leave
        /// If no valid card has been read, swaps visibility with the Scan Card button
        /// If a valid card is read but the user denies confirmation, shows both buttons to allow a rescan or confirm the scanned card
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void confirmButton(object sender, EventArgs e)
        {
            if (DependencyService.Get<ICard>().ReadSuccesful())
            {
                if (await DisplayAlert("Card Details", DependencyService.Get<ICard>().GetCardName() + "\n"
                    + DependencyService.Get<ICard>().GetCardNum() + "\n"
                    + DependencyService.Get<ICard>().GetCardCvv() + "\n"
                    + "Valid expiry? " + (DependencyService.Get<ICard>().IsExpiryValid() ? "Yes" : "No"), "Confirm", "Cancel"))
                {
                    await DisplayAlert("Confimed", "Payment confirmed", "OK");

                    await LeavePage();
                    return;
                }
                else
                {
                    scanCardButton.IsVisible = true;
                    confirmPayButton.IsVisible = true;
                }
            }
            else
            {
                await DisplayAlert("Error", "Couldn't read card data, please try to scan your card again", "OK");
                scanCardButton.IsVisible = true;
                confirmPayButton.IsVisible = false;
            }
        }

        /// <summary>
        /// Sends notification to the waitstaff of the amount to be collected, then allows the user to leave the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void payWithCashClicked(object sender, EventArgs e)
        {
            // Send notification to waitstaff

            await DisplayAlert("Cash Payment", "Your server is on their way to collect your cash payment", "OK");

            await LeavePage();
        }

        /// <summary>
        /// Leave the current page. Unlocks the user's payment-in-progress attribute, canceling the persistence to this page.
        /// Prompts users if they wish to play a game of chance
        /// Will add points to their account eventually
        /// </summary>
        /// <returns></returns>
        async Task LeavePage()
        {
            // Remove previous page to prevent double payment
            if(Navigation.NavigationStack.Count() > 1)
                Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count() - 2]);


            // Add points


            // Unlock user from payment page
            RealmManager.Write(() =>
            {
                RealmManager.All<User>().FirstOrDefault().paymentInProgress = false;
                RealmManager.All<User>().FirstOrDefault().contribution = 0;
                RealmManager.All<User>().FirstOrDefault().tip = 0;
            });

            // Offer game
           
                Navigation.InsertPageBefore(new endPage(), this);
            
            await Navigation.PopAsync();
        }

        async void OnRefillButtonClicked(object sender, EventArgs e)
        {
           
        }

        async void OnServerButtonClicked(object sender, EventArgs e)
        {
            
        }

        // Prevent going back to previous pages, as the order has already been sent. Must continue and pay
        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}