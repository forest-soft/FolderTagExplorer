﻿using DataAccessLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FolderTagExplorer
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class TagPage : Page
	{
		public TagPage()
		{
			this.InitializeComponent();
		}

		private void BackButton_Click(object sender, RoutedEventArgs e)
		{
			this.Frame.GoBack();
		}

		private async void AddButton_Click(object sender, RoutedEventArgs e)
		{
			TextBox inputTextBox = new TextBox();
			inputTextBox.AcceptsReturn = false;
			inputTextBox.Height = 32;

			ContentDialog dialog = new ContentDialog();
			
			dialog.Content = inputTextBox;
			dialog.Title = "タグ追加";
			dialog.IsSecondaryButtonEnabled = true;
			dialog.PrimaryButtonText = "登録";
			dialog.SecondaryButtonText = "キャンセル";

			/*
			dialog.PrimaryButtonClick += (sender2, e2) =>
			{
				if (inputTextBox.Text.Length == 0)
				{
					e2.Cancel = true;
				}
				else
				{
					dialog.Hide();
				}
			};
			*/
			dialog.PrimaryButtonClick += AddDialog_AddButton_Click;
			await dialog.ShowAsync();
		}

		private async void AddDialog_AddButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			string name = ((TextBox)sender.Content).Text.Trim();

			string error_message = this.Validate(name);
			if (error_message.Length != 0)
			{
				sender.Hide();

				ContentDialog error_dialog = new ContentDialog
				{
					Content = error_message,
					CloseButtonText = "閉じる"
				};
				await error_dialog.ShowAsync();

				await sender.ShowAsync();
			}
			else
			{
				DataAccess.AddTagData("Main", name);

				sender.Hide();
			}
		}

		private String Validate(string name, string id = null)
		{
			if (name.Length == 0)
			{
				return "タグを入力してください。";
			}

			if (DataAccess.GetTagByName("Main", name, id) != null)
			{
				return "既に存在するタグです。";
			}

			return "";
		}
	}
}