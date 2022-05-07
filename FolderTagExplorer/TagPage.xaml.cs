using DataAccessLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private string data_name = null;
		private ObservableCollection<TagListRow> tag_list = new ObservableCollection<TagListRow>();
		private Boolean is_change = false;
		private TagListRow edit_select_item = null;

		public TagPage()
		{
			this.InitializeComponent();

			this.data_name = ((App)Application.Current).data_name;

			this.LoadTagList();
		}

		private void LoadTagList(string scroll_tag_id = null)
		{
			Dictionary<string, Dictionary<String, String>> list = DataAccess.GetTagList(this.data_name);
			this.tag_list.Clear();
			foreach (var v in list)
			{
				TagListRow row = new TagListRow(v.Value["id"], v.Value["name"]);
				this.tag_list.Add(row);

				if (scroll_tag_id != null && scroll_tag_id == v.Value["id"])
				{
					this.TagListView.SelectedIndex = this.tag_list.IndexOf(row);
					this.TagListView.ScrollIntoView(row, ScrollIntoViewAlignment.Leading);
				}
			}
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is Dictionary<String, String>)
			{
				Dictionary<String, String> param = (Dictionary<String, String>)e.Parameter;

			}

			base.OnNavigatedTo(e);
		}

		private void BackButton_Click(object sender, RoutedEventArgs e)
		{
			((App)Application.Current).is_tag_change = this.is_change;

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
			dialog.PrimaryButtonClick += AddDialog_AddButton_Click;
			await dialog.ShowAsync();
		}

		private async void AddDialog_AddButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			string name = ((TextBox)sender.Content).Text.Trim();

			string error_message = this.Validate(name);
			if (error_message.Length != 0)
			{
				// ダイアログを多重で表示できないので登録ダイアログを一旦閉じる。
				sender.Hide();

				// エラーメッセージのダイアログを表示する。
				ContentDialog error_dialog = new ContentDialog
				{
					Content = error_message,
					CloseButtonText = "閉じる"
				};
				await error_dialog.ShowAsync();

				// 再度登録ダイアログを表示する。
				await sender.ShowAsync();
			}
			else
			{
				string id = DataAccess.AddTagData(this.data_name, name);

				this.LoadTagList(id);

				// 登録したタグをタグリストに反映させる。
				((App)Application.Current).SetTagList();

				sender.Hide();
			}
		}

		private String Validate(string name, string id = null)
		{
			if (name.Length == 0)
			{
				return "タグを入力してください。";
			}

			if (DataAccess.GetTagByName(this.data_name, name, id) != null)
			{
				return "既に存在するタグです。";
			}

			return "";
		}

		private async void EditButton_Click(object sender, RoutedEventArgs e)
		{
			TagListRow select_item = (TagListRow)((FrameworkElement)sender).DataContext;
			this.TagListView.SelectedIndex = this.tag_list.IndexOf(select_item);
			this.edit_select_item = select_item;

			TextBox inputTextBox = new TextBox();
			inputTextBox.AcceptsReturn = false;
			inputTextBox.Height = 32;
			inputTextBox.Text = select_item.Name;

			ContentDialog dialog = new ContentDialog();

			dialog.Content = inputTextBox;
			dialog.Title = "タグ更新";
			dialog.IsSecondaryButtonEnabled = true;
			dialog.PrimaryButtonText = "更新";
			dialog.SecondaryButtonText = "キャンセル";
			dialog.PrimaryButtonClick += EditDialog_EditButton_Click;
			dialog.SecondaryButtonClick += EditDialog_CancelButton_Click;
			await dialog.ShowAsync();
		}

		private void EditDialog_CancelButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			this.edit_select_item = null;
		}

		private async void EditDialog_EditButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			string id = this.edit_select_item.Id;
			string name = ((TextBox)sender.Content).Text.Trim();

			string error_message = this.Validate(name, id);
			if (error_message.Length != 0)
			{
				// ダイアログを多重で表示できないので登録ダイアログを一旦閉じる。
				sender.Hide();

				// エラーメッセージのダイアログを表示する。
				ContentDialog error_dialog = new ContentDialog
				{
					Content = error_message,
					CloseButtonText = "閉じる"
				};
				await error_dialog.ShowAsync();

				// 再度更新ダイアログを表示する。
				await sender.ShowAsync();
			}
			else
			{
				DataAccess.EditTagData(this.data_name, id, name);

				this.LoadTagList(id);

				this.edit_select_item = null;

				this.is_change = true;

				sender.Hide();
			}
		}

		private async void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			TagListRow select_item = (TagListRow)((FrameworkElement)sender).DataContext;
			this.TagListView.SelectedIndex = this.tag_list.IndexOf(select_item);

			ContentDialog noWifiDialog = new ContentDialog
			{
				// Title = "",
				Content = select_item.Name + "を削除しますか？",
				PrimaryButtonText = "はい",
				CloseButtonText = "いいえ"
			};

			ContentDialogResult result = await noWifiDialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				DataAccess.DeleteTagData(this.data_name, select_item.Id);
				this.tag_list.Remove(select_item);

				this.is_change = true;
			}
		}
	}
}
