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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace FolderTagExplorer
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private ObservableCollection<NamedColor> recordings = new ObservableCollection<NamedColor>();

		public MainPage()
		{
			this.InitializeComponent();

			this.init();
		}

		private async void init()
		{
			// SQLiteのDBに初期データを流し込む。
			DataAccess.InitializeDatabase();

			List<Dictionary<String, String>> list = DataAccess.SelectAllData();
			foreach (var v in list)
			{
				await this.AddItem(v["path"], true);
			}
		}

		private void Page_DragOver(object sender, DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Link;
			e.DragUIOverride.Caption = "インポート";
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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//this.addItem("c:\\ddd");
			// DataAccess.AddData("button_click");
		}

		private async Task AddItem(string path, bool is_init = false)
		{
			if (!is_init)
			{
				// 既に登録済みのPathであれば追加しない。
				if (DataAccess.GetItemByPath(path) != null)
				{
					return;
				}
			}

			StorageFile storageFile = null;
			StorageFolder storageFolder = null;
			Boolean IsFile = false;
			string name = null;
			try
			{
				storageFile = await StorageFile.GetFileFromPathAsync(path);
				IsFile = true;
				name = storageFile.Name;
			}
			catch (Exception e)
			{
				try
				{
					storageFolder = await StorageFolder.GetFolderFromPathAsync(path);
					name = storageFolder.Name;
				}
				catch (Exception e2)
				{
					MessageDialog md = new MessageDialog("アクセスできませんでした。");
					await md.ShowAsync();
					return;

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
				// サムネイルのキャッシュが作られてない場合はデフォルトの画像アイコンが返ってきてしまうので、
				// その場合はオリジナル画像をそのまま表示する。
				bitmapImage.SetSource(await imageFile.OpenReadAsync());
			}
			else
			{
				bitmapImage.SetSource(thumbnail);
			}

			if (!is_init)
			{
				this.recordings.Insert(0, new NamedColor(path, IsFile, name, bitmapImage));
				DataAccess.AddData(path);
			}
			else
			{
				this.recordings.Add(new NamedColor(path, IsFile, name, bitmapImage));
			}
		}

		private async void ImageGridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			NamedColor ClickedItem = (NamedColor)e.ClickedItem;
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
		}
	}

	class NamedColor
	{
		public NamedColor(string Path, Boolean IsFile, string DisplayName, BitmapImage Image)
		{
			this.Path = Path;
			this.IsFile = IsFile;
			this.DisplayName = DisplayName;
			this.ImageSource = Image;
		}

		public string Path { get; set; }

		public Boolean IsFile { get; set; }

		public string DisplayName { get; set; }

		public BitmapImage ImageSource { get; set; }
	}
}
