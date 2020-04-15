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
    public partial class categoryPage : ContentPage
    {
        string category;
        List<MenuFoodItem> members;
        List<Ingredient> ingredients; // Used to filter items which we don't have enough ingredients for

        public categoryPage(string categoryName)
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();

            categoryLabel.Text = categoryName;
            category = categoryName;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await GetIngredientsRequest.SendGetIngredientsRequest();
            ingredients = RealmManager.All<IngredientList>().FirstOrDefault().doc.ToList();
            PopulateMenu();
        }

        /// <summary>
        /// Populates the list of items in this category based on the ingredients left in the inventory
        /// Items which require ingredients we do not have will not be displayed
        /// If no items can be displayed, a message will be shown instead
        /// </summary>
        void PopulateMenu()
        {
            // Clear existing categories
            while (categoryItemList.Children.Count != 0)
                categoryItemList.Children.RemoveAt(0);


            // Get menu items within this category.
            members = RealmManager.All<MenuItemsList>().FirstOrDefault().menuItems.Where((MenuFoodItem m) => m.category == category).ToList();

            // Determine which menu items we have sufficient ingredients for
            
            List<MenuFoodItem> memCopy = new List<MenuFoodItem>(); // Must copy the items since we cannot easily iterate and update at the same time
            foreach (MenuFoodItem m in members)
                memCopy.Add(new MenuFoodItem(m));

            foreach(MenuFoodItem m in memCopy)
            {
                foreach(Ingredient i in m.ingredients)
                {
                    // Remove a member if we don't have the necessary ingredients
                    var index = ingredients.FindIndex((Ingredient j) => j._id == i._id && j.quantity > 0);
                    if(index == -1)
                    {
                        members.RemoveAt(members.FindIndex((MenuFoodItem f) => f._id == m._id));
                        break;
                    }
                }
            }

            // Create buttons for each category member
            for (int i = 0; i < members.Count; ++i)
            {
                Button temp;
                string itemName = members[i].name;
                string itemID = members[i]._id;
                categoryItemList.Children.Add(temp = (new Button()
                {
                    Text = members[i].name + " | " + members[i].StringPrice,
                    Margin = new Thickness(20, 0, 20, 20),
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.White,
                    WidthRequest = 140,
                    BackgroundColor = Color.FromHex("1fbd85"),
                    CornerRadius = 15
                }));

                temp.Clicked += async (sender, args) => await Navigation.PushAsync(new menuItemPage(itemID));
            }

            // No items with sufficient ingredients
            if (members.Count() == 0)
            {
                categoryItemList.Children.Add(new Label()
                {
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Color.Black,
                    FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label)),
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(15, 15, 15, 15),
                    Text = "We've come up empty!"
                });

                categoryItemList.Children.Add(new Label()
                {
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Color.Black,
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                    Margin = new Thickness(15, 15, 15, 15),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Text = "We're all out of the ingredients for the items in this category. Please use the back button and look at a different category"
                });
            }
        }

        async void OnAlertButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new alertPage());
        }

        async void OnTableButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TablePage());
        }
    }
}
