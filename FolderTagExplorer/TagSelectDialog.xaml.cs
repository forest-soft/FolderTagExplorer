﻿using DataAccessLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FolderTagExplorer
{
	public sealed partial class TagSelectDialog : ContentDialog
	{
		private ObservableCollection<TagListRow> tag_list = new ObservableCollection<TagListRow>();

		private string data_name = null;
		private string item_id = null;
		public Dictionary<string, string> select_tag_id_list = null;

		public TagSelectDialog(string data_name, Dictionary<string, string> select_tag_id_list, string item_id = null)
		{
			this.InitializeComponent();

			this.data_name = data_name;
			this.item_id = item_id;
			/*
			Dictionary<string, object> item_data = DataAccess.GetItem(this.data_name, this.item_id);
			if (item_data == null)
			{
				// Todo エラー処理
			}
			select_tag_id_list = (Dictionary<string, string>)item_data["tag_id_list"];
			*/
			this.select_tag_id_list = select_tag_id_list;

			Dictionary<string, Dictionary<String, String>> list = DataAccess.GetTagList(this.data_name);
			foreach (var v in list)
			{
				this.tag_list.Add(new TagListRow(v.Value["id"], v.Value["name"]));
			}

			if (this.item_id != null)
			{
				this.TitleTextBlock.Text = "タグ紐付け";
				this.PrimaryButtonText = "保存";
			}
		}

		/// <summary>
		/// 選択・保存ボタン押下時のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			this.select_tag_id_list = new Dictionary<String, String>();
			foreach (TagListRow row in this.TagListView.SelectedItems)
			{
				string tag_id = row.Id;
				select_tag_id_list[tag_id] = tag_id;
			}
			
			if (this.item_id != null)
			{
				List<Dictionary<String, String>> select_tag_list = new List<Dictionary<String, String>>();
				foreach (string tag_id in this.select_tag_id_list.Values)
				{
					Dictionary<String, String> data = new Dictionary<String, String>();
					data["id"] = tag_id;
					select_tag_list.Add(data);
				}
				DataAccess.SaveTagRelationForItem(this.data_name, this.item_id, select_tag_list);
			}
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{

		}

		private void TagListView_Loaded(object sender, RoutedEventArgs e)
		{
			foreach (TagListRow row in this.tag_list)
			{
				if (select_tag_id_list.ContainsKey(row.Id))
				{
					this.TagListView.SelectedItems.Add(row);
				}
			}
		}
	}
}
