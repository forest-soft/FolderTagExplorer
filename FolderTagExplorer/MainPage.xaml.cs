using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using DataAccessLibrary;
using Windows.Storage.FileProperties;
using Windows.UI.WindowManagement;
using Windows.UI.ViewManagement;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace FolderTagExplorer
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private ObservableCollection<NamedColor> recordings = new ObservableCollection<NamedColor>();
		private string data_name = "Main";

		public MainPage()
		{
			this.InitializeComponent();
			this.NavigationCacheMode = NavigationCacheMode.Enabled;

			this.init();
		}

		private async void init()
		{
			ApplicationView.GetForCurrentView().Title = data_name;

			// SQLiteのDBに初期データを流し込む。
			DataAccess.InitializeDatabase(this.data_name);

			Boolean is_show_permission_dialog = false;
			List<Dictionary<String, String>> list = DataAccess.SelectAllData(this.data_name);
			foreach (var v in list)
			{
				Boolean add_result = await this.AddItem(v["path"], true, v["id"]);
				if (!add_result)
				{
					is_show_permission_dialog = true;
				}
			}

			if (is_show_permission_dialog)
			{
				await this.ShowPermissionDialog();
			}
		}

		private async Task ShowPermissionDialog()
		{
			ContentDialog noWifiDialog = new ContentDialog
			{
				// Title = "",
				Content = "ファイル、フォルダにアクセスできませんでした。\r\n本アプリにファイルシステムへのアクセス許可を付与していない場合は、\r\nファイルシステムへのアクセス許可を付与してください。",
				PrimaryButtonText = "アプリのアクセス許可設定画面を開く",
				CloseButtonText = "閉じる"
			};
			ContentDialogResult result = await noWifiDialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				// await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
				await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures-app"));
			}
		}

		private void Page_DragOver(object sender, DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Link;
			e.DragUIOverride.Caption = "リストに追加する";
		}

		private async void Page_Drop(object sender, DragEventArgs e)
		{
			if (e.DataView.Contains(StandardDataFormats.StorageItems))
			{
				var items = await e.DataView.GetStorageItemsAsync();
				if (items.Count > 0)
				{
					foreach (var item in items)
					{
						if (item is StorageFile)
						{
							await this.AddItem(((StorageFile)item).Path);
						}
						else if (item is StorageFolder)
						{
							await this.AddItem(((StorageFolder)item).Path);
						}
						else
						{
							MessageDialog md = new MessageDialog("アクセスできませんでした。");
							await md.ShowAsync();
							return;
						}
					}
				}
			}
		}

		private void GridView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
		{
			//null;
			// 要素追加時に追加した要素の位置までスクロールさせたい。
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			Dictionary<String, String> param = new Dictionary<String, String>();
			param["data_name"] = this.data_name;
			this.Frame.Navigate(typeof(TagPage), param);
		}

		private async Task<Boolean> AddItem(string path, bool is_init = false, string id = null)
		{
			if (!is_init)
			{
				// 既に登録済みのPathであれば追加しない。
				if (DataAccess.GetItemByPath(this.data_name, path) != null)
				{
					return false;
				}
			}

			StorageFile storageFile = null;
			StorageFolder storageFolder = null;
			Boolean IsFile = false;
			Boolean is_exist = false;
			string name = null;
			try
			{
				storageFile = await StorageFile.GetFileFromPathAsync(path);
				IsFile = true;
				is_exist = true;
				name = storageFile.Name;
			}
			catch (Exception e)
			{
				try
				{
					storageFolder = await StorageFolder.GetFolderFromPathAsync(path);
					is_exist = true;
					name = storageFolder.Name;
				}
				catch (Exception e2)
				{
					if (!is_init)
					{
						// MessageDialog md = new MessageDialog("ファイル、フォルダが見つかりませんでした。");
						// await md.ShowAsync();

						await this.ShowPermissionDialog();
						return false;
					}

					name = Path.GetFileName(path);
				}
			}

			StorageFile imageFile = null;
			if (storageFile == null && storageFolder != null)
			{
				QueryOptions options = new QueryOptions();
				options.FolderDepth = FolderDepth.Shallow;
				// options.FolderDepth = FolderDepth.Deep;
				options.FileTypeFilter.Add(".jpg");
				options.FileTypeFilter.Add(".jpeg");
				options.FileTypeFilter.Add(".png");
				options.FileTypeFilter.Add(".gif");
				/*
				ネットワークドライブだとソート指定ができない。
				SortEntry sortOrder = new SortEntry();
				sortOrder.AscendingOrder = true;
				sortOrder.PropertyName = "System.FileName";
				options.SortOrder.Add(sortOrder);
				*/

				var result = storageFolder.CreateFileQueryWithOptions(options);
				IReadOnlyList<StorageFile> fileList = await result.GetFilesAsync(0, 1);
				if (fileList.Count != 0)
				{
					storageFile = fileList[0];
				}
			}

			if (storageFile != null && storageFile.ContentType.StartsWith("image/"))
			{
				imageFile = storageFile;
			}

			var bitmapImage = new BitmapImage();
			StorageItemThumbnail thumbnail = null;
			if (imageFile != null)
			{
				thumbnail = await imageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, 600);
			}
			else
			{
				if (storageFile != null)
				{
					thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, 600);
				}
				else if (storageFolder != null)
				{
					thumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.SingleItem, 600);
				}
			}

			if (thumbnail == null || (imageFile != null && thumbnail.Type == ThumbnailType.Icon))
			{
				if (imageFile == null)
				{
					/*
					var fontIcon = new FontIcon();
					fontIcon.FontFamily = new FontFamily("Segoe MDL2 Assets");
					fontIcon.Glyph = "\xE790";
					bitmapImage.SetSource(fontIcon);
					*/
				}
				else
				{
					// サムネイルのキャッシュが作られてない場合はデフォルトの画像アイコンが返ってきてしまうので、
					// その場合はオリジナル画像をそのまま表示する。
					bitmapImage.SetSource(await imageFile.OpenReadAsync());
				}
			}
			else
			{
				bitmapImage.SetSource(thumbnail);
			}

			if (!is_init)
			{
				id = DataAccess.AddData(this.data_name, path);
				this.recordings.Insert(0, new NamedColor(id, path, IsFile, name, is_exist, bitmapImage));
				
			}
			else
			{
				this.recordings.Add(new NamedColor(id, path, IsFile, name, is_exist, bitmapImage));
			}

			return is_exist;
		}

		private async void ImageGridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			NamedColor ClickedItem = (NamedColor)e.ClickedItem;

			if (!ClickedItem.IsExist)
			{
				MessageDialog md = new MessageDialog("ファイル、フォルダが見つかりませんでした。");
				await md.ShowAsync();

				this.ImageGridView.SelectedIndex = -1;
				return;
			}

			if (ClickedItem.IsFile)
			{
				StorageFile storageFile = await StorageFile.GetFileFromPathAsync(ClickedItem.Path);
				Boolean result = await Launcher.LaunchFileAsync(storageFile);
				if (!result)
				{
					// Windowsの設定で既定のアプリが選ばれていないとファイルの起動に失敗するので、アプリ選択画面を表示させる。
					var options = new LauncherOptions();
					options.DisplayApplicationPicker = true;
					await Launcher.LaunchFileAsync(storageFile, options);
				}
			}
			else
			{
				StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(ClickedItem.Path);
				await Launcher.LaunchFolderAsync(storageFolder);
			}

			this.ImageGridView.SelectedIndex = -1;
		}

		/// <summary>
		/// ImageGridViewのItemのコンテキストメニュークリック時のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void ImageGridViewItem_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			string type = (String)((FrameworkElement)sender).Tag;
			NamedColor select_item = (NamedColor)((FrameworkElement)sender).DataContext;

			if (type == "tag_relation")
			{
				TagSelectDialog ddd = new TagSelectDialog();
				await ddd.ShowAsync();
			}
			else if (type == "delete")
			{
				ContentDialog noWifiDialog = new ContentDialog
				{
					// Title = "",
					Content = "リストから削除しますか？",
					PrimaryButtonText = "はい",
					CloseButtonText = "いいえ"
				};

				ContentDialogResult result = await noWifiDialog.ShowAsync();
				if (result == ContentDialogResult.Primary)
				{
					DataAccess.DeleteItemData(this.data_name, select_item.Id);
					this.recordings.Remove(select_item);
				}
			}
		}
	}

	class NamedColor
	{
		public NamedColor(string id, string path, Boolean is_file, string display_name, Boolean is_exist, BitmapImage image)
		{
			this.Id = id;
			this.Path = path;
			this.IsFile = is_file;
			this.DisplayName = display_name;
			this.IsExist = is_exist;
			
			if (is_exist)
			{
				this.ImageSource = image;
				this.ImageVisibility = Visibility.Visible;
				this.NotExistIconVisibility = Visibility.Collapsed;
			}
			else
			{
				this.ImageVisibility = Visibility.Collapsed;
				this.NotExistIconVisibility = Visibility.Visible;
			}
		}

		public string Id { get; set; }

		public string Path { get; set; }

		public Boolean IsFile { get; set; }

		public string DisplayName { get; set; }

		public Boolean IsExist { get; set; }

		public BitmapImage ImageSource { get; set; }

		public Visibility NotExistIconVisibility { get; set; }
		public Visibility ImageVisibility { get; set; }
	}
}
