﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EmployeeApp.Models;
using EmployeeApp.Models.ServiceRequests;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EmployeeApp.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class menuPage : ContentPage
    {
        public menuPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();

            
        }

        /// <summary>
        /// Called upon the page appearing. Gets most recent menu items and populates the list with category buttons
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await UpdateMenuItems();
            PopulateCategories();
        }

        /// <summary>
        /// Pulls the most recent list of menu items from the remote database
        /// </summary>
        /// <returns></returns>
        async Task UpdateMenuItems()
        {
            RealmManager.RemoveAll<MenuItemsList>();
            var success = await GetMenuItemsRequest.SendGetMenuItemsRequest();
            
        }

        /// <summary>
        /// Creates the list of categories from the menu items pulled from the remote database. Assumes at least one category exists, but shouldn't break if there aren't any
        /// </summary>
        void PopulateCategories()
        {
            // Clear existing categories
            while(categoryList.Children.Count != 0)
                categoryList.Children.RemoveAt(0);


            List<string> categoryNames = new List<string>();

            // Collect all category names
            foreach(MenuFoodItem m in RealmManager.All<MenuItemsList>().FirstOrDefault().menuItems)
            {
                categoryNames.Add(m.category);
            }

            categoryNames = categoryNames.Distinct().ToList(); // Get only unique categories

            // Create buttons for each category
            for (int i = 0; i < categoryNames.Count; ++i)
            {
                Button temp;
                string buttonName = categoryNames[i];

                categoryList.Children.Add(temp = (new Button()
                {
                    Text = buttonName,
                    Margin = new Thickness(30, 0, 30, 20),
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.White,
                    WidthRequest = 140,
                    BackgroundColor = Color.FromHex("1fbd85"),
                    CornerRadius = 15
                }));

                temp.Clicked += async (sender, args) => await Navigation.PushAsync(new categoryPage(buttonName));
            }
        }


        async void OnRefillButtonClicked(object sender, EventArgs e)
        {
          
        }

        async void OnServerButtonClicked(object sender, EventArgs e)
        {
           
        }
    }
}